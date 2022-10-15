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
using System.Threading.Tasks;
using System.Windows;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Interop;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Windows.Resources;

namespace Keymeleon
{
    public partial class MainWindow : Window
    {
        // GLOBAL VARIABLES ---
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
        private Dictionary<string, int> cachedApplications = new();
        private string leastRecentlyUsedCacheEntry;
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
        int profile;

        HwndSource hwndSource;

        int mode = 1;
        bool mimicScreenActive = false;
        bool autocolourActive = false;

        ToolStripItem tsOld;

        // P/INVOKE METHODS ---
        static class NativeMethods
        {
            //foreground detection
            [DllImport("user32.dll")]
            public static extern System.IntPtr SetWinEventHook(uint eventMin, uint eventMax, System.IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
            public delegate void WinEventDelegate(System.IntPtr hWinEventHook, uint eventType, System.IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
            [DllImport("user32.dll")]
            public static extern bool UnhookWinEvent(System.IntPtr hWinEventHook);

            //keypress detection
            [DllImport("user32.dll")]
            public static extern IntPtr SetWindowsHookExA(int idHook, HookProc lpfn, IntPtr hmod, uint dwThreadId);
            [DllImport("user32.dll")]
            public static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
            [DllImport("user32.dll")]
            public static extern bool UnhookWindowsHookEx(IntPtr hhk);

            [DllImport("user32.dll")]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out IntPtr ProcessId);

            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();

            //global hotkeys
            [DllImport("user32.dll")]
            public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
            [DllImport("user32.dll")]
            public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

            //keyboard controls
            [DllImport("kym.dll")]
            public static extern int SetLayoutBase(string configFileName, int profileToModify);
            [DllImport("kym.dll")]
            public static extern int ApplyLayoutLayer(string configFileName, int profileToModify);
            [DllImport("kym.dll")]
            public static extern int SetActiveProfile(int profile);
            [DllImport("kym.dll")]
            public static extern int SetMode(int mode); //1: CUSTOM, 2: FIXED
            [DllImport("kym.dll")]
            public static extern int SetPrimaryColour(int r, int g, int b);
            [DllImport("kym.dll")]
            public static extern int SetKeyColour(string keycode, int r, int g, int b, int profile);

        }

        // INITIALISATION ---
        public MainWindow()
        {
            if (!Environment.CurrentDirectory.Equals(AppDomain.CurrentDomain.BaseDirectory))
            {
                Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory; //if program started by startup registry, the currentDir will be system32. This ensures the program can access its data.
            }

            //ensure single-instance
            string procName = Process.GetCurrentProcess().ProcessName;
            Process[] processes = Process.GetProcessesByName(procName);

            if (processes.Length > 1)
            {
                var dialog = new PopupDialog("Error", "An instance of Keymeleon is already running.");
                dialog.ShowDialog();
                System.Windows.Application.Current.Shutdown();
                return;
            }

            //
            if (Environment.Is64BitOperatingSystem)
            {
                if (!Environment.Is64BitProcess)
                {
                    var dialog = new PopupDialog("Warning", "32-bit version of Keymeleon running on a 64-bit OS.\nThis may lead to issues. \n\nSee https://github.com/Razzula/Keymeleon/releases");
                    dialog.ShowDialog();
                }
            }

            //test keyboard
            int res = NativeMethods.SetActiveProfile(1);
            if (res < 0)
            {
                var dialog = new PopupDialog("Error", "Could not connect to keyboard.\nPlease reconnect the device and try again.");
                dialog.ShowDialog();
                System.Windows.Application.Current.Shutdown();
                return;
            }
            profile = 1;

            //SETUP
            InitializeComponent();

            configManager = new ConfigManager();
            if (!File.Exists("layouts/Default.base"))
            {
                configManager.SaveBaseConfig("layouts/Default.base");
            }

            //remove any temp files
            var dirInfo = new DirectoryInfo(Environment.CurrentDirectory + "/layouts");
            var info = dirInfo.GetFiles("_*.layer");
            foreach (var file in info)
            {
                File.Delete(file.FullName);
            }

            //START
            //minimise to system tray
            nIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
            nIcon.Text = "Keymeleon";
            nIcon.DoubleClick += new EventHandler(OpenEditor);

            contextMenu = new ContextMenuStrip
            {
                AutoClose = true
            };
            contextMenu.ForeColor = System.Drawing.Color.Yellow;
            nIcon.ContextMenuStrip = contextMenu;

            ToolStripItem ts = new ToolStripMenuItem("Open Editor");
            ts.Click += new EventHandler(OpenEditor);
            ts.BackColor = ColorTranslator.FromHtml("#292929");
            contextMenu.Items.Add(ts);

            StreamResourceInfo sri = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Resources/CheckBoxChecked_Dark.png"));
            var checkboxImg = System.Drawing.Image.FromStream(sri.Stream);

            ts = new ToolStripMenuItem("Mode 1", checkboxImg);
            ts.Click += new EventHandler(SetMode_1);
            ts.BackColor = ColorTranslator.FromHtml("#292929");
            ts.ToolTipText = "Display user-defined layouts depedning on the focused application and keypresses (default mode)";
            contextMenu.Items.Add(ts);
            tsOld = ts;

            sri = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Resources/CheckBoxUnchecked_Dark.png"));
            checkboxImg = System.Drawing.Image.FromStream(sri.Stream);

            ts = new ToolStripMenuItem("Mode 2", checkboxImg);
            ts.Click += new EventHandler(SetMode_2);
            ts.BackColor = ColorTranslator.FromHtml("#292929");
            ts.ToolTipText = "Mimic the screen's average colour (designed for films/games)";
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
                StartController();
            }
        }

