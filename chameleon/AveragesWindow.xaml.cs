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

using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace chameleon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class AveragesWindow : Window
    {
        ImageProcessor processor;

        public AveragesWindow()
        {
            InitializeComponent();

            processor = new();

            var dirInfo = new DirectoryInfo(Environment.CurrentDirectory + "/images");
            FileInfo[] info = dirInfo.GetFiles("*.png");
            foreach (var file in info)
            {
                comboBox.Items.Add(file.Name);
            }
        }

        private void LoadImage(object sender, RoutedEventArgs e)
        {
            if (comboBox.SelectedItem == null)
            {
                displayImage.Source = null;
                return;
            }
            string file = "images/" + comboBox.SelectedItem.ToString();

            var image = new BitmapImage(new Uri("pack://siteoforigin:,,,/../" + file));
            displayImage.Source = image;

            var timer = new System.Diagnostics.Stopwatch();

            Bitmap src = new(file);

            processor.AnalyzeImage(src, 0, modeOutput, meanOutput, rmsOutput, stanTime);
            processor.AnalyzeImage(src, 200, lleModeOutput, lleMeanOutput, lleRmsOutput, lleTime);

            timer.Start();
            processor.ProcessImage(src, 2f);
            timer.Stop();
            double timeToProcess = timer.Elapsed.TotalSeconds;
            ppOutput.Source = ToBitmapImage(src);

            processor.AnalyzeImage(src, 200, ppModeOutput, ppMeanOutput, ppRmsOutput, ppTime, timeToProcess);

            data.Content = src.Width + "x" + src.Height;
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

    }
}
