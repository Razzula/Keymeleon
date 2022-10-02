//  Copyright (C) 2022  Jack Gillespie  https://github.com/Razzula/Keymeleon/blob/main/LICENSE.md

using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Keymeleon
{
    public partial class ApplicationListItem : UserControl
    {
        public ApplicationListItem(string name, Icon icon = null)
        {
            InitializeComponent();

            Text.Text = name;
            if (icon != null)
            {
                var img = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, new Int32Rect(0, 0, icon.Width, icon.Height), BitmapSizeOptions.FromEmptyOptions());
                Image.Source = img;
            }
            else
            {
                Image.Width = 22;
            }
        }

        public string GetName()
        {
            return Text.Text;
        }
    }
}
