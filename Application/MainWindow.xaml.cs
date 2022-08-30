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
using System.Drawing.Imaging;

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

        int mode = 1;
        CancellationTokenSource mimicScreenSource;

        // P/INVOKE METHODS ---
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

            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();

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

            ts = new ToolStripMenuItem("Mode 1");
            ts.Click += new EventHandler(SetMode_1);
            ts.BackColor = ColorTranslator.FromHtml("#292929");
            ts.ToolTipText = "Display user-defined layouts depedning on the focused application and keypresses (default mode)";
            //ts.ForeColor = System.Drawing.Color.Green;
            contextMenu.Items.Add(ts);

            ts = new ToolStripMenuItem("Mode 2");
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

        public void StartController()
        {
            if (mode == 1) //ADAPT TO FOREGROUND
            {
                StartFocusMonitoring();
            }
            else if ( mode == 2) //MIMIC SCREEN
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
            }
            else if (mode == 2) //MIMIC SCREEN
            {
                mimicScreenSource.Cancel();
            }
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

            //setup method to handle events (change of focus)
            winEventProcDelegate = new NativeMethods.WinEventDelegate(WinEventProc);
            hWinEvent = NativeMethods.SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, System.IntPtr.Zero, winEventProcDelegate, (uint)0, (uint)0, WINEVENT_OUTOFCONTEXT); //begin listening to change of window focus

            //setup method to handle key events
            hotkeyActive = false;
            var hmod = Marshal.GetHINSTANCE(typeof(Window).Module);
            hWinHook = NativeMethods.SetWindowsHookExA(13, winHookProc, hmod, 0);

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

            registeredHotkeys.Clear();

            int res = 0;

            int oldProfile = profile;
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
                    if (File.Exists("layouts/_" + profile.ToString() + ".conf"))
                    {
                        res += NativeMethods.ApplyLayoutLayer("layouts/_" + profile.ToString() + ".conf", profile);
                    }
                    res += NativeMethods.ApplyLayoutLayer("layouts/"+focusedApplication+".conf", profile);

                    //create temp config to revert to base
                    configManager.LoadLayerConfig("layouts/"+focusedApplication+".conf", 1);
                    var deltaState = configManager.GetStatesDelta(0, 1);
                    configManager.SaveInverseConfig("layouts/_" + profile.ToString() + ".conf", 0, 1);
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

            hotkeyActive = false;
            //undo any active hotkey effect
            if (File.Exists("layouts/_" + oldProfile.ToString() + "a.conf"))
            {
                string temp = "layouts/_" + oldProfile.ToString() + "a.conf";
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
                            string fileName = "layouts/" + focusedApplication + "_" + key + ".conf";
                            configManager.LoadLayerConfig(fileName, 2);
                            configManager.SaveInverseConfig("layouts/_" + profile.ToString() + "a.conf", 1, 2);

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

                            int res = NativeMethods.ApplyLayoutLayer("layouts/_" + profile.ToString() + "a.conf", profile);
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

        // MODE 2: MIMIC SCREEN COLOURS ---
        private void StartScreenMonitoring()
        {
            int res = 0;

            res += NativeMethods.SetActiveProfile(1);
            res += NativeMethods.SetMode(2);

            mimicScreenSource = new CancellationTokenSource();
            new Task(() => MimicScreen(), mimicScreenSource.Token, TaskCreationOptions.LongRunning).Start();
        }

        System.Drawing.Color currentPrimaryColour;

        private void MimicScreen()
        {
            int res;
            while (true)
            {
                //get screen image
                System.Drawing.Rectangle bounds = Screen.GetBounds(System.Drawing.Point.Empty);
                Bitmap capture = new Bitmap(bounds.Width, bounds.Height);

                using (Graphics g = Graphics.FromImage(capture))
                {
                    g.CopyFromScreen(System.Drawing.Point.Empty, System.Drawing.Point.Empty, bounds.Size);
                }

                //resolution
                capture = new Bitmap(capture, 240, 135);

                var averageColour = GetAverageColourOf(capture, 200);
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

        private System.Drawing.Color GetAverageColourOf(Bitmap src, int lowLightExclusion=0)
        {
            //PROCESS IMAGE
            using (Graphics g = Graphics.FromImage(src))
            {
                float c = 3f;
                float t = (1.0f - c) / 2.0f;

                ColorMatrix clrMatrix = new ColorMatrix(new float[][] {
                        new float[] {c,0,0,0,0},
                        new float[] {0,c,0,0,0},
                        new float[] {0,0,c,0,0},
                        new float[] {0,0,0,1,0},
                        new float[] {t,t,t,0,1}
                    });
                ImageAttributes imgAttribs = new ImageAttributes();
                imgAttribs.SetColorMatrix(clrMatrix, ColorMatrixFlag.Default, ColorAdjustType.Default);

                g.DrawImage(src, new System.Drawing.Rectangle(0, 0, src.Width, src.Height),
                    0, 0, src.Width, src.Height, System.Drawing.GraphicsUnit.Pixel, imgAttribs);
            }

            //ANALYSE IMAGE
            int width = src.Width;
            int height = src.Height;

            double[] mean = new double[] { 0, 0, 0 };
            int count = 0;

            //get data
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    System.Drawing.Color pixel = src.GetPixel(x, y);

                    if (pixel.R + pixel.G + pixel.B >= lowLightExclusion)
                    {
                        //mean
                        mean[0] += pixel.R;
                        mean[1] += pixel.G;
                        mean[2] += pixel.B;

                        count += 1;
                    }
                }
            }

            //process data
            if (count > 0)
            {
                //mean
                for (int i = 0; i < 3; i++)
                {
                    mean[i] /= count;
                }
            }

            return System.Drawing.Color.FromArgb(255, (int)mean[0], (int)mean[1], (int)mean[2]);
        }

        // UI INTERFACE ---
        private void OpenEditor(object sender, EventArgs e)
        {
            nIcon.Visible = false;
            StopController();

            int res = NativeMethods.SetActiveProfile(1);
            int res2 = NativeMethods.SetMode(1); //custom layout
            if (res < 0 || res2 <0)
            {
                OnError();
                return;
            }

            EditorWindow editor = new();
            editor.Show();
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

            //remove any temp files
            var dirInfo = new DirectoryInfo(Environment.CurrentDirectory + "/layouts");
            var info = dirInfo.GetFiles("_*.conf");
            foreach (var file in info)
            {
                File.Delete(file.FullName);
            }

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
        }

    }

    //TODO; if screen goes on standby, set keyboard to black
}
