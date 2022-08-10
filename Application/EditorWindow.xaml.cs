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

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Keymeleon
{
    public partial class EditorWindow : Window
    {
        static class NativeMethods
        {
            [DllImport("kym.dll")]
            public static extern int setKeyColour(string keycode, int r, int g, int b, int profile);
        }

        public EditorWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //get keycode
            var text = (e.Source as Button).Content.ToString();
            if (rBox.Text.Equals("")) { rBox.Text = "0"; }
            if (gBox.Text.Equals("")) { gBox.Text = "0"; } //simple validation
            if (bBox.Text.Equals("")) { bBox.Text = "0"; }
            //get colour
            int r = Int32.Parse(rBox.Text);
            int g = Int32.Parse(gBox.Text);
            int b = Int32.Parse(bBox.Text);
            Debug.WriteLine(text+" "+r+" "+g+" "+b);

            //write to device
            int temp = NativeMethods.setKeyColour(text, r, g, b, 1);
            Debug.WriteLine(temp);
            if (temp == 192) //successful
            {
                //reflect change in UI
                Color color = Color.FromRgb(Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));
                (e.Source as Button).Foreground = new SolidColorBrush(color);
            }

        }

        private void Text_Changed(object sender, RoutedEventArgs e)
        {
            string text = (e.Source as TextBox).Text;

            if (text.Equals(""))
            {
                (e.Source as TextBox).Text = "0";
                return;
            }

            int temp;
            try
            {
                temp = Int32.Parse(text);
            }
            catch (System.FormatException)
            {
                (e.Source as TextBox).Text = text.TrimEnd(text[text.Length - 1]);
                return;
            }

            if (temp > 255)
            {
                (e.Source as TextBox).Text = "255";
            }
            else if (temp < 0)
            {
                (e.Source as TextBox).Text = "0";
            }
        }
    }

}
