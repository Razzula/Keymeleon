using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Keymeleon
{
    internal class ScreenColourCalculator
    {
        public Bitmap GetScreenImage(int width=-1, int height=-1)
        {
            Rectangle bounds = Screen.GetBounds(Point.Empty);

            //get screen image
            Bitmap src = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(src))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            } //g.Dispose()

            //resolution
            if (width < 0)
            {
                width = bounds.Width;
            }
            if (height < 0)
            {
                height = bounds.Height;
            }
            var capture = new Bitmap(src, width, height);
            src.Dispose(); //remove full-sized image from memory

            return capture;
        }

        public Color GetAverageColourOf(Bitmap src, Double x1, Double y1, Double x2, Double y2, int lowLightExclusion = 0)
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

                imgAttribs.Dispose();
            } //g.Dispose()

            //ANALYSE IMAGE
            int originX = (int) (x1 * src.Width);
            int originY = (int) (y1 * src.Height);
            int targetX = (int) (x2 * src.Width);
            int targetY = (int) (y2 * src.Height);

            double[] mean = new double[] { 0, 0, 0 };
            int count = 0;

            //get data
            for (int y = originY; y < targetY; y++)
            {
                for (int x = originX; x < targetX; x++)
                {
                    Color pixel = src.GetPixel(x, y);

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

            return Color.FromArgb(255, (int)mean[0], (int)mean[1], (int)mean[2]);
        }
    }
}