        private void OnStart(object sender, RoutedEventArgs e)
        {
            //create globalhotkey
            IntPtr handle = new WindowInteropHelper(this).Handle;
            hwndSource = HwndSource.FromHwnd(handle);
            hwndSource.AddHook(HwndHook);

            NativeMethods.RegisterHotKey(handle, 117, 0x01, 0x4B); //ALT + K
            NativeMethods.RegisterHotKey(handle, 171, 0x03, 0x4B); //CTRL + ALT + K

            //minimise to system tray
            Hide();
        }

        public void StartController()
        {
            if (mode == 1) //ADAPT TO FOREGROUND
            {
                StartFocusMonitoring();
            }
            else if (mode == 2) //MIMIC SCREEN
            {
                StartScreenMonitoring();
            }
        }

        public void StopController()
        {
            if (mode == 1) //ADAPT TO FOREGROUND
            {
                NativeMethods.UnhookWinEvent(hWinEvent); //stop responding to window changes
                NativeMethods.UnhookWindowsHookEx(hWinHook); //stop responding to keypresses
                autocolourActive = false;
            }
            else if (mode == 2) //MIMIC SCREEN
            {
                mimicScreenActive = false;
            }
        }

        //global hotkey to open editor
        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case 117: //ALT + K //open editor

                            if (nIcon.Visible)
                            {
                                OpenEditor(null);
                            }
                            handled = true;
                            break;

                        case 171: //CTRL + ALT + K //open autokey editor

