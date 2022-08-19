/*
    Copyright (C) 2022  Jack Gillespie  https://github.com/Razzula/Keymeleon/blob/main/LICENSE.md

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

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
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Interop;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;
using System.Threading;
using System.Reflection;

namespace Keymeleon
{
    public partial class MainWindow : Window
    {
        System.IntPtr hWinEvent;
        NativeMethods.WinEventDelegate winEventProcDelegate;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;

        System.IntPtr hWinHook;
        private delegate int HookProc(int code, IntPtr wParam, IntPtr lParam);

        readonly HookProc winHookProc;

        private readonly NotifyIcon nIcon = new();
        readonly ContextMenuStrip contextMenu;
        readonly ConfigManager configManager;
        private string focusedApplication;
        private string cachedApplication;
        readonly Dictionary<string, uint> keycodes = new()
        {
            { "LShift", 0xA0 },
            { "RShift", 0xA1 },
            { "LCtrl", 0xA2 },
            { "RCtrl", 0xA3 },
            { "Alt", 0xA4 }
        };
        readonly List<int> registeredHotkeys = new();
        bool hotkeyActive = false;

        static class NativeMethods
        {

            [DllImport("user32.dll")]
            public static extern System.IntPtr SetWinEventHook(uint eventMin, uint eventMax, System.IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
            public delegate void WinEventDelegate(System.IntPtr hWinEventHook, uint eventType, System.IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
            [DllImport("user32.dll")]
            public static extern bool UnhookWinEvent(System.IntPtr hWinEventHook);

            [DllImport("user32.dll")]
            public static extern IntPtr SetWindowsHookExA(int idHook, HookProc lpfn, IntPtr hmod, uint dwThreadId);
            [DllImport("user32.dll")]
            public static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
            [DllImport("user32.dll")]
            public static extern bool UnhookWindowsHookEx(IntPtr hhk);

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
            if (!Environment.CurrentDirectory.Equals(AppDomain.CurrentDomain.BaseDirectory))
            {
                Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory; //if program started by startup registry, the currentDir will be system32. This ensures the program can access its data.
            }

            InitializeComponent();

            int res = NativeMethods.SetActiveProfile(1);
            if (res < 0)
            {
                var dialog = new PopupDialog("Error", "Could not connect to keyboard.\nPlease reconnect the device and try again.");
                dialog.ShowDialog();
                Close();
            }

            //SETUP
            configManager = new ConfigManager();
            if (!File.Exists("layouts/Default.base"))
            {
                configManager.SaveBaseConfig("layouts/Default.base");
            }

            //remove any temp files
            var dirInfo = new DirectoryInfo(Environment.CurrentDirectory + "/layouts");
            var info = dirInfo.GetFiles("_*.conf");
            foreach (var file in info)
            {
                File.Delete(file.FullName);
            }

            //START
            //minimise to system tray
            Hide();
            nIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
            nIcon.Text = "Keymeleon";
            nIcon.DoubleClick += new EventHandler(OpenEditor);

            contextMenu = new ContextMenuStrip
            {
                AutoClose = true
            };
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

            winHookProc = new HookProc(WinHookProc);

            //begin
            //if (Debugger.IsAttached) { Properties.Settings.Default.Reset(); } //DEBUG
            if (Properties.Settings.Default.FirstRun) //first time setup
            {
                var editor = new EditorWindow();
                editor.Show();
                var settingsMenu = new Settings();
                settingsMenu.Owner = editor;
                settingsMenu.ShowDialog();

                Properties.Settings.Default.FirstRun = false;
                Properties.Settings.Default.Save();
            }
            else
            {
                StartFocusMonitoring();
            }
        }

        private void WinEventProc(System.IntPtr hWinEventHook, uint eventType, System.IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {

            //get current focused process
            IntPtr pid = System.IntPtr.Zero;
            NativeMethods.GetWindowThreadProcessId(hwnd, out pid);
            Process p = Process.GetProcessById((int)pid);

            //get name of application that process is owned by
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

            registeredHotkeys.Clear();

            if (File.Exists("layouts/"+focusedApplication+".conf")) //is there a layer to apply
            {
                //register hotkeys
                var dirInfo = new DirectoryInfo(Environment.CurrentDirectory + "/layouts");
                var info = dirInfo.GetFiles(focusedApplication+"_*.conf");
                foreach (var file in info)
                {
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(file.FullName);
                    string keyname = fileName.Substring(fileName.IndexOf('_') + 1);
                    try
                    {
                        registeredHotkeys.Add((int)keycodes[keyname]);
                    }
                    catch (KeyNotFoundException) { }
                }

                //apply layer to keyboard
                if (!focusedApplication.Equals(cachedApplication)) //if config is already cached on profile2, no need to rewrite //TODO; include profile3 for greater cache capacity
                {
                    //set layout to base
                    if (File.Exists("layouts/_1.conf"))
                    {
                        NativeMethods.ApplyLayoutLayer("layouts/_1.conf", 2);
                    }
                    NativeMethods.ApplyLayoutLayer("layouts/"+focusedApplication+".conf", 2);
                    cachedApplication = focusedApplication;

                    //create temp config to revert to base
                    configManager.LoadLayerConfig("layouts/"+focusedApplication+".conf", 1);
                    var deltaState = configManager.GetStatesDelta(0, 1);
                    configManager.SaveInverseConfig("layouts/_1.conf", 0, 1);
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
            NativeMethods.UnhookWindowsHookEx(hWinHook); //stop responding to keypresses

            EditorWindow editor = new();
            editor.Show();
        }

        public void StartFocusMonitoring()
        {
            if (!File.Exists("layouts/Default.base"))
            {
                configManager.SaveBaseConfig("layouts/Default.base");
            }
            NativeMethods.SetLayoutBase("layouts/Default.base", 1);

            if (File.Exists("layouts/"+Properties.Settings.Default.AltBase+".base"))
            {
                NativeMethods.SetLayoutBase("layouts/" + Properties.Settings.Default.AltBase + ".base", 2);
                configManager.LoadBaseConfig("layouts/" + Properties.Settings.Default.AltBase + ".base");
                cachedApplication = null;
            } else
            {
                NativeMethods.SetLayoutBase("layouts/Default.base", 2);
                configManager.LoadBaseConfig("layouts/Default.base");
            }

            //setup method to handle events (change of focus)
            winEventProcDelegate = new NativeMethods.WinEventDelegate(WinEventProc);
            hWinEvent = NativeMethods.SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, System.IntPtr.Zero, winEventProcDelegate, (uint)0, (uint)0, WINEVENT_OUTOFCONTEXT); //begin listening to change of window focus

            //setup method to handle key events
            var hmod = Marshal.GetHINSTANCE(typeof(Window).Module);
            hWinHook = NativeMethods.SetWindowsHookExA(13, winHookProc, hmod, 0);

            nIcon.Visible = true;
        }

        public void Exit(object sender, EventArgs e)
        {
            nIcon.Visible = false;
            NativeMethods.UnhookWinEvent(hWinEvent); //stop responding to window changes
            NativeMethods.UnhookWindowsHookEx(hWinHook); //stop responding to keypresses

            //remove any temp files
            var dirInfo = new DirectoryInfo(Environment.CurrentDirectory + "/layouts");
            var info = dirInfo.GetFiles("_*.conf");
            foreach (var file in info)
            {
                File.Delete(file.FullName);
            }

            Close();
        }

        int WinHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            int keycode = Marshal.ReadInt32(lParam);

            if (registeredHotkeys.Contains(keycode)) //check if registered
            {
                int msg = wParam.ToInt32();
                switch (msg)
                {
                    case 256:
                        if (!hotkeyActive)
                        {
                            string key = keycodes.FirstOrDefault(x => x.Value == keycode).Key;
                            string fileName = "layouts/" + cachedApplication + "_" + key + ".conf";
                            configManager.LoadLayerConfig(fileName, 2);
                            configManager.SaveInverseConfig("layouts/_2.conf", 1, 2);
                            NativeMethods.ApplyLayoutLayer(fileName, 2);

                            hotkeyActive = true;
                        }
                        break;
                    case 257:
                        if (hotkeyActive)
                        {
                            NativeMethods.ApplyLayoutLayer("layouts/_2.conf", 2); //TEMP

                            hotkeyActive = false;
                        }
                        break;
                }
            }
            //TODO; make above async
            return NativeMethods.CallNextHookEx(hWinHook, nCode, wParam, lParam);
        }
    }

    //TODO; if screen goes on standby, set keyboard to black
}
