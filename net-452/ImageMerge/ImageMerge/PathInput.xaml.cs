using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ImageMerge
{
    /// <summary>
    /// PathInput.xaml 的交互逻辑
    /// </summary>
    public partial class PathInput : Window
    {
        public PathInput()
        {
            InitializeComponent();
        }

        internal string Paths { get; set; }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Paths = TextBoxPaths.Text;
            Close();
        }
    }
}
