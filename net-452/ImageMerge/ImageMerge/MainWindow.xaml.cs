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
        private double _itemWidth, _itemHeight;

        private double ItemWidth
        {
            get { return _itemWidth; }
            set { TextBoxWidth.Text = (_itemWidth = value).ToString(CultureInfo.InvariantCulture); }
        }

        private double ItemHeight
        {
            get { return _itemHeight; }
            set { TextBoxHeight.Text = (_itemHeight = value).ToString(CultureInfo.InvariantCulture); }
        }

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

        private readonly Dictionary<string, Size> _presetSizeMap = new Dictionary<string, Size>
        {
            {"天猫", new Size(320, 304)},
            {"京东", new Size(340, 304)}
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

            var totalWidth = ItemWidth*2;
            var totalHeight = (count/2 + count%2)*ItemHeight;

            var drawingVisual = new DrawingVisual();
            var context = drawingVisual.RenderOpen();
            AddLog("打开画布");

            for (var i = 0; i < count; i++)
            {
                context.DrawImage(Images[i],
                    new Rect(new Point(i%2*((int) ItemWidth), i/2*((int) ItemHeight)), new Size(ItemWidth, ItemHeight)));
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

        private void PasteExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var content = Clipboard.GetText();
            LoadImages(content);
        }

        private void InputPath_OnClick(object sender, RoutedEventArgs e)
        {
            var pathInput = new PathInput();
            pathInput.ShowDialog();
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
            var selectedSize = ((KeyValuePair<string, Size>) ComboBoxPreset.SelectedValue).Value;
            ItemWidth = selectedSize.Width;
            ItemHeight = selectedSize.Height;
        }
    }
}