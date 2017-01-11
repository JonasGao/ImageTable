using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace ImageMerge
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        private double ItemWidth { get; set; }
        private double ItemHeight { get; set; }
        private int Col { get; set; }

        private bool _isImageDownloading;

        public bool IsImageDownloading
        {
            get { return _isImageDownloading; }
            set { GridBackDrop.Visibility = (_isImageDownloading = value) ? Visibility.Visible : Visibility.Hidden; }
        }

        private string LogContent { get; set; }

        public ObservableCollection<BitmapImage> Images { get; set; }

        private static HttpClient WebClient { get; } = new HttpClient();

        private static SaveFileDialog SaveFileDialog { get; } = new SaveFileDialog
        {
            Filter = "图像文件(*.jpg)|*.jpg"
        };

        private readonly ObservableCollection<KeyValuePair<string, LayoutProfile>> _presetSizeMap = new ObservableCollection<KeyValuePair<string, LayoutProfile>>
        {
            new KeyValuePair<string, LayoutProfile>("天猫", new LayoutProfile(320, 304, 2)),
            new KeyValuePair<string, LayoutProfile>("京东", new LayoutProfile(340, 304, 2))
        };

        public MainWindow()
        {
            InitializeComponent();

            ComboBoxPreset.ItemsSource = _presetSizeMap;
            ComboBoxPreset.SelectedIndex = 0;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            ClearLog();

            var count = Images.Count;

            if (count < 1)
            {
                AddLog("没有图片需要生成");
            }

            var totalWidth = ItemWidth*Col;
            var totalHeight = (count/Col + count%Col)*ItemHeight;

            var drawingVisual = new DrawingVisual();
            var context = drawingVisual.RenderOpen();
            AddLog("打开画布");

            for (var i = 0; i < count; i++)
            {
                context.DrawImage(Images[i],
                    new Rect(new Point(i%Col*((int) ItemWidth), i/Col*((int) ItemHeight)), new Size(ItemWidth, ItemHeight)));
            }

            context.Close();
            AddLog("已关闭画布");

            var renderTargetBitmap = new RenderTargetBitmap((int) totalWidth, (int) totalHeight, 96, 96,
                PixelFormats.Pbgra32);
            renderTargetBitmap.Render(drawingVisual);

            var encode = new JpegBitmapEncoder();
            encode.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

            var result = SaveFileDialog.ShowDialog();

            if (result == null || !result.Value) return;

            using (var stream = SaveFileDialog.OpenFile())
            {
                encode.Save(stream);
            }
        }

        private IEnumerable<Task<BitmapImage>> GetImagesAsync(IEnumerable<string> imagePaths)
        {
            return imagePaths.Select((path, i) => GetImageAsync(path)).ToArray();
        }

        private async Task<BitmapImage> GetImageAsync(string imagePath)
        {
            AddLog($"准备下载图片：{imagePath}");

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = await WebClient.GetStreamAsync(imagePath);
            bitmap.EndInit();

            AddLog($"图片下载结束：{imagePath}");
            return bitmap;
        }

        private string[] GetImagePaths(string paths)
        {
            var pathArray = paths.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
            var allStartWithHttp = pathArray.All(p => p.ToLower().StartsWith("http://"));
            if (allStartWithHttp) return pathArray;
            AddLog("一些地址不是以 http:// 开头。生成失败");
            throw new StopMergeException();
        }

        private void AddLog(string log)
        {
            if (LogContent == null)
                LogContent = "";

            LogContent += "\r\n" + log;

            TextBoxLog.Dispatcher.Invoke(() => { TextBoxLog.Text = LogContent; });
        }

        private void ClearLog()
        {
            TextBoxLog.Text = LogContent = "";
        }

        private async void LoadImages(string content)
        {
            if (IsImageDownloading)
                return;

            ProgressBarDownloading.Value = 0;
            IsImageDownloading = true;
            ButtonMerge.IsEnabled = false;

            try
            {
                var imagePaths = GetImagePaths(content);

                if (imagePaths.Length < 1)
                {
                    IsImageDownloading = false;
                    return;
                }

                if (Images != null && Images.Count > 0)
                    foreach (var bitmapImage in Images.Where(i => i.StreamSource != null))
                    {
                        bitmapImage.StreamSource.Dispose();
                    }

                Images = new ObservableCollection<BitmapImage>();
                var downloadTasks = GetImagesAsync(imagePaths);

                var imageDownloadTasks = downloadTasks as Task<BitmapImage>[] ?? downloadTasks.ToArray();
                double count = imageDownloadTasks.Length;
                for (var i = 0; i < count; i++)
                {
                    Images.Add(await imageDownloadTasks[i]);
                    ProgressBarDownloading.Value = (i + 1)/count*100;
                }

                ButtonMerge.IsEnabled = true;
                ImageListView.ItemsSource = Images;
            }
            catch (StopMergeException)
            {
                AddLog("任务出错，终止执行");
            }

            IsImageDownloading = false;
        }

        private void InputPath_OnClick(object sender, RoutedEventArgs e)
        {
            var pathInput = new PathInput();
            var result = pathInput.ShowDialog();
            if (result.HasValue && result.Value)
                LoadImages(pathInput.Paths);
        }

        private void ImageListView_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragDrop.DoDragDrop(this, new DataObject("dragger", sender), DragDropEffects.Move);
        }

        private void ImageListView_OnDrop(object sender, DragEventArgs e)
        {
            var fromImage = (Image) e.Data.GetData("dragger");
            var toImage = (Image) sender;

            var fromBitmap = (BitmapImage) fromImage.Source;
            var toBitmap = (BitmapImage) toImage.Source;

            var fromIndex = Images.IndexOf(fromBitmap);
            var toIndex = Images.IndexOf(toBitmap);

            Images[fromIndex] = toBitmap;
            Images[toIndex] = fromBitmap;
        }

        private void ComboBoxPreset_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedSize = ((KeyValuePair<string, LayoutProfile>) ComboBoxPreset.SelectedValue).Value;
            ItemWidth = selectedSize.Size.Width;
            ItemHeight = selectedSize.Size.Height;
            Col = selectedSize.Col;

            TextBlockWidth.Text = ItemWidth.ToString(CultureInfo.CurrentCulture);
            TextBlockHeight.Text = ItemHeight.ToString(CultureInfo.CurrentCulture);
            TextBlockPanelWidth.Text = (ItemWidth * Col).ToString(CultureInfo.CurrentCulture);
            TextBlockCol.Text = Col.ToString(CultureInfo.CurrentCulture);
        }

        private void ButtonSizeAdd_OnClick(object sender, RoutedEventArgs e)
        {
            var sizeInput = new SizeInput();
            var dialogResult = sizeInput.ShowDialog();
            if (!dialogResult.HasValue || !dialogResult.Value)
            {
                return;
            }
            var itemsSource = (ObservableCollection<KeyValuePair<string, LayoutProfile>>) ComboBoxPreset.ItemsSource;
            itemsSource.Add(new KeyValuePair<string, LayoutProfile>(sizeInput.SizeName, sizeInput.LayoutProfile));
            ComboBoxPreset.SelectedIndex = itemsSource.Count - 1;
        }
    }
}