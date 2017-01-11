using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ImageMerge
{
    class LayoutProfile
    {
        internal Size Size { get; set; }

        internal int Col { get; set; }

        internal LayoutProfile(double width, double height, int col)
        {
            Size = new Size(width, height);
            Col = col;
        }

        public override string ToString()
        {
            return string.Format("{0} / {1} 列", Size, Col);
        }
    }
}
