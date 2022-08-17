﻿using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Interop;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;

namespace Keymeleon
{
    public partial class MainWindow : Window
    {
        System.IntPtr hWinEvent;
        NativeMethods.WinEventDelegate winEventProcDelegate;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;

        private NotifyIcon nIcon = new NotifyIcon();
        ContextMenuStrip contextMenu;

        ConfigManager configManager;
        private string cachedApplication;

        static class NativeMethods
        {

            [DllImport("user32.dll")]
            public static extern System.IntPtr SetWinEventHook(uint eventMin, uint eventMax, System.IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
            public delegate void WinEventDelegate(System.IntPtr hWinEventHook, uint eventType, System.IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
            [DllImport("user32.dll")]
            public static extern bool UnhookWinEvent(System.IntPtr hWinEventHook);

            [DllImport("user32.dll")]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out IntPtr ProcessId);

            [DllImport("kym.dll")]
            public static extern int SetLayoutBase(string configFileName, int profileToModify);
            [DllImport("kym.dll")]
            public static extern int ApplyLayoutLayer(string configFileName, int profileToModify);
            [DllImport("kym.dll")]
            public static extern int SetActiveProfile(int profile);
        }

        public MainWindow()
        {
            InitializeComponent();

            //cache default config on profile1 (to minimise rewrites of onboard flash)
            if (File.Exists("layouts/Default.base"))
            {
                Debug.WriteLine(NativeMethods.SetLayoutBase("layouts/Default.base", 1));
                Debug.WriteLine(NativeMethods.SetLayoutBase("layouts/Default.base", 2));
            }

            configManager = new ConfigManager();
            configManager.LoadBaseConfig("layouts/Default.base");

            //remove any temp files
            File.Delete("layouts/_temp.conf");

            //minimise to system tray
            Hide();
            nIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath); //TEMP
            nIcon.Text = "Keymeleon";
            nIcon.DoubleClick += new EventHandler(OpenEditor);

            contextMenu = new ContextMenuStrip();
            contextMenu.AutoClose = true;
            nIcon.ContextMenuStrip = contextMenu;

            ToolStripItem ts = new ToolStripMenuItem("Open Editor");
            ts.Click += new EventHandler(OpenEditor);
            ts.BackColor = ColorTranslator.FromHtml("#292929");
            contextMenu.Items.Add(ts);

            ts = new ToolStripMenuItem("Exit");
            ts.Click += new EventHandler(Exit);
            ts.BackColor = ColorTranslator.FromHtml("#292929");
            contextMenu.Items.Add(ts);

            contextMenu.ForeColor = System.Drawing.Color.White;

            //begin
            StartFocusMonitoring();
        }

        private void WinEventProc(System.IntPtr hWinEventHook, uint eventType, System.IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            Debug.WriteLine("Foreground changed");

            //get current focused process
            IntPtr pid = System.IntPtr.Zero;
            NativeMethods.GetWindowThreadProcessId(hwnd, out pid);
            Process p = Process.GetProcessById((int)pid);

            //get name of application that process is owned by
            string focusedApplication;
            try
            {
                focusedApplication = p.MainModule.FileVersionInfo.FileDescription;
            }
            catch (System.ComponentModel.Win32Exception)
            // if Keymeleon is x86, this exception will throw when trying to access an x64 program (however, it should only be x86 on 32bit CPUs, so there shouldn't be any x64 programs to trigger this) //TODO; add check for this
            // if Keymeleon is not elevated, this exception will throw when trying to access an elevated program //TODO; request that Keymeleon be elevated
            {
                focusedApplication = p.MainWindowTitle;
            }
            catch (NullReferenceException)
            {
                Debug.WriteLine("nullptr error");
                return;
            }

            Debug.WriteLine(focusedApplication);

            if (File.Exists("layouts/"+focusedApplication+".conf"))
            {
                if (!focusedApplication.Equals(cachedApplication)) //if config is already cached on profile2, no need to rewrite //TODO; include profile3 for greater cache capacity
                {
                    //set layout to base
                    if (File.Exists("layouts/_temp.conf"))
                    {
                        Debug.WriteLine(NativeMethods.ApplyLayoutLayer("layouts/_temp.conf", 2));
                    }
                    Debug.WriteLine(NativeMethods.ApplyLayoutLayer("layouts/"+focusedApplication+".conf", 2));
                    cachedApplication = focusedApplication;

                    //create temp config to revert to base
                    configManager.LoadLayerConfig("layouts/"+focusedApplication+".conf");
                    var deltaState = configManager.GetStatesDelta();
                    configManager.SaveInverseConfig("layouts/_temp.conf");
                }
                NativeMethods.SetActiveProfile(2);
            }
            else
            {
                NativeMethods.SetActiveProfile(1); //switch to cached profile1
            }
        }

        private void OpenEditor(object sender, EventArgs e)
        {
            nIcon.Visible = false;
            NativeMethods.UnhookWinEvent(hWinEvent); //stop responding to window changes

            EditorWindow editor = new EditorWindow();
            editor.Show();
        }

        public void StartFocusMonitoring()
        {
            if (File.Exists("layouts/Default.base"))
            {
                Debug.WriteLine(NativeMethods.SetLayoutBase("layouts/Default.base", 1));
                Debug.WriteLine(NativeMethods.SetLayoutBase("layouts/Default.base", 2));
            }

            //setup method to handle events (change of focus)
            winEventProcDelegate = new NativeMethods.WinEventDelegate(WinEventProc);
            hWinEvent = NativeMethods.SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, System.IntPtr.Zero, winEventProcDelegate, (uint)0, (uint)0, WINEVENT_OUTOFCONTEXT); //begin listening to change of window focus

            nIcon.Visible = true;
        }

        public void Exit(object sender, EventArgs e)
        {
            NativeMethods.UnhookWinEvent(hWinEvent); //stop responding to window changes
            nIcon.Visible = false;

            //remove any temp files
            File.Delete("layouts/_temp.conf");

            Close();
        }

    }

    //TODO; if screen goes on standby, set keyboard to black
}