                            if (nIcon.Visible)
                            {
                                //get screen image
                                System.Drawing.Rectangle bounds = Screen.GetBounds(System.Drawing.Point.Empty);
                                Bitmap src = new Bitmap(bounds.Width, bounds.Height);
                                using (Graphics g = Graphics.FromImage(src))
                                {
                                    g.CopyFromScreen(System.Drawing.Point.Empty, System.Drawing.Point.Empty, bounds.Size);
                                }

                                OpenEditor(focusedApplication, src);
                            }
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        // MODE 1: ADAPT TO FOREGROUND SOFTWARE ---
        // setup monitoring
        private void StartFocusMonitoring()
        {
            int res = 0;

            res += NativeMethods.SetMode(1);

            if (!File.Exists("layouts/Default.base")) //if no default base, create one
            {
                configManager.SaveBaseConfig("layouts/Default.base");
            }
            res += NativeMethods.SetLayoutBase("layouts/Default.base", 1);

            if (File.Exists("layouts/" + Properties.Settings.Default.AltBase + ".base")) //if custom base exists, use for non-defaults
            {
                res += NativeMethods.SetLayoutBase("layouts/" + Properties.Settings.Default.AltBase + ".base", 2);
                res += NativeMethods.SetLayoutBase("layouts/" + Properties.Settings.Default.AltBase + ".base", 3);
                configManager.LoadBaseConfig("layouts/" + Properties.Settings.Default.AltBase + ".base");
                cachedApplications.Clear();
            }
            else
            {
                res += NativeMethods.SetLayoutBase("layouts/Default.base", 2);
                res += NativeMethods.SetLayoutBase("layouts/Default.base", 3);
                configManager.LoadBaseConfig("layouts/Default.base");
            }

            //error handling
            Debug.WriteLine(res);
            if (res < 0)
            {
                OnError();
            }

            //get current window
            var hwnd = NativeMethods.GetForegroundWindow();
            WinEventProc(IntPtr.Zero, 0, hwnd, 0, 0, 0, 0);

            //FOCUS
            //setup method to handle events (change of focus)
            winEventProcDelegate = new NativeMethods.WinEventDelegate(WinEventProc);
            hWinEvent = NativeMethods.SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, System.IntPtr.Zero, winEventProcDelegate, (uint)0, (uint)0, WINEVENT_OUTOFCONTEXT); //begin listening to change of window focus

            nIcon.Visible = true;
        }

        // respond to foreground changes
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
            // if Keymeleon is x86, this exception will throw when trying to access an x64 program (however, it should only be x86 on 32bit CPUs, so there shouldn't be any x64 programs to trigger this)
            // if Keymeleon is not elevated, this exception will throw when trying to access an elevated program //TODO; request that Keymeleon be elevated
            {
                focusedApplication = p.ProcessName;
            }
            catch (NullReferenceException)
            {
                Debug.WriteLine("nullptr error");
                return;
            }

            Debug.WriteLine(focusedApplication);

            autocolourActive = false;

            //deregister hotkeys
            registeredHotkeys.Clear();
            NativeMethods.UnhookWindowsHookEx(hWinHook); //stop responding to keypresses


            int res = 0;

            int oldProfile = profile;
            if (File.Exists("layouts/" + focusedApplication + ".layer")) //is there a layer to apply
            {
                //register hotkeys
                var dirInfo = new DirectoryInfo(Environment.CurrentDirectory + "/layouts");
                var info = dirInfo.GetFiles(focusedApplication + "_*.layer");

                if (info.Length > 0)
                {
                    //setup method to handle key events
                    hotkeyActive = false;
                    var hmod = Marshal.GetHINSTANCE(typeof(Window).Module);
                    hWinHook = NativeMethods.SetWindowsHookExA(13, winHookProc, hmod, 0);
                }
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
                int profile;
                if (cachedApplications.ContainsKey(focusedApplication)) //if config is already cached
                {
                    profile = cachedApplications[focusedApplication];
                }
                else
                {
                    if (cachedApplications.Count < 2) //if cache has space
                    {
                        profile = cachedApplications.Count + 2;
                        cachedApplications.Add(focusedApplication, profile);
                    }
                    else
                    {
                        //overide LRU entry
                        profile = cachedApplications[leastRecentlyUsedCacheEntry];
                        cachedApplications.Remove(leastRecentlyUsedCacheEntry);
                        cachedApplications.Add(focusedApplication, profile);
                    }

                    //set layout to base
                    if (File.Exists("layouts/_" + profile.ToString() + ".layer"))
                    {
                        res += NativeMethods.ApplyLayoutLayer("layouts/_" + profile.ToString() + ".layer", profile);
                    }
                    res += NativeMethods.ApplyLayoutLayer("layouts/" + focusedApplication + ".layer", profile);

                    //create temp config to revert to base
                    configManager.LoadLayerConfig("layouts/" + focusedApplication + ".layer", 1);
                    var deltaState = configManager.GetStatesDelta(0, 1);
                    configManager.SaveInverseConfig("layouts/_" + profile.ToString() + ".layer", 0, 1);
                }
                res += NativeMethods.SetActiveProfile(profile);
                this.profile = profile;

                //track LRU
                foreach (var application in cachedApplications)
                {
                    if (!application.Key.Equals(focusedApplication))
                    {
                        leastRecentlyUsedCacheEntry = application.Key;
                        break;
                    }
                }
                if (leastRecentlyUsedCacheEntry == null) //cache only holds one item
                {
                    leastRecentlyUsedCacheEntry = focusedApplication;
                }

            }
            else
            {
                res += NativeMethods.SetActiveProfile(1); //switch to default base
                profile = 1;
            }

