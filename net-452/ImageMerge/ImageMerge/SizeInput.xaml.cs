using System.Text.RegularExpressions;
using System.Windows;
using static System.String;

namespace ImageMerge
{
    /// <summary>
    /// SizeInput.xaml 的交互逻辑
    /// </summary>
    public partial class SizeInput : Window
    {
        private const string RegexD = "\\d+";

        internal string SizeName { get; set; }
        internal Size Size { get; set; }

        public SizeInput()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var name = TextBoxName.Text;
            var width = TextBoxWidth.Text;
            var height = TextBoxHeight.Text;

            if (IsNullOrEmpty(name) || IsNullOrEmpty(width) || IsNullOrEmpty(height))
            {
                MessageBox.Show("请填写完整后添加");
                return;
            }

            if (!Regex.IsMatch(width, RegexD) || !Regex.IsMatch(height, RegexD))
            {
                MessageBox.Show("宽高必须是整数数字");
                return;
            }

            DialogResult = true;
            SizeName = name;
            Size = new Size(double.Parse(width), double.Parse(height));
        }
    }
}
