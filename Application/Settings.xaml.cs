//  Copyright (C) 2022  Jack Gillespie  https://github.com/Razzula/Keymeleon/blob/main/LICENSE.md

using System;
using System.Windows;

using Microsoft.Win32;
using System.IO;

namespace Keymeleon
{
    public partial class Settings : Window
    {
        RegistryKey key;

        public Settings()
        {
            InitializeComponent();

            //startup
            key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (key.GetValue("Keymeleon") != null)
            {
                startupBox.IsChecked = true;
            }

            //base
            var dirInfo = new DirectoryInfo(Environment.CurrentDirectory + "/layouts");
            FileInfo[] info = dirInfo.GetFiles("*.base");
            foreach (var file in info)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(file.FullName);
                if (fileName[0] != '_')
                {
                    baseList.Items.Add(fileName);
                }
            }
            if (baseList.Items.Contains(Properties.Settings.Default.AltBase))
            {
                baseList.SelectedItem = Properties.Settings.Default.AltBase;
            }
            else
            {
                baseList.SelectedItem = "Default";
            }
        }

        private void Exit(object sender, EventArgs e)
        {
            //apply changes
            if ((bool)startupBox.IsChecked)
            {
                key.SetValue("Keymeleon", System.AppDomain.CurrentDomain.BaseDirectory + "Keymeleon.exe");
            }
            else
            {
                if (key.GetValue("Keymeleon") != null)
                {
                    key.DeleteValue("Keymeleon");
                }
            }

            if (baseList.SelectedItem != null)
            {
                Properties.Settings.Default.AltBase = baseList.SelectedItem.ToString();
            }
            Properties.Settings.Default.Save();

            Close();
        }
    }
}