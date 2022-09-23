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
            File.Delete(Environment.CurrentDirectory + "/layouts/" + currentItem + ".conf");
            File.Delete(Environment.CurrentDirectory + "/snapshots/" + currentItem + ".png");
            configList.SelectedIndex = 0;
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

        public void SelectApplication(string application)
        {
            configList.Items.Add(application);
            configList.SelectedItem = application;
        }
    }
}
