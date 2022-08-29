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
    public partial class ResolutionsWindow : Window
    {
        ImageProcessor processor;

        public ResolutionsWindow()
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

            timer.Start();
            processor.SaturateImage(src, 2f);
            timer.Stop();
            double timeToProcess = timer.Elapsed.TotalSeconds;

            Bitmap FHDsrc = new Bitmap(src, 1920, 1080);
            Bitmap HDsrc = new Bitmap(src, 1280, 720);
            Bitmap SDsrc = new Bitmap(src, 640, 480);
            Bitmap LDsrc = new Bitmap(src, 240, 135);

            processor.AnalyzeImage(src, 200, nativeModeOutput, nativeMeanOutput, nativeRmsOutput, nativeTime, timeToProcess);
            processor.AnalyzeImage(FHDsrc, 200, FHDModeOutput, FHDMeanOutput, FHDRmsOutput, FHDtime, timeToProcess);
            processor.AnalyzeImage(HDsrc, 200, HDModeOutput, HDMeanOutput, HDRmsOutput, HDtime, timeToProcess);
            processor.AnalyzeImage(SDsrc, 200, SDModeOutput, SDMeanOutput, SDRmsOutput, SDtime, timeToProcess);
            processor.AnalyzeImage(LDsrc, 200, LDModeOutput, LDMeanOutput, LDRmsOutput, LDtime, timeToProcess);

            data.Content = "Native\n" + src.Width + "x" + src.Height;
        }
    }
}
