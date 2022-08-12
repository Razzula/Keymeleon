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
using System.IO;

namespace Keymeleon
{
    public partial class EditorWindow : Window
    {
        static class NativeMethods
        {
            [DllImport("kym.dll")]
            public static extern int SetLayoutBase(string configFileName, int profileToModify);
            [DllImport("kym.dll")]
            public static extern int SetActiveProfile(int profile);
            [DllImport("kym.dll")]
            public static extern int ApplyLayoutLayer(string configFileName, int profileToModify);
            [DllImport("kym.dll")]
            public static extern int SetKeyColour(string keycode, int r, int g, int b, int profile);
        }

        ConfigManager configManager;
        Button[][] rows;

        public EditorWindow()
        {
            InitializeComponent();

            configManager = new ConfigManager();

            Button[][] rows = {
                new[] { Esc, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12 },
                new[] { Tilde, _1, _2, _3, _4, _5, _6, _7, _8, _9, _0, Minus, Equals, Num_Lock, Num_Slash, Num_Asterisk },
                new[] { Tab, q, w, e, r, t, y, u, i, o, p, BracketL, BracketR, Num_7, Num_8, Num_9 },
                new[] { CapsLock, a, s, d, f, g, h, j, k, l, Semicolon, Apostrophe, Hash, Num_4, Num_5, Num_6 },
                new[] { LShift, z, x, c, v, b, n, m, Comma, Period, Slash, RShift, Enter, Num_1, Num_2, Num_3 },
                new[] { LCtrl, Super, LAlt, Space, RAlt, Fn, Menu, RCtrl, Left, Down, Up, Right, Backspace, Num_0, Num_Period, Num_Enter },
                new[] { Backslash, PrtSc, ScrLk, Pause, null, Insert, Home, PgUp, Delete, End, PgDn, Num_Minus, Num_Plus }
            };
            this.rows = rows;

            var dirInfo = new DirectoryInfo(Environment.CurrentDirectory+"/layouts");
            FileInfo[] info = dirInfo.GetFiles("*.conf");

            //setup list box
            ComboBox comboBox = applicationsList;
            comboBox.Items.Add("Default.base"); //temp
            comboBox.SelectedItem = comboBox.Items.GetItemAt(0);
            foreach (var file in info)
            {
                string fileName = System.IO.Path.GetFileName(file.FullName);
                if (fileName[0] != '_')
                {
                    comboBox.Items.Add(fileName);
                }
            }

            LoadBaseConfig("layouts/default.base");
            NativeMethods.SetActiveProfile(1);
        }

        private void LoadBaseConfig(string fileName)
        {
            var tempState = configManager.LoadBaseConfig(fileName);
            if (tempState == null)
            {
                return;
            }

            foreach (var item in tempState)
            {
                //show colour in UI
                Color colour = Color.FromRgb(Convert.ToByte(item.Value[0]), Convert.ToByte(item.Value[1]), Convert.ToByte(item.Value[2]));
                Button btn = (Button) this.FindName(item.Key);
                if (btn == null)
                {
                    btn = (Button)this.FindName("_" + item.Key);
                }
                btn.Foreground = new SolidColorBrush(colour);
            }

            //display config on keyboard
            Debug.WriteLine(fileName);//DEBUG
            Debug.WriteLine(NativeMethods.SetLayoutBase(fileName, 1));

            readBtn.IsEnabled = false;
        }

        private void LoadLayerConfig(string fileName)
        {
            var deltaState = configManager.GetStatesDelta();
            var tempState = configManager.LoadLayerConfig(fileName);
            if (tempState == null)
            {
                return;
            }

            //undo previous layer
            foreach (var item in deltaState)
            {
                var btn = (Button)this.FindName(item.Key);
                if (btn == null)
                {
                    btn = (Button)this.FindName("_" + item.Key);
                }
                //show colour in UI
                Color colour = Color.FromRgb(Convert.ToByte(item.Value[0]), Convert.ToByte(item.Value[1]), Convert.ToByte(item.Value[2]));
                btn.Foreground = new SolidColorBrush(colour);
                //show colour on device
                NativeMethods.SetKeyColour(item.Key, item.Value[0], item.Value[1], item.Value[2], 1); //TODO; optimize
            }

            foreach (var item in tempState)
            {
                var btn = (Button) this.FindName(item.Key);
                if (btn == null)
                {
                    btn = (Button)this.FindName("_"+ item.Key);
                }

                if (btn != null)
                {
                    //show colour in UI
                    Color colour = Color.FromRgb(Convert.ToByte(item.Value[0]), Convert.ToByte(item.Value[1]), Convert.ToByte(item.Value[2]));
                    btn.Foreground = new SolidColorBrush(colour);
                }
            }
            //display config on keyboard
            Debug.WriteLine(fileName);//DEBUG
            Debug.WriteLine(NativeMethods.ApplyLayoutLayer(fileName, 1));

            readBtn.IsEnabled = false;
        }

