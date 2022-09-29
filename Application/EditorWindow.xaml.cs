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

        string selectedControl = "BRUSH";
        Cursor activeCursor = new Cursor(Application.GetResourceStream(new Uri("Resources/cursors/BRUSH.cur", UriKind.Relative)).Stream);

        string lastSelected;

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
            info = dirInfo.GetFiles("*.layer");
            foreach (var file in info)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(file.FullName);
                if (!fileName.Contains('_')) //TODO; somehow distinguish between temp/hotkey files and genuine (e.g. streaming_client.exe)
                {
                    layerList.Items.Add(fileName);
                }
            }
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
            int res = NativeMethods.SetLayoutBase(fileName, 1);
            if (res < 0)
            {
                OnError();
                NativeMethods.SetLayoutBase(fileName, 1);
            }

            readBtn.IsEnabled = false;
            loadIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Refresh_Disabled.png"));
            saveBtn.IsEnabled = false;
            saveIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Save_Disabled.png"));
        }

        private void LoadLayerConfig(string fileName, int layer)
        {
            var deltaState = configManager.GetStatesDelta(layer-1, layer);
            var tempState = configManager.LoadLayerConfig(fileName, layer);
            int res;

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
                res = NativeMethods.SetKeyColour(item.Key, item.Value[0], item.Value[1], item.Value[2], 1);
                if (res < 0)
                {
                    OnError();
                    NativeMethods.SetKeyColour(item.Key, item.Value[0], item.Value[1], item.Value[2], 1);
                }
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
            res = NativeMethods.ApplyLayoutLayer(fileName, 1);
            if (res < 0)
            {
                OnError();
                NativeMethods.ApplyLayoutLayer(fileName, 1);
            }

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
            int layer = 1;

            if ((bool) hotkeyCheck.IsChecked) //layer
            {
                fileName += '_' + hotkeyList.SelectedItem.ToString();
                layer = 2;
            }

            configManager.SaveLayerConfig("layouts/" + fileName + ".layer", layer);
        }

        private void LoadBaseConfig(object sender, RoutedEventArgs e)
        {
            if (baseList.SelectedIndex == -1) { return; }
            if (baseList.SelectedItem.Equals(lastSelected)) { return; }
            if (!ConfirmChanges())
            {
                baseList.SelectedItem = lastSelected;
                lastSelected = null;
                return;
            }

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
            if (layerList.SelectedIndex == -1) { return; }
            if (layerList.SelectedItem.Equals(lastSelected)) { return; }
            if (!ConfirmChanges())
            {
                layerList.SelectedItem = lastSelected;
                lastSelected = null;
                return;
            }

            if (layerList.SelectedItem == null) { return; }

            string fileName = layerList.SelectedItem.ToString();
            LoadLayerConfig("layouts/" + fileName + ".layer", 1);

            if ((bool) hotkeyCheck.IsChecked)
            {
                if (hotkeyList.SelectedItem == null) { return; }
                fileName += '_' + hotkeyList.SelectedItem.ToString();
                LoadLayerConfig("layouts/" + fileName + ".layer", 2);
            }
        }

        private void LoadTopLayerConfig(object sender, RoutedEventArgs e) //hotketList changed
        {
            if (hotkeyList.SelectedIndex == -1) { return; }
            if (hotkeyList.SelectedItem.Equals(lastSelected)) { return; }
            if (!ConfirmChanges())
            {
                hotkeyList.SelectedItem = lastSelected;
                lastSelected = null;
                return;
            }

            if (layerList.SelectedItem == null) { return; }
            if (hotkeyList.SelectedItem == null) {
                LoadLayerConfig(sender, e);
                return;
            }

            string fileName = layerList.SelectedItem.ToString() + '_' + hotkeyList.SelectedItem.ToString();
            LoadLayerConfig("layouts/" + fileName + ".layer", 2);
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
            
        }

        private void SetControl(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)e.Source;

            if (btn.Tag != null)
            {
                selectedControl = btn.Tag.ToString();
            }

            // reflect in UI
            brushBtn.BorderBrush = null;
            eraseBtn.BorderBrush = null;
            dropBtn.BorderBrush = null;
            fillBtn.BorderBrush = null;
            btn.BorderBrush = new SolidColorBrush((Color) ColorConverter.ConvertFromString("White"));

            //set cursor to tool icon
            activeCursor = new Cursor(
                Application.GetResourceStream(new Uri("Resources/cursors/"+selectedControl+".cur", UriKind.Relative)).Stream
            );
        }

        private void UpdateCursor(object sender, MouseEventArgs e)
        {
            if (activeCursor != null)
            {
                this.Cursor = activeCursor;
            }
        }

        private void ResetCursor(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }

        private void ButtonClicked(object sender, MouseButtonEventArgs e)
        {
            int layer;
            if ((bool) hotkeyCheck.IsChecked)
            {
                layer = 2;
            }
            else
            {
                layer = 1;
            }

            Button btn = (Button) e.Source;
            string keycode = btn.Name.ToString();
            if (keycode[0].Equals('_'))
            {
                keycode = keycode.Substring(1);
            }

            int r; int g; int b;

            if (e.ChangedButton == MouseButton.Left) // SELECTED CONTROL
            {
                switch (selectedControl)
                {
                    case ("BRUSH"):
                        //get colour
                        var selectedColour = colourPicker.SelectedColor;
                        r = selectedColour.R;
                        g = selectedColour.G;
                        b = selectedColour.B;

                        btn.Opacity = 1;

                        configManager.UpdateLayer(layer, keycode, r, g, b);
                        break;

                    case ("ERASER"):
                        if ((bool)layerCheck.IsChecked) //layer
                        {
                            //get colour
                            int[] colourValues = configManager.RemoveKey(keycode, layer);

                            r = colourValues[0];
                            g = colourValues[1];
                            b = colourValues[2];

                            btn.Opacity = 0.3;
                        }
                        else //base
                        {
                            r = 0;
                            g = 0;
                            b = 0;
                            configManager.UpdateLayer(layer, keycode, r, g, b);
                        }
                        break;

                    case ("EYEDROP"):
                        GetKeyColour(btn);
                        return;

                    case ("FILL"): //TODO; tolerance level
                        selectedColour = colourPicker.SelectedColor;
                        r = selectedColour.R;
                        g = selectedColour.G;
                        b = selectedColour.B;

                        var data = configManager.UpdateLayerMass(keycode, r, g, b);
                        Color newColour = Color.FromRgb(Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));

                        if (data.Count > 7) //use _temp.base
                        {
                            NativeMethods.SetLayoutBase("layouts/_temp.base", 1);
                            File.Delete("layouts/_temp.base");
                        }
                        foreach (string key in data)
                        {
                            //UI
                            btn = (Button)FindName(key);
                            if (btn == null)
                            {
                                btn = (Button)FindName("_" + key);
                            }
                            btn.Foreground = new SolidColorBrush(newColour);
                            //keyboard
                            if (data.Count <= 7)
                            {
                                NativeMethods.SetKeyColour(key, r, g, b, 1);
                            }
                        }

                        readBtn.IsEnabled = true;
                        loadIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Refresh.png"));
                        saveBtn.IsEnabled = true;
                        saveIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Save.png"));

                        return;

                    default:
                        return;
                }
            }
            else if (e.ChangedButton == MouseButton.Right) // ERASER  && (bool) layerCheck.IsChecked
            {
                if ((bool)layerCheck.IsChecked) //layer
                {
                    //get colour
                    int[] colourValues = configManager.RemoveKey(keycode, layer);

                    r = colourValues[0];
                    g = colourValues[1];
                    b = colourValues[2];

                    btn.Opacity = 0.3;
                }
                else //base
                {
                    r = 0;
                    g = 0;
                    b = 0;
                    configManager.UpdateLayer(layer, keycode, r, g, b);
                }
            }
            else if (e.ChangedButton == MouseButton.Middle) // EYEDROP
            {
                GetKeyColour(btn);
                return;
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
            int res = NativeMethods.SetKeyColour(keycode, r, g, b, 1);
            if (res < 0)
            {
                OnError();
                NativeMethods.SetKeyColour(keycode, r, g, b, 1);
            }

        }

        private void PaletteClicked(object sender, MouseButtonEventArgs e)
        {
            int r; int g; int b;
            var control = (Rectangle)e.Source;

            if (e.ChangedButton == MouseButton.Left) // SELECTED CONTROL
            {
                switch (selectedControl)
                {
                    case ("ERASER"):
                        control.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#353535"));
                        break;

                    case ("EYEDROP"):
                        string keyHex = control.Fill.ToString();
                        Color colour = (Color)ColorConverter.ConvertFromString(keyHex);
                        colourPicker.SelectedColor = colour;
                        break;

                    default: // BRUSH or FILL
                        var selectedColour = colourPicker.SelectedColor;
                        control.Fill = new SolidColorBrush(selectedColour);

                        break;

                }
            }
            else if (e.ChangedButton == MouseButton.Right) // ERASER
            {
                control.Fill = new SolidColorBrush((Color) ColorConverter.ConvertFromString("#353535"));
            }
            else if (e.ChangedButton == MouseButton.Middle) // EYEDROP
            {
                string keyHex = control.Fill.ToString();
                Color colour = (Color)ColorConverter.ConvertFromString(keyHex);
                colourPicker.SelectedColor = colour;
            }
        }

        private void GetKeyColour(Button btn)
        {
            string keyHex = btn.Foreground.ToString();
            Color keyColour = (Color)ColorConverter.ConvertFromString(keyHex);
            colourPicker.SelectedColor = keyColour;
        }

        private void Exit(object sender, EventArgs e)
        {
            Close();
        }

        private void OnExit(object sender, EventArgs e)
        {
            MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.StartController();
        }

        private void DeleteCurrentConfig(object sender, RoutedEventArgs e)
        {
            if (!ConfirmChanges())
            {
                return;
            }
            saveBtn.IsEnabled = false;

            string fileName;
            if ((bool)hotkeyCheck.IsChecked) //top layer
            {
                fileName = layerList.SelectedItem.ToString() + '_' + hotkeyList.SelectedItem.ToString() + ".layer";
                File.Delete("layouts/" + fileName);
                LoadLayerConfig(sender, e);
                return;
            }

            string fileExtension;
            if ((bool)layerCheck.IsChecked) //layer
            {
                fileName = layerList.SelectedItem.ToString();
                fileExtension = ".layer";

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

            if (!ConfirmChanges())
            {
                var checkBox = sender as CheckBox;
                checkBox.IsChecked = !checkBox.IsChecked;
                return;
            }
            saveBtn.IsEnabled = false;

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
                fillBtn.IsEnabled = false;
                fillIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Fill_Disabled.png"));
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

                // disable fill
                fillBtn.IsEnabled = false;
                fillIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Fill_Disabled.png"));
                fillBtn.BorderBrush = null;

                selectedControl = "BRUSH";
                brushBtn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("White"));
                activeCursor = new Cursor(Application.GetResourceStream(new Uri("Resources/cursors/BRUSH.cur", UriKind.Relative)).Stream );
            }
            else
            {
                layerList.SelectedIndex = -1;
                Controls.SetValue(Grid.RowProperty, 0);
                newBtn.IsEnabled = true;
                newIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/New.png"));

                fillBtn.IsEnabled = true;
                fillIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Fill_Dark.png"));

                LoadBaseConfig(sender, e);
            }
            layerList.IsEnabled = (bool) layerCheck.IsChecked;
            hotkeyList.IsEnabled = (bool) hotkeyCheck.IsChecked;

            layerCheck.IsEnabled = (bool) !hotkeyCheck.IsChecked;
            hotkeyCheck.IsEnabled = (bool) layerCheck.IsChecked;
        }

        private bool ConfirmChanges()
        {
            if (!saveBtn.IsEnabled)
            {
                return true;
            }
            PopupConfirmation popupConfirmation = new("Unsaved changes", "Unsaved changes will be lost. Are you sure you wish to proceed?");
            popupConfirmation.Owner = this;
            popupConfirmation.ShowDialog();

            return popupConfirmation.result;
        }

        private void LoadConfig(object sender, RoutedEventArgs e)
        {

            if(!ConfirmChanges())
            {
                return;
            }
            saveBtn.IsEnabled = false;

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

            readBtn.IsEnabled = false;
            loadIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Refresh_Disabled.png"));
            saveBtn.IsEnabled = false;
            saveIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Save_Disabled.png"));
        }

        private void NewConfig(object sender, RoutedEventArgs e)
        {
            if (!ConfirmChanges())
            {
                return;
            }
            saveBtn.IsEnabled = false;

            if ((bool)layerCheck.IsChecked) //layer
            {
                ApplicationSelector applicationSelector = new ApplicationSelector(".layer", SelectApplication);
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

        public void SelectApplication(string application)
        {
            layerList.Items.Add(application);
            layerList.SelectedItem = application;
        }

        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            var menu = new Settings();
            menu.Owner = this;
            menu.ShowDialog();
        }

        private void OnError()
        {
            var dialog = new PopupDialog("Error", "Could not write to keyboard.\nPlease reconnect the device, then continue.");
            dialog.ShowDialog();
        }

        private void OpenZoneMarker(object sender, RoutedEventArgs e)
        {
            OpenZoneMarker(null);
        }

        public void OpenZoneMarker(string? currentApplication, System.Drawing.Bitmap? snapshot=null)
        {
            ZoneMarker zoneMarker = new ZoneMarker(this, currentApplication, snapshot);

            zoneMarker.Owner = this;
            zoneMarker.ShowDialog();
        }

        private void StoreCurrentSelection(object sender, MouseEventArgs e)
        {
            lastSelected = (sender as ComboBox).SelectedItem.ToString();
        }
    }
}