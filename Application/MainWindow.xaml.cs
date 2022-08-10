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

namespace Keymeleon
{
    public partial class MainWindow : Window
    {
        NativeMethods.WinEventDelegate winEventProcDelegate;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;

        private string cachedApplication;

        static class NativeMethods
        {

            [DllImport("user32.dll")]
            public static extern System.IntPtr SetWinEventHook(uint eventMin, uint eventMax, System.IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
            public delegate void WinEventDelegate(System.IntPtr hWinEventHook, uint eventType, System.IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

            [DllImport("user32.dll")]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out IntPtr ProcessId);

            [DllImport("kym.dll")]
            public static extern int setCustomLayout(string configFileName, int profileToModify);
            [DllImport("kym.dll")]
            public static extern int setActiveProfile(int profile);
        }

        public MainWindow()
        {
            InitializeComponent();

            //cache default config on profile1 (to minimise rewrites of onboard flash)
            if (File.Exists("default.conf"))
            {
                int temp = NativeMethods.setCustomLayout("default.conf", 1);
                Debug.WriteLine(temp);
                temp = NativeMethods.setCustomLayout("default.conf", 2);
                Debug.WriteLine(temp);
            }

            //setup method to handle events (change of focus)
            winEventProcDelegate = new NativeMethods.WinEventDelegate(WinEventProc);
            NativeMethods.SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, System.IntPtr.Zero, winEventProcDelegate, (uint)0, (uint)0, WINEVENT_OUTOFCONTEXT); //begin listening to change of window focus
        
            //TODO; minimise to system tray
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
                focusedApplication = p.MainModule.FileVersionInfo.FileDescription.ToString();
            }
            catch (System.ComponentModel.Win32Exception)
            // if Keymeleon is x86, this exception will throw when trying to access an x64 program (however, it should only be x86 on 32bit CPUs, so there shouldn't be any x64 programs to trigger this) //TODO; add check for this
            // if Keymeleon is not elevated, this exception will throw when trying to access an elevated program //TODO; request that Keymeleon be elevated
            {
                focusedApplication = p.MainWindowTitle.ToString();
            }
            catch (NullReferenceException)
            {
                Debug.WriteLine("nullptr error");
                return;
            }

            Debug.WriteLine(focusedApplication);

            if (File.Exists(focusedApplication + ".conf"))
            {
                if (!focusedApplication.Equals(cachedApplication)) //if config is already cached on profile2, no need to rewrite //TODO; include profile3 for greater cache capacity
                {
                    Debug.WriteLine(NativeMethods.setCustomLayout(focusedApplication + ".conf", 2));
                    cachedApplication = focusedApplication;
                }
                NativeMethods.setActiveProfile(2);

            }
            else
            {
                NativeMethods.setActiveProfile(1); //switch to cached profile1
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            EditorWindow editor = new EditorWindow();
            editor.Show();
        }

    }
}
