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
        internal LayoutProfile LayoutProfile { get; set; }

        public SizeInput()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var name = TextBoxName.Text;
            var width = TextBoxWidth.Text;
            var height = TextBoxHeight.Text;
            var col = TextBoxCol.Text;

            if (IsNullOrEmpty(name) || IsNullOrEmpty(width) || IsNullOrEmpty(height) || IsNullOrEmpty(col))
            {
                MessageBox.Show("请填写完整后添加");
                return;
            }

            if (!Regex.IsMatch(width, RegexD) || !Regex.IsMatch(height, RegexD) || !Regex.IsMatch(col, RegexD))
            {
                MessageBox.Show("宽高和列必须是整数数字");
                return;
            }

            DialogResult = true;
            SizeName = name;
            LayoutProfile = new LayoutProfile(double.Parse(width), double.Parse(height), int.Parse(col));
        }
    }
}
