//  Copyright (C) 2022  Jack Gillespie  https://github.com/Razzula/Keymeleon/blob/main/LICENSE.md

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using System.IO;
using System.Diagnostics;
using System.Drawing;

namespace Keymeleon
{
    public partial class ZoneMarker : Window
    {
        EditorWindow editor;

        bool isCurrentFieldOrigin = true;

        string[] keys = {
            "Esc", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12", "PrtSc", "ScrLk", "Pause",
            "Tilde", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "Minus", "Equals", "Backspace", "Insert", "Home", "PgUp", "Num_Lock", "Num_Slash", "Num_Asterisk", "Num_Minus",
            "Tab", "q", "w", "e", "r", "t", "y", "u", "i", "o", "p", "BracketL", "BracketR", "Enter", "Delete", "End", "PgDn", "Num_7", "Num_8", "Num_9", "Num_Plus",
            "CapsLock", "a", "s", "d", "f", "g", "h", "j", "k", "l", "Semicolon", "Apostrophe", "Hash", "Num_4", "Num_5", "Num_6",
            "LShift", "Backslash", "z", "x", "c", "v", "b", "n", "m", "Comma", "Period", "Slash", "RShift", "Num_1", "Num_2", "Num_3",
            "LCtrl", "Super", "LAlt", "Space", "RAlt", "Fn", "Menu", "RCtrl", "Left", "Down", "Up", "Right", "Num_0", "Num_Period", "Num_Enter"
        };

        public ZoneMarker(EditorWindow owner, string? application = null, Bitmap? snapshot = null)
        {
            InitializeComponent();
            editor = owner;

            List<string> existingConfigs = new();

            //get all exist configs
            var dirInfo = new DirectoryInfo(Environment.CurrentDirectory + "/layouts");
            FileInfo[] info = dirInfo.GetFiles("*.conf");
            foreach (FileInfo file in info)
            {
                var config = System.IO.Path.GetFileNameWithoutExtension(file.FullName);
                if (!config.Contains('_')) //ignore temps/hotkeys
                {
                    existingConfigs.Add(config);
                    configList.Items.Add(config);
                }
            }

            if (application != null && snapshot != null)
            {
                string configFile = Environment.CurrentDirectory + "/layouts/" + application + ".conf";
                if (!File.Exists(configFile))
                {
                    var confirmation = new PopupConfirmation("Config not found", "There is no autokey configuration for this application. Create one?");
                    confirmation.ShowDialog();
                    if (confirmation.result)
                    {
                        File.Create(configFile).Close();
                        configList.Items.Add(application);
                    }
                }

                string imageFile = Environment.CurrentDirectory + "/snapshots/" + application + ".png";
                if (File.Exists(imageFile))
                {
                    var confirmation = new PopupConfirmation("Snapshot conflict", "Snapshot already exists for this application. Overwrite?");
                    confirmation.ShowDialog();
                    if (confirmation.result)
                    {
                        snapshot.Save(imageFile);
                    }
                }
                else
                {
                    snapshot.Save(imageFile);
                }
                snapshot.Dispose();

                configList.SelectedItem = application;
            }

        }

        public void Exit(object sender, EventArgs e)
        {
            Close();
        }

        private void ConfigSelected(object sender, SelectionChangedEventArgs e)
        {
            LoadConfig(sender, e);

            //image
            var dirInfo = new DirectoryInfo(Environment.CurrentDirectory + "/snapshots");
            FileInfo[] info = dirInfo.GetFiles("*.png");
            foreach (FileInfo file in info)
            {
                var imageName = System.IO.Path.GetFileNameWithoutExtension(file.FullName);
                if (imageName.Equals(configList.SelectedItem))
                {
                    snapshotDisplay.Source = new BitmapImage(new Uri(file.FullName));
                    return;
                }
            }
            snapshotDisplay.Source = null;
        }

        private void ListItemSelected(object sender, SelectionChangedEventArgs e)
        {
            snapshotCanvas.Cursor = Cursors.Pen;
        }

        private void DeleteCurrentConfig(object sender, RoutedEventArgs e)
        {
            var currentItem = configList.SelectedItem;
            if (configList.SelectedIndex == 0)
            {
                configList.SelectedIndex = 1;
            }
            else
            {
                configList.SelectedIndex = 0;
            }
            snapshotDisplay.Source = null;

            File.Delete(Environment.CurrentDirectory + "/layouts/" + currentItem + ".conf");
            //File.Delete(Environment.CurrentDirectory + "/snapshots/" + currentItem + ".png"); //TODO; fix
            configList.Items.Remove(currentItem);
        }

