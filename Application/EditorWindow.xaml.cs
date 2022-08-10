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

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

namespace Keymeleon
{
    public partial class EditorWindow : Window
    {
        static class NativeMethods
        {
            [DllImport("kym.dll")]
            public static extern int setCustomLayout(string configFileName, int profileToModify);
            [DllImport("kym.dll")]
            public static extern int setKeyColour(string keycode, int r, int g, int b, int profile);
            [DllImport("kym.dll")]
            public static extern int setActiveProfile(int profile);
        }

        Dictionary<string, int[]> defaultState = new Dictionary<string, int[]>();
        Dictionary<string, int[]> keyboardState = new Dictionary<string, int[]>();

        public EditorWindow()
        {
            LoadConfig("default.conf", defaultState); //TEMP
            InitializeComponent();
        }

        private void LoadConfig(string fileName, Dictionary<string, int[]> state)
        {
            state.Clear();

            StreamReader streamReader;
            try
            {
                streamReader = File.OpenText(fileName);
            }
            catch (System.IO.FileNotFoundException)
            {
                //TODO; add error msg
                return;
            }
            
            string text = streamReader.ReadToEnd();
            streamReader.Close();
            //split into line
            string[] lines = text.Split(Environment.NewLine);

            //setup dictionary
            foreach (string line in lines)
            {
                if (line.Equals("")) //blank
                {
                    continue;
                }
                if (line[0].Equals('#')) //comment
                {
                    continue;
                }
                //read from line
                string[] data = line.Split('\t');
                Debug.Write(data[0]+" ");
                Debug.WriteLine(data[1]);

                //set value in dictionary
                int r = Convert.ToInt32(data[1].Substring(0, 2), 16);
                int g = Convert.ToInt32(data[1].Substring(2, 2), 16);
                int b = Convert.ToInt32(data[1].Substring(4, 2), 16);
                state.Add(data[0], new[] { r, g, b });

                var test = (Button) this.FindName("button_" + data[0]);

                if (test != null)
                {
                    //show colour in UI
                    Color colour = Color.FromRgb(Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));
                    test.Foreground = new SolidColorBrush(colour);
                }

            }

            //display config on keyboard
            Debug.WriteLine(NativeMethods.setCustomLayout(fileName, 1));
            NativeMethods.setActiveProfile(1);
        }

        private void SaveConfig(object sender, RoutedEventArgs e)
        {
            Dictionary<string, int[]> tempDictionary;
            string fileName = configBox.Text;
            if (fileName.Equals("default.conf"))
            {
                //update defaultState
                foreach (var item in keyboardState)
                {
                    if (defaultState.ContainsKey(item.Key))
                    {
                        defaultState[item.Key] = item.Value;
                    }
                    else
                    {
                        defaultState.Add(item.Key, item.Value);
                    }
                }

                tempDictionary = defaultState;
            }
            else
            {
                tempDictionary = keyboardState;
            }

            List<string> lines = new List<string>();

            foreach (var item in tempDictionary)
            {
                if (item.Value[0] == -1 || item.Value[1] == -1 || item.Value[2] == -1) //transparent
                {
                    //TODO; default colour
                    continue;
                }
                lines.Add(item.Key + '\t' + item.Value[0].ToString("x2") + item.Value[1].ToString("x2") + item.Value[2].ToString("x2"));
            }

            File.WriteAllLines(configBox.Text, lines);
        }

        private void ButtonLoaded(object sender, RoutedEventArgs e)
        {
            var keycode = (e.Source as Button).Content.ToString();
            //show colour in UI
            if (defaultState.ContainsKey(keycode))
            {
                var values = defaultState[keycode];
                Color colour = Color.FromRgb(Convert.ToByte(values[0]), Convert.ToByte(values[1]), Convert.ToByte(values[2]));
                (e.Source as Button).Foreground = new SolidColorBrush(colour);
            }
            else
            {
                defaultState.Add(keycode, new[] { -1, -1, -1 }); //if key not in default.conf, ensure it is present in dictionary
            }
        }

        private void LoadConfig(object sender, RoutedEventArgs e)
        {
            LoadConfig(configBox.Text, keyboardState);
        }

        private void ButtonClicked(object sender, RoutedEventArgs e)
        {
            //get keycode
            var keycode = (e.Source as Button).Content.ToString();
            if (rBox.Text.Equals("")) { rBox.Text = "0"; }
            if (gBox.Text.Equals("")) { gBox.Text = "0"; } //simple validation
            if (bBox.Text.Equals("")) { bBox.Text = "0"; }
            //get colour
            int r = Int32.Parse(rBox.Text);
            int g = Int32.Parse(gBox.Text);
            int b = Int32.Parse(bBox.Text);
            Debug.WriteLine(keycode + " "+r+" "+g+" "+b); //DEBUG

            keyboardState[keycode] = new[] { r, g, b };

            //write to device
            int temp = NativeMethods.setKeyColour(keycode, r, g, b, 1);
            Debug.WriteLine(temp);

            if (temp == 192) //successful
            {
                //reflect change in UI
                Color colour = Color.FromRgb(Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));
                (e.Source as Button).Foreground = new SolidColorBrush(colour);
            }

        }

        private void TextChanged(object sender, RoutedEventArgs e)
        {
            string text = (e.Source as TextBox).Text;

            if (text.Equals(""))
            {
                (e.Source as TextBox).Text = "0";
                DisplayColour();
                return;
            }

            int temp;
            try
            {
                temp = Int32.Parse(text);
            }
            catch (System.FormatException)
            {
                (e.Source as TextBox).Text = text.TrimEnd(text[text.Length - 1]);
                return;
            }

            if (temp > 255)
            {
                (e.Source as TextBox).Text = "255";
            }
            else if (temp < 0)
            {
                (e.Source as TextBox).Text = "0";
            }
            DisplayColour();
        }

        private void DisplayColour()
        {
            //get colour
            if (rBox == null) { return; }
            int r = Int32.Parse(rBox.Text);
            if (bBox == null) { return; }
            int g = Int32.Parse(gBox.Text);
            if (bBox == null) { return; }
            int b = Int32.Parse(bBox.Text);

            if (colourDisplay == null) { return; }
            Color colour = Color.FromRgb(Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));
            colourDisplay.Fill = new SolidColorBrush(colour);
        }
    }

}
