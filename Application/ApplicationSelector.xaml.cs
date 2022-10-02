//  Copyright (C) 2022  Jack Gillespie  https://github.com/Razzula/Keymeleon/blob/main/LICENSE.md

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

using System.IO;
using System.Diagnostics;
using System.Drawing;

namespace Keymeleon
{
    public partial class ApplicationSelector : Window
    {
        //EditorWindow editor;
        Action<string> submitFunction;
        string fileExtension;

        List<string> existingConfigs = new();
        List<string> currentApplications = new();
        List<string> allApplications = new();

        //bool isExpanded = false;

        public ApplicationSelector(string fileExtension, Action<string> submitFunction)
        {
            InitializeComponent();
            this.submitFunction = submitFunction;
            this.fileExtension = fileExtension;

            //get all exist configs
            var dirInfo = new DirectoryInfo(Environment.CurrentDirectory + "/layouts");
            FileInfo[] info = dirInfo.GetFiles("*" + fileExtension);
            foreach (FileInfo file in info)
            {
                var config = System.IO.Path.GetFileNameWithoutExtension(file.FullName);
                existingConfigs.Add(config);
                if (!config.Contains('_')) //ignore temps/hotkeys
                {
                    templateList.Items.Add(config);
                }
            }

            //fill list with open applications
            Process[] processes = Process.GetProcesses();
            foreach (Process p in processes)
            {
                string applicationName = p.MainWindowTitle;
                if (String.IsNullOrEmpty(applicationName))
                {
                    continue;
                }

                try
                {
                    if (String.IsNullOrEmpty(p.MainModule.FileVersionInfo.FileDescription))
                    {
                        continue;
                    }
                    applicationName = p.MainModule.FileVersionInfo.FileDescription;
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    applicationName = p.ProcessName;
                }

                currentApplications.Add(applicationName);

                //create item
                var item = new ListViewItem();

                Icon? icon = null;
                try
                {
                    icon = System.Drawing.Icon.ExtractAssociatedIcon(p.MainModule.FileName);
                }
                catch (System.ComponentModel.Win32Exception) { }
                item.Content = new ApplicationListItem(applicationName, icon);
                item.Height = 24;

                //add to lsitview
                applicationList.Items.Add(item);
                if (existingConfigs.Contains(applicationName))
                {
                    item.IsEnabled = false;
                    item.Opacity = 0.3;
                }
            }

            //get list of all applications
            //string uninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            //using RegistryKey rk = Registry.LocalMachine.OpenSubKey(uninstallKey);
            //foreach (string skName in rk.GetSubKeyNames())
            //{
            //    using RegistryKey sk = rk.OpenSubKey(skName);
            //    try
            //    {
            //        var displayName = sk.GetValue("DisplayName");
            //        var size = sk.GetValue("EstimatedSize");

            //        if (displayName != null)
            //        {
            //            allApplications.Add(displayName.ToString());
            //        }
            //    }
            //    catch (Exception) { }
            //}
        }

        //expand/collapse
        //private void ToggleList(object sender, RoutedEventArgs e)
        //{
        //    applicationList.Items.Clear();
        //    List<string> items;
        //    if (isExpanded)
        //    {
        //        items = currentApplications;

        //        textBlock.Text = "Current Applications";
        //        expandImg.Source = new BitmapImage(new Uri(@"/Resources/Expand.png", UriKind.Relative));
        //    }
        //    else
        //    {
        //        items = allApplications;

        //        textBlock.Text = "All Applications";
        //        expandImg.Source = new BitmapImage(new Uri(@"/Resources/Collapse.png", UriKind.Relative));
        //    }

        //    foreach (string item in items)
        //    {
        //        ListViewItem temp = new ListViewItem();
        //        temp.Content = item;
        //        if (existingConfigs.Contains(item))
        //        {
        //            temp.IsEnabled = false;
        //        }
        //        applicationList.Items.Add(temp);
        //    }
        //    isExpanded = !isExpanded;

        //    btnSubmit.IsEnabled = false;
        //    this.SizeToContent = SizeToContent.Width; //TODO; fix
        //}

        private void Submit(object sender, RoutedEventArgs e)
        {
            var listItem = (ListViewItem)applicationList.SelectedItem;
            var item = (ApplicationListItem)listItem.Content;
            string fileName = "layouts/" + item.GetName() + fileExtension;

            if (!File.Exists(fileName))
            {
                if (templateList.SelectedIndex == 0)
                {
                    //create new file
                    File.Create(fileName).Close();
                }
                else
                {
                    //copy file
                    File.Copy("layouts/" + templateList.SelectedItem + fileExtension, fileName);
                }
                submitFunction.Invoke(item.GetName());
                Close();
            }
            else
            {
                //file already exists
            }
        }

        private void applicationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnSubmit.IsEnabled = true;
        }

        public void Exit(object sender, EventArgs e)
        {
            Close();
        }
    }
}