        private void LoadConfig(object sender, RoutedEventArgs e)
        {
            //clear
            snapshotCanvas.Children.Clear();
            zoneList.Items.Clear();

            //read file
            string fileName = "layouts/" + configList.SelectedItem + ".conf";

            StreamReader streamReader;
            try
            {
                streamReader = File.OpenText(fileName);
            }
            catch (System.IO.FileNotFoundException)
            {
                return;
            }

            string text = streamReader.ReadToEnd();
            streamReader.Close();
            //split into lines
            string[] lines = text.Split(Environment.NewLine);

            foreach (string line in lines)
            {
                //fill fields
                string[] lineData = line.Split(" ");
                if (lineData.Length >= 5)
                {
                    var listItem = AddZoneListItem();
                    listItem.SetKey(lineData[0]);
                    listItem.SetOrigin(Convert.ToDouble(lineData[1]), Convert.ToDouble(lineData[2]));
                    listItem.SetTarget(Convert.ToDouble(lineData[3]), Convert.ToDouble(lineData[4]));
                }
            }
            RedrawZones(sender, e);

            snapshotCanvas.Cursor = Cursors.No;
        }

        private void NewConfig(object sender, RoutedEventArgs e)
        {
            ApplicationSelector applicationSelector = new ApplicationSelector(".conf", SelectApplication);
            applicationSelector.Owner = this;
            applicationSelector.ShowDialog(); //showDialog to prevent anything from happening until selector is closed
        }

        private void SaveConfig(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists("layouts"))
            {
                Directory.CreateDirectory("layouts");
            }

            List<string> lines = new();

            foreach (ZoneListItem item in zoneList.Items)
            {
                var key = item.GetKey();
                if (key == null)
                {
                    PopupDialog popupDialog = new("Invalid data", "Cannot save due to invalid data.");
                    popupDialog.Owner = this;
                    popupDialog.ShowDialog();
                    return;
                }
                var originCoords = item.GetOrigin();
                var targetCoords = item.GetTarget();

                lines.Add(key + " " + originCoords[0] + " " + originCoords[1] + " " + targetCoords[0] + " " + targetCoords[1]);
            }

            File.WriteAllLines("layouts/" + configList.SelectedItem + ".conf", lines);
        }

        private void SelectApplication(string application)
        {
            configList.Items.Add(application);
            configList.SelectedItem = application;
        }

        private ZoneListItem AddZoneListItem()
        {
            var listItem = new ZoneListItem(SetCurrentField, RemoveZoneListItem);
            listItem.SetKeyList(keys);
            zoneList.Items.Add(listItem);

            return listItem;
        }

        private void AddZoneListItem(object sender, RoutedEventArgs e)
        {
            var item = AddZoneListItem();
            zoneList.SelectedItem = item;
        }

        public void RemoveZoneListItem(ZoneListItem listItem)
        {
            snapshotCanvas.Children.Remove(listItem.GetRectangle());
            zoneList.Items.Remove(listItem);
            snapshotCanvas.Cursor = Cursors.No;
        }

        System.Windows.Shapes.Rectangle currentRectangle;

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            var currentListItem = zoneList.SelectedItem as ZoneListItem;
            if (currentListItem == null)
            {
                return;
            }

            System.Windows.Point p = Mouse.GetPosition(snapshotCanvas);

            // screen co-ords
            Double x = p.X / snapshotCanvas.ActualWidth;
            Double y = p.Y / snapshotCanvas.ActualHeight;

            if (isCurrentFieldOrigin)
            {
                currentListItem.SetOrigin(x, y);
            }
            else
            {
                currentListItem.SetTarget(x, y);
            }
            Debug.WriteLine(x + "," + y);

            DrawRectangle(currentListItem);
        }

        private void DrawRectangle(ZoneListItem listItem)
        {
            //draw
            currentRectangle = listItem.GetRectangle();
            if (currentRectangle == null)
            { //create rectangle
                currentRectangle = new System.Windows.Shapes.Rectangle();
                currentRectangle.Fill = new SolidColorBrush() { Color = Colors.Yellow, Opacity = 0.35f };

                snapshotCanvas.Children.Add(currentRectangle);
                listItem.SetRectangle(currentRectangle);
            }

            Double[] coords1 = listItem.GetOrigin();
            Double[] coords2 = listItem.GetTarget();

            Double[] originCoords = new Double[2] { Math.Min(coords1[0], coords2[0]), Math.Min(coords1[1], coords2[1]) };
            Double[] targetCoords = new Double[2] { Math.Max(coords1[0], coords2[0]), Math.Max(coords1[1], coords2[1]) };

            Canvas.SetLeft(currentRectangle, originCoords[0] * snapshotCanvas.ActualWidth);
            Canvas.SetTop(currentRectangle, originCoords[1] * snapshotCanvas.ActualHeight);

            currentRectangle.Width = (targetCoords[0] - originCoords[0]) * snapshotCanvas.ActualWidth;
            currentRectangle.Height = (targetCoords[1] - originCoords[1]) * snapshotCanvas.ActualHeight;
        }

        private void SelectZoneItem(object sender, RoutedEventArgs e)
        {
            var listItem = (ZoneListItem)zoneList.SelectedItem;
            currentRectangle = listItem.GetRectangle();
        }

        public void SetCurrentField(ZoneListItem sender, bool isFieldOrigin)
        {
            zoneList.SelectedItem = sender;
            isCurrentFieldOrigin = isFieldOrigin;
        }

        private void RedrawZones(object sender, EventArgs e)
        {
            foreach (ZoneListItem item in zoneList.Items)
            {
                DrawRectangle(item);
            }
        }
    }
}
