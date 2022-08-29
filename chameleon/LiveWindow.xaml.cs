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

using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Timers;
using System.Threading;
using System.IO;

namespace chameleon
{
    /// <summary>
    /// Interaction logic for LiveWindow.xaml
    /// </summary>
    public partial class LiveWindow : Window
    {
        ImageProcessor processor;

        public LiveWindow()
        {
            InitializeComponent();

            processor = new();
        }

        private void Capture()
        {
            while (enabled)
            {
                var timer = new System.Diagnostics.Stopwatch();
                timer.Start();

                System.Drawing.Rectangle bounds = Screen.GetBounds(System.Drawing.Point.Empty);
                Bitmap capture = new Bitmap(bounds.Width, bounds.Height);

                using (Graphics g = Graphics.FromImage(capture))
                {
                    g.CopyFromScreen(System.Drawing.Point.Empty, System.Drawing.Point.Empty, bounds.Size);
                }

                //change res
                //capture = new Bitmap(capture, 1280, 720);
                //capture = new Bitmap(capture, 640, 480);
                capture = new Bitmap(capture, 240, 135);

                enableProcess.Dispatcher.Invoke(new Action(() => {
                    if ((bool) enableProcess.IsChecked)
                    {
                        processor.ProcessImage(capture, 2f);
                    }
                }));
                processor.AnalyzeImage(capture, 200, modeOutput, meanOutput, rmsOutput);

                display.Dispatcher.Invoke(new Action(() => {
                    display.Source = ToBitmapImage(capture);
                }));

                timer.Stop();
                timerOut.Dispatcher.Invoke(new Action(() => {
                    timerOut.Content = timer.Elapsed.TotalSeconds + "s\n(" + (int)(1 / timer.Elapsed.TotalSeconds) + "fps)";
                }));
            }
        }

        public static BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        CancellationTokenSource cancelationTokenSource;
        bool enabled = false;

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool) checkbox.IsChecked)
            {
                enabled = true;
                cancelationTokenSource = new CancellationTokenSource();
                new Task(() => Capture(), cancelationTokenSource.Token, TaskCreationOptions.LongRunning).Start();
            }
            else
            {
                cancelationTokenSource.Cancel();
                enabled = false;
            }
            
        }
    }
}