        private void SaveConfig(object sender, RoutedEventArgs e)
        {
            string fileName = applicationsList.SelectedItem.ToString();
            if (fileName.EndsWith(".base"))
            {
                configManager.SaveBaseConfig("layouts/"+fileName);
            }
            else if (fileName.EndsWith(".conf"))
            {
                configManager.SaveLayerConfig("layouts/"+fileName);
            }
        }

        private void LoadConfig(object sender, RoutedEventArgs e)
        {
            string fileName = applicationsList.SelectedItem.ToString();
            if (fileName.EndsWith(".base"))
            {
                LoadBaseConfig("layouts/"+fileName);
            }
            else if (fileName.EndsWith(".conf"))
            {
                LoadLayerConfig("layouts/"+fileName);
            }
        }

        public void CreateConfig(string fileName)
        {
            File.Create("layouts/"+fileName).Close();
            applicationsList.Items.Add(fileName);
            applicationsList.SelectedItem = fileName;
        }

        private void ButtonClicked(object sender, RoutedEventArgs e)
        {
            string keycode = (e.Source as Button).Name.ToString();
            if (keycode[0].Equals('_'))
            {
                keycode = keycode.Substring(1);
            }
            if (rBox.Text.Equals("")) { rBox.Text = "0"; }
            if (gBox.Text.Equals("")) { gBox.Text = "0"; } //simple validation
            if (bBox.Text.Equals("")) { bBox.Text = "0"; }
            //get colour
            int r = Int32.Parse(rBox.Text);
            int g = Int32.Parse(gBox.Text);
            int b = Int32.Parse(bBox.Text);
            //Debug.WriteLine(keycode + " "+r+" "+g+" "+b);//DEBUG

            //reflect change in UI
            Color colour = Color.FromRgb(Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));
            (e.Source as Button).Foreground = new SolidColorBrush(colour);
            configManager.UpdateLayer(keycode, r, g, b);

            //write to device
            Debug.WriteLine(NativeMethods.SetKeyColour(keycode, r, g, b, 1));

        }

        private void TextChanged(object sender, RoutedEventArgs e)
        {
            string text = (e.Source as TextBox).Text;

            if (text.Equals(""))
            {
                (e.Source as TextBox).Text = "0";
                DisplayColour();
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
            DisplayColour();
        }

        private void DisplayColour()
        {
            //get colour
            if (rBox == null) { return; }
            int r = Int32.Parse(rBox.Text);
            if (bBox == null) { return; }
            int g = Int32.Parse(gBox.Text);
            if (bBox == null) { return; }
            int b = Int32.Parse(bBox.Text);

            if (colourDisplay == null) { return; }
            Color colour = Color.FromRgb(Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));
            colourDisplay.Fill = new SolidColorBrush(colour);
        }

        private void OnWindowClose(object sender, EventArgs e)
        {
            MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.StartFocusMonitoring();
        }

        private void OpenApplicationSelector(object sender, RoutedEventArgs e)
        {
            ApplicationSelector applicationSelector = new ApplicationSelector(this);
            applicationSelector.Show(); //TODO; prevent anything from happening until selector is closed
        }

        private void DeleteCurrentConfig(object sender, RoutedEventArgs e)
        {
            string fileName = applicationsList.SelectedItem.ToString();
            if (fileName.Equals("Default.conf"))
            {
                return;
            }
            File.Delete("layouts/"+fileName);

            applicationsList.SelectedIndex = 0;
            applicationsList.Items.Remove(fileName);
            
        }
    }

}