            //AUTOKEYS
            if (File.Exists("layouts/" + focusedApplication + ".conf")) //if there is an autokey config
            {
                autocolourActive = true;
                var autoColourKeysSource = new CancellationTokenSource();
                new Task(() => AutoColourKeys(), autoColourKeysSource.Token, TaskCreationOptions.LongRunning).Start();
            }

            //HOTKEY
            hotkeyActive = false;
            //undo any active hotkey effect
            if (File.Exists("layouts/_" + oldProfile.ToString() + "a.layer"))
            {
                string temp = "layouts/_" + oldProfile.ToString() + "a.layer";
                res += NativeMethods.ApplyLayoutLayer(temp, oldProfile);
                File.Delete(temp);
            }

            //error handling
            Debug.WriteLine(res);
            if (res < 0)
            {
                OnError();
            }
        }

        // respond to keypresses
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
                            hotkeyActive = true;

                            string key = keycodes.FirstOrDefault(x => x.Value == keycode).Key;
                            string fileName = "layouts/" + focusedApplication + "_" + key + ".layer";
                            configManager.LoadLayerConfig(fileName, 2);
                            configManager.SaveInverseConfig("layouts/_" + profile.ToString() + "a.layer", 1, 2);

                            int res = NativeMethods.ApplyLayoutLayer(fileName, profile);
                            //error handling
                            Debug.WriteLine(res);
                            if (res < 0)
                            {
                                OnError();
                            }
                        }
                        break;

                    case 257:
                        if (hotkeyActive)
                        {
                            hotkeyActive = false;

                            int res = NativeMethods.ApplyLayoutLayer("layouts/_" + profile.ToString() + "a.layer", profile);
                            //error handling
                            Debug.WriteLine(res);
                            if (res < 0)
                            {
                                OnError();
                            }
                        }
                        break;
                }
            }

            return NativeMethods.CallNextHookEx(hWinHook, nCode, wParam, lParam);
        }

        //autocolour keys
        private class AutoColourConfig
        {
            public string keyID = "";
            public Double originX;
            public Double originY;
            public Double targetX;
            public Double targetY;
            public System.Drawing.Color currentColour = new();
        }
        private void AutoColourKeys()
        {
            List<AutoColourConfig> autoKeys = new();

            //read file
            string fileName = "layouts/" + focusedApplication + ".conf";

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
                //fill dictionary
                string[] lineData = line.Split(" ");
                if (lineData.Length >= 5)
                {
                    var keyConfig = new AutoColourConfig();
                    keyConfig.keyID = lineData[0];
                    keyConfig.originX = Convert.ToDouble(lineData[1]);
                    keyConfig.originY = Convert.ToDouble(lineData[2]);
                    keyConfig.targetX = Convert.ToDouble(lineData[3]);
                    keyConfig.targetY = Convert.ToDouble(lineData[4]);
                    autoKeys.Add(keyConfig);
                }
            }

            //logic
            int res;
            ScreenColourCalculator screenColourCalculator = new();

            while (autocolourActive)
            {
                Bitmap src = screenColourCalculator.GetScreenImage();

                foreach (var autoKey in autoKeys)
                {
                    var averageColour = screenColourCalculator.GetAverageColourOf(src, autoKey.originX, autoKey.originY, autoKey.targetX, autoKey.targetY, 200);

                    if (Math.Abs(autoKey.currentColour.R - averageColour.R) + //only write is colour is different
                        Math.Abs(autoKey.currentColour.G - averageColour.G) +
                        Math.Abs(autoKey.currentColour.B - averageColour.B) > 125)
                    {
                        res = NativeMethods.SetKeyColour(autoKey.keyID, averageColour.R, averageColour.G, averageColour.B, profile);

                        if (res < 0)
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                OnError();
                            });
                            break;
                        }

                        autoKey.currentColour = averageColour;
                    }
                }

                src.Dispose(); //remove image from memory
            }
        }

        // MODE 2: MIMIC SCREEN COLOURS ---
        private void StartScreenMonitoring()
        {
            int res = 0;

            res += NativeMethods.SetActiveProfile(1);
            res += NativeMethods.SetMode(2);

            mimicScreenActive = true;
            var mimicScreenSource = new CancellationTokenSource();
            new Task(() => MimicScreen(), mimicScreenSource.Token, TaskCreationOptions.LongRunning).Start();
        }


        private void MimicScreen()
        {
            int res;
            System.Drawing.Color currentPrimaryColour = new();
            ScreenColourCalculator screenColourCalculator = new();

            while (mimicScreenActive)
            {
                Bitmap src = screenColourCalculator.GetScreenImage(240, 135);
                var averageColour = screenColourCalculator.GetAverageColourOf(src, 0, 0, 1, 1, 200);
                src.Dispose(); //remove image from memory

                if (Math.Abs(currentPrimaryColour.R - averageColour.R) + //only write is colour is different
                    Math.Abs(currentPrimaryColour.G - averageColour.G) +
                    Math.Abs(currentPrimaryColour.B - averageColour.B) > 125)
                {
                    res = NativeMethods.SetPrimaryColour(averageColour.R, averageColour.G, averageColour.B);

                    if (res < 0)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            OnError();
                        });
                        break;
                    }

                    currentPrimaryColour = averageColour;
                }

            }
        }

        // UI INTERFACE ---
        private void OpenEditor(object sender, EventArgs e) //wrapper
        {
            OpenEditor(null);
        }

        private void OpenEditor(string? currentApplication, Bitmap? src = null)
        {
            nIcon.Visible = false;
            StopController();

            int res = NativeMethods.SetActiveProfile(1);
            int res2 = NativeMethods.SetMode(1); //custom layout
            if (res < 0 || res2 < 0)
            {
                OnError();
                return;
            }

            EditorWindow editor = new();
            editor.Show();

            if (currentApplication != null)
            {
                editor.OpenZoneMarker(currentApplication, src);
            }
        }

        private void OnError()
        {
            nIcon.Visible = false;
            StopController();

            var dialog = new PopupDialog("Error", "Could not write to keyboard.\nPlease reconnect the device and restart Keymeleon.");
            dialog.ShowDialog();
            System.Windows.Application.Current.Shutdown();
            Close();
        }

        public void Exit(object sender, EventArgs e)
        {
            nIcon.Visible = false;
            StopController();

            //deregister hotkeys
            IntPtr handle = new WindowInteropHelper(this).Handle;
            NativeMethods.UnregisterHotKey(handle, 117); //ALT + K
            NativeMethods.UnregisterHotKey(handle, 171); //CTRL + ALT + K

            //remove any temp files
            var dirInfo = new DirectoryInfo(Environment.CurrentDirectory + "/layouts");
            var info = dirInfo.GetFiles("_*.layer");
            foreach (var file in info)
            {
                File.Delete(file.FullName);
            }

            NativeMethods.SetActiveProfile(1);

            Close();
        }

        private void SetMode_1(object sender, EventArgs e)
        {
            if (mode == 1)
            {
                return;
            }
            StopController();
            mode = 1;
            StartController();

            //ui
            var activeItem = (ToolStripItem)sender;
            var checkImg = tsOld.Image;

            tsOld.Image = activeItem.Image;
            activeItem.Image = checkImg;

            tsOld = activeItem;
        }

        private void SetMode_2(object sender, EventArgs e)
        {
            if (mode == 2)
            {
                return;
            }

            StopController();
            mode = 2;
            StartController();

            //ui
            var activeItem = (ToolStripItem)sender;
            var checkImg = tsOld.Image;

            tsOld.Image = activeItem.Image;
            activeItem.Image = checkImg;

            tsOld = activeItem;
        }

    }

    //TODO; if screen goes on standby, set keyboard to black
}
