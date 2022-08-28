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
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

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

            AnalyzeImage(src, 0, modeOutput, meanOutput, rmsOutput, stanTime);
            AnalyzeImage(src, 200, lleModeOutput, lleMeanOutput, lleRmsOutput, lleTime);

            timer.Start();
            Bitmap processedSrc = SaturateImage(src, 2f);
            timer.Stop();
            double timeToProcess = timer.Elapsed.TotalSeconds;
            ppOutput.Source = ToBitmapImage(processedSrc);

            AnalyzeImage(processedSrc, 200, ppModeOutput, ppMeanOutput, ppRmsOutput, ppTime, timeToProcess);

            data.Content = src.Width + "x" + src.Height;
        }

        private Bitmap SaturateImage(Bitmap src, float saturation)
        {
            Graphics gr = Graphics.FromImage(src);

            float rWeight = 0.3333f;
            float gWeight = 0.3333f;
            float bWeight = 0.3333f;

            float a = (1.0f - saturation) * rWeight + saturation;
            float b = (1.0f - saturation) * rWeight;
            float c = (1.0f - saturation) * rWeight;
            float d = (1.0f - saturation) * gWeight;
            float e = (1.0f - saturation) * gWeight + saturation;
            float f = (1.0f - saturation) * gWeight;
            float g = (1.0f - saturation) * bWeight;
            float h = (1.0f - saturation) * bWeight;
            float i = (1.0f - saturation) * bWeight + saturation;

            float[][] ptsArray = {
                new float[] {a,  b,  c,  0, 0},
                new float[] {d,  e,  f,  0, 0},
                new float[] {g,  h,  i,  0, 0},
                new float[] {0,  0,  0,  1, 0},
                new float[] {0, 0, 0, 0, 1}
            };

            ColorMatrix clrMatrix = new ColorMatrix(ptsArray);
            ImageAttributes imgAttribs = new ImageAttributes();

            // Set color matrix
            imgAttribs.SetColorMatrix(clrMatrix, ColorMatrixFlag.Default, ColorAdjustType.Default);

            gr.DrawImage(src, new System.Drawing.Rectangle(0, 0, src.Width, src.Height),
                         0, 0, src.Width, src.Height, System.Drawing.GraphicsUnit.Pixel, imgAttribs);

            return src;
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

        private void AnalyzeImage(Bitmap src, int lowLightCutoff, System.Windows.Shapes.Rectangle modeOut, System.Windows.Shapes.Rectangle meanOut, System.Windows.Shapes.Rectangle rmsOut, Label timerOut, double timerMod = 0)
        {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            int width = src.Width;
            int height = src.Height;

            double[] mean = new double[] { 0, 0, 0 };
            double[] rms = new double[] { 0, 0, 0 };
            var mode = new Dictionary<System.Drawing.Color, int>();
            int count = 0;

            //get data
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    System.Drawing.Color pixel = src.GetPixel(x, y);

                    if (pixel.R + pixel.G + pixel.B >= lowLightCutoff)
                    {
                        //mean
                        mean[0] += pixel.R;
                        mean[1] += pixel.G;
                        mean[2] += pixel.B;

                        rms[0] += pixel.R * pixel.R;
                        rms[1] += pixel.G * pixel.G;
                        rms[2] += pixel.B * pixel.B;

                        count += 1;

                        //mode
                        if (mode.ContainsKey(pixel))
                        {
                            mode[pixel] += 1;
                        }
                        else
                        {
                            mode.Add(pixel, 1);
                        }
                    }
                }
            }

            //process data
            for (int i = 0; i < 3; i++)
            {
                mean[i] /= count;
                rms[i] = (int)Math.Sqrt(rms[i] / count);
            }
            var currentMode = mode.ElementAt(0);
            foreach (var item in mode)
            {
                if (item.Value > currentMode.Value)
                {
                    currentMode = item;
                }
                else if (item.Value == currentMode.Value)
                {
                    // get brighter value if mode is joint
                    if (item.Key.R + item.Key.G + item.Key.B > currentMode.Key.R + currentMode.Key.G + currentMode.Key.B)
                    {
                        currentMode = item;
                    }
                }
            }

            //output data
            modeOut.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(currentMode.Key.R, currentMode.Key.G, currentMode.Key.B));
            meanOut.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(Convert.ToByte(mean[0]), Convert.ToByte(mean[1]), Convert.ToByte(mean[2])));
            rmsOut.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(Convert.ToByte(rms[0]), Convert.ToByte(rms[1]), Convert.ToByte(rms[2])));

            timer.Stop();
            timerOut.Content = (timer.Elapsed.TotalSeconds + timerMod) + "s";
        }
    }
}
