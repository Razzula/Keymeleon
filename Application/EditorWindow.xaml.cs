//  Copyright (C) 2022  Jack Gillespie  https://github.com/Razzula/Keymeleon/blob/main/LICENSE.md

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

            //base
            FileInfo[] info = dirInfo.GetFiles("*.base");
            foreach (var file in info)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(file.FullName);
                if (fileName[0] != '_')
                {
                    baseList.Items.Add(fileName);
                }
            }
            if (!File.Exists("layouts/Default.base"))
            {
                configManager.SaveBaseConfig("layouts/Default.base");
            }
            baseList.SelectedItem = "Default";
            //layers
            info = dirInfo.GetFiles("*.conf");
            foreach (var file in info)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(file.FullName);
                if (!fileName.Contains('_'))
                {
                    layerList.Items.Add(fileName);
                }
            }

            LoadBaseConfig("layouts/Default.base");
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
                btn.Opacity = 1;
            }

            //display config on keyboard
            Debug.WriteLine(fileName);//DEBUG
            NativeMethods.SetLayoutBase(fileName, 1);

            readBtn.IsEnabled = false;
            loadIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Refresh_Disabled.png"));
            saveBtn.IsEnabled = false;
            saveIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Save_Disabled.png"));
        }

        private void LoadLayerConfig(string fileName, int layer)
        {
            var deltaState = configManager.GetStatesDelta(layer-1, layer);
            var tempState = configManager.LoadLayerConfig(fileName, layer);

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
                NativeMethods.SetKeyColour(item.Key, item.Value[0], item.Value[1], item.Value[2], 1);
            }

            foreach (var row in rows)
            {
                foreach (Button btn in row)
                {
                    if (btn != null)
                    {
                        btn.Opacity = 0.3;
                    }
                }
            }

            if (tempState == null)
            {
                return;
            }

            //apply new layer
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
                    btn.Opacity = 1;
                }
            }
            //display config on keyboard
            Debug.WriteLine(fileName);//DEBUG
            NativeMethods.ApplyLayoutLayer(fileName, 1);

            readBtn.IsEnabled = false;
            loadIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Refresh_Disabled.png"));
            saveBtn.IsEnabled = false;
            saveIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Save_Disabled.png"));
        }

        private void SaveBaseConfig(object sender, RoutedEventArgs e)
        {
            string fileName = baseList.SelectedItem.ToString();
            configManager.SaveBaseConfig("layouts/" + fileName + ".base");
        }

        private void SaveLayerConfig(object sender, RoutedEventArgs e)
        {
            string fileName = layerList.SelectedItem.ToString();

            if ((bool) hotkeyCheck.IsChecked) //layer
            {
                fileName += '_' + hotkeyList.SelectedItem.ToString();
            }

            configManager.SaveLayerConfig("layouts/" + fileName + ".conf", 1);
        }

        private void LoadBaseConfig(object sender, RoutedEventArgs e)
        {
            if (baseList.SelectedItem != null)
            {
                string fileName = baseList.SelectedItem.ToString();
                LoadBaseConfig("layouts/" + fileName + ".base");

                bool enable = !baseList.SelectedItem.Equals("Default");
                delBtn.IsEnabled = enable; //prevent deletion of default base
                if (enable)
                {
                    deleteIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Delete_Dark.png"));
                }
                else
                {
                    deleteIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Delete_Disabled.png"));
                }

                if ((bool) layerCheck.IsChecked)
                {
                    LoadLayerConfig(sender, e);
                }
            }
        }

        private void LoadLayerConfig(object sender, RoutedEventArgs e) //layerList changed
        {

            if (layerList.SelectedItem == null) { return; }

            string fileName = layerList.SelectedItem.ToString();
            LoadLayerConfig("layouts/" + fileName + ".conf", 1);

            if ((bool) hotkeyCheck.IsChecked)
            {
                if (hotkeyList.SelectedItem == null) { return; }
                fileName += '_' + hotkeyList.SelectedItem.ToString();
                LoadLayerConfig("layouts/" + fileName + ".conf", 2);
            }
        }

        private void LoadTopLayerConfig(object sender, RoutedEventArgs e) //hotketList changed
        {

            if (layerList.SelectedItem == null) { return; }
            if (hotkeyList.SelectedItem == null) { return; }

            string fileName = layerList.SelectedItem.ToString() + '_' + hotkeyList.SelectedItem.ToString();
            LoadLayerConfig("layouts/" + fileName + ".conf", 2);
        }

        public void CreateConfig(string fileName, string template=null)
        {   

            string[] file = fileName.Split(".");

            if (file[1].Equals("base"))
            {
                new ConfigManager().SaveBaseConfig("layouts/" + fileName);

                baseList.Items.Add(file[0]);
                baseList.SelectedItem = file[0];
            }
            else
            {
                if (template == null)
                {
                    File.Create("layouts/" + fileName).Close();
                }
                else
                {
                    File.Copy("layouts/" + template, "layouts/" + fileName);
                }

                layerList.Items.Add(file[0]);
                layerList.SelectedItem = file[0];
            }
            
        }

        private void ButtonClicked(object sender, MouseButtonEventArgs e)
        {
            Button btn = (Button) e.Source;
            string keycode = btn.Name.ToString();
            if (keycode[0].Equals('_'))
            {
                keycode = keycode.Substring(1);
            }

            if (rBox.Text.Equals("")) { rBox.Text = "0"; }
            if (gBox.Text.Equals("")) { gBox.Text = "0"; } //simple validation
            if (bBox.Text.Equals("")) { bBox.Text = "0"; }

            int r; int g; int b;

            if (e.ChangedButton == MouseButton.Left)
            {
                //get colour
                r = Int32.Parse(rBox.Text);
                g = Int32.Parse(gBox.Text);
                b = Int32.Parse(bBox.Text);
                
                btn.Opacity = 1;

                configManager.UpdateLayer(1, keycode, r, g, b);
            }
            else if (e.ChangedButton == MouseButton.Right && (bool) layerCheck.IsChecked)
            {
                //get colour
                int[] colourValues = configManager.RemoveKey(keycode, 1);

                r = colourValues[0];
                g = colourValues[1];
                b = colourValues[2];

                btn.Opacity = 0.3;
            }
            else
            {
                return;
            }

            readBtn.IsEnabled = true;
            loadIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Refresh.png"));
            saveBtn.IsEnabled = true;
            saveIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Save.png"));

            //reflect change in UI
            Color colour = Color.FromRgb(Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));
            btn.Foreground = new SolidColorBrush(colour);
            //write to device
            NativeMethods.SetKeyColour(keycode, r, g, b, 1);

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

        private void Exit(object sender, EventArgs e)
        {
            Close();
        }

        private void OnExit(object sender, EventArgs e)
        {
            MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.StartFocusMonitoring();
        }

        private void DeleteCurrentConfig(object sender, RoutedEventArgs e)
        {
            string fileName;
            if ((bool)hotkeyCheck.IsChecked) //top layer
            {
                fileName = layerList.SelectedItem.ToString() + '_' + hotkeyList.SelectedItem.ToString() + ".conf";
                File.Delete("layouts/" + fileName);
                LoadLayerConfig(sender, e);
                return;
            }

            string fileExtension;
            if ((bool)layerCheck.IsChecked) //layer
            {
                fileName = layerList.SelectedItem.ToString();
                fileExtension = ".conf";

                layerList.SelectedIndex = 0;
                layerList.Items.Remove(fileName);
            }
            else //base
            {
                fileName = baseList.SelectedItem.ToString();
                fileExtension = ".base";

                baseList.SelectedIndex = 0;
                baseList.Items.Remove(fileName);
            }
            File.Delete("layouts/"+fileName+fileExtension);

        }

        private void ToggleLayer(object sender, RoutedEventArgs e)
        {
            if ((bool) hotkeyCheck.IsChecked)
            {
                if (hotkeyList.SelectedIndex == -1)
                {
                    hotkeyList.SelectedIndex = 0;
                }
                Controls.SetValue(Grid.RowProperty, 2);
                delBtn.IsEnabled = true;
                deleteIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Delete_Dark.png"));
                newBtn.IsEnabled = false;
                newIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/New_Disabled.png"));
            }
            else if ((bool) layerCheck.IsChecked)
            {
                if (layerList.SelectedIndex == -1)
                {
                    layerList.SelectedIndex = 0;
                }
                hotkeyList.SelectedIndex = -1;
                Controls.SetValue(Grid.RowProperty, 1);
                delBtn.IsEnabled = true;
                deleteIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Delete_Dark.png"));
                newBtn.IsEnabled = true;
                newIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/New.png"));

                LoadLayerConfig(sender, e);
            }
            else
            {
                layerList.SelectedIndex = -1;
                Controls.SetValue(Grid.RowProperty, 0);
                newBtn.IsEnabled = true;
                newIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/New.png"));

                LoadBaseConfig(sender, e);
            }
            layerList.IsEnabled = (bool) layerCheck.IsChecked;
            hotkeyList.IsEnabled = (bool) hotkeyCheck.IsChecked;

            layerCheck.IsEnabled = (bool) !hotkeyCheck.IsChecked;
            hotkeyCheck.IsEnabled = (bool) layerCheck.IsChecked;
        }

        private void LoadConfig(object sender, RoutedEventArgs e)
        {
            if ((bool) layerCheck.IsChecked) //layer
            {
                LoadLayerConfig(sender, e);
            }
            else //base
            {
                LoadBaseConfig(sender, e);
            }
        }

        private void SaveConfig(object sender, RoutedEventArgs e)
        {
            if ((bool)layerCheck.IsChecked) //layer
            {
                SaveLayerConfig(sender, e);
            }
            else //base
            {
                SaveBaseConfig(sender, e);
            }
        }

        private void NewConfig(object sender, RoutedEventArgs e)
        {
            if ((bool)layerCheck.IsChecked) //layer
            {
                ApplicationSelector applicationSelector = new ApplicationSelector(this);
                applicationSelector.Owner = this;
                applicationSelector.ShowDialog(); //showDialog to prevent anything from happening until selector is closed
            }
            else //base
            {
                int i = 1;
                while (baseList.Items.Contains(i.ToString()))
                {
                    i++;
                }
                CreateConfig(i+".base");
            }
        }

        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            var menu = new Settings();
            menu.Owner = this;
            menu.ShowDialog();
        }
    }
}