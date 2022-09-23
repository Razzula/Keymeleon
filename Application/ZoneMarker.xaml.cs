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

using System.IO;
using System.Diagnostics;
using System.ServiceProcess;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Drawing;

namespace Keymeleon
{
    public partial class ZoneMarker : Window
    {
        EditorWindow editor;

        TextBox currentField;

        public ZoneMarker(EditorWindow owner, string? application=null, Bitmap? snapshot=null)
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

        }

        private void NewConfig(object sender, RoutedEventArgs e)
        {
            ApplicationSelector applicationSelector = new ApplicationSelector(".conf", SelectApplication);
            applicationSelector.Owner = this;
            applicationSelector.ShowDialog(); //showDialog to prevent anything from happening until selector is closed
        }

        private void SaveConfig(object sender, RoutedEventArgs e)
        {

        }

        private void SelectApplication(string application)
        {
            configList.Items.Add(application);
            configList.SelectedItem = application;
        }

        private void AddZoneListItem(object sender, RoutedEventArgs e)
        {
            var listItem = new ZoneListItem(RemoveZoneListItem);
            zoneList.Items.Add(listItem);
        }

        public void RemoveZoneListItem(ZoneListItem listItem)
        {
            snapshotCanvas.Children.Remove(listItem.GetRectangle());
            zoneList.Items.Remove(listItem);
        }

        System.Windows.Shapes.Rectangle currentRectangle;

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            System.Windows.Point p = Mouse.GetPosition(snapshotCanvas);

            // screen co-ords
            int x = (int)((p.X / snapshotCanvas.ActualWidth) * 1920f);
            int y = (int)((p.Y / snapshotCanvas.ActualHeight) * 1080f);

            if (currentField != null)
            {
                currentField.Text = x + "," + y;
            }
            Debug.WriteLine(x + "," + y);

            //UI
            if (currentRectangle == null)
            { //create rectangle
                System.Windows.Shapes.Rectangle rectangle = new System.Windows.Shapes.Rectangle();
                Canvas.SetLeft(rectangle, p.X);
                Canvas.SetTop(rectangle, p.Y);
                rectangle.Width = 5;
                rectangle.Height = 5;
                rectangle.Fill = new SolidColorBrush() { Color = Colors.Yellow, Opacity = 0.35f };

                snapshotCanvas.Children.Add(rectangle);
                currentRectangle = rectangle;
            }
            else
            { //alter rectangle
                var tempX = (int)p.X - Canvas.GetLeft(currentRectangle);
                if (tempX < 0) //left of original point
                {
                    Canvas.SetLeft(currentRectangle, p.X);
                }
                currentRectangle.Width = Math.Abs(tempX); //resize

                var tempY = (int) p.Y - Canvas.GetTop(currentRectangle);
                if (tempY < 0) //above original point
                {
                    Canvas.SetTop(currentRectangle, p.Y);
                }
                currentRectangle.Height = Math.Abs(tempY); //resize

                currentRectangle = null;
            }
        }
    }
}
