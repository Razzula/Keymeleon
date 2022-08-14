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

namespace Keymeleon
{
    public partial class ApplicationSelector : Window
    {
        EditorWindow editor;

        List<string> existingConfigs = new List<string>();
        List<string> currentApplications = new List<string>();
        List<string> allApplications = new List<string>();

        bool isExpanded = false;

        public ApplicationSelector(EditorWindow owner)
        {
            InitializeComponent();
            editor = owner;

            //get all exist configs
            var dirInfo = new DirectoryInfo(Environment.CurrentDirectory + "/layouts");
            FileInfo[] info = dirInfo.GetFiles("*.conf");
            foreach (FileInfo file in info)
            {
                existingConfigs.Add(System.IO.Path.GetFileNameWithoutExtension(file.FullName));
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
                catch (System.ComponentModel.Win32Exception) { }

                currentApplications.Add(applicationName);

                //create item
                ListViewItem item = new ListViewItem();
                item.Content = applicationName;

                //add to lsitview
                applicationList.Items.Add(item);
                if (existingConfigs.Contains(applicationName))
                {
                    item.IsEnabled = false;
                }
            }

            //get list of all applications
            string uninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(uninstallKey))
            {
                foreach (string skName in rk.GetSubKeyNames())
                {
                    using (RegistryKey sk = rk.OpenSubKey(skName))
                    {
                        try
                        {
                            var displayName = sk.GetValue("DisplayName");
                            var size = sk.GetValue("EstimatedSize");

                            if (displayName != null)
                            {
                                allApplications.Add(displayName.ToString());
                            }
                        }
                        catch (Exception ex)
                        { }
                    }
                }
            }
        }

        //expand/collapse
        private void ToggleList(object sender, RoutedEventArgs e)
        {
            applicationList.Items.Clear();
            List<string> items;
            if (isExpanded)
            {
                items = currentApplications;

                textBlock.Text = "Current Applications";
                expandImg.Source = new BitmapImage(new Uri(@"/Resources/Expand.png", UriKind.Relative));
            }
            else
            {
                items = allApplications;

                textBlock.Text = "All Applications";
                expandImg.Source = new BitmapImage(new Uri(@"/Resources/Collapse.png", UriKind.Relative));
            }

            foreach (string item in items)
            {
                ListViewItem temp = new ListViewItem();
                temp.Content = item;
                if (existingConfigs.Contains(item))
                {
                    temp.IsEnabled = false;
                }
                applicationList.Items.Add(temp);
            }
            isExpanded = !isExpanded;

            btnSubmit.IsEnabled = false;
            this.SizeToContent = SizeToContent.Width; //TODO; fix
        }

        private void Submit(object sender, RoutedEventArgs e)
        {
            ListViewItem item = (ListViewItem) applicationList.SelectedItem;
            string fileName = "layouts/"+item.Content+".conf";
            if (fileName.EndsWith(".conf") || fileName.EndsWith(".base"))
            {
                if (!File.Exists(fileName))
                {
                    editor.CreateConfig(item.Content+".conf");
                    Close();
                }
                else
                {
                    //file already exists
                }
            }
            else
            {
                //invalid name
            }
        }

        private void applicationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnSubmit.IsEnabled = true;
        }
    }
}
