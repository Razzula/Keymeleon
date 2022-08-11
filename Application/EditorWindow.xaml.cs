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
            public static extern int SetLayoutBase(string configFileName, int profileToModify);
            [DllImport("kym.dll")]
            public static extern int SetActiveProfile(int profile);
            [DllImport("kym.dll")]
            public static extern int ApplyLayoutLayer(string configFileName, int profileToModify);
            [DllImport("kym.dll")]
            public static extern int SetKeyColour(string keycode, int r, int g, int b, int profile);
        }

        Dictionary<string, int[]> defaultState = new Dictionary<string, int[]>();
        Dictionary<string, int[]> keyboardState = new Dictionary<string, int[]>();
        Button[][] rows;

        public EditorWindow()
        {
            InitializeComponent();

            Button[][] rows = {
                new[] { Esc, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12 },
                new[] { Tilde, _1, _2, _3, _4, _5, _6, _7, _8, _9, _0, Minus, Equals, Num_Lock, Num_Slash, Num_Asterisk },
                new[] { Tab, q, w, e, r, t, y, u, i, o, p, BracketL, BracketR, Num_7, Num_8, Num_9 },
                new[] { CapsLock, a, s, d, f, g, h, j, k, l, Semicolon, Apostrophe, Hash, Num_4, Num_5, Num_6 },
                new[] { LShift, z, x, c, v, b, n, m, Comma, Period, Slash, RShift, Enter, Num_1, Num_2, Num_3 },
                new[] { LCtrl, Super, LAlt, Space, RAlt, Fn, Menu, RCtrl, Left, Down, Up, Right, Backspace, Num_0, Num_Period, Num_Enter },
                new[] { Backslash, PrtSc, ScrLk, Pause, null, Insert, Home, PgUp, Delete, End, PgDn, Num_Minus, Num_Plus }
            };
            this.rows = rows;

            LoadBaseConfig("default.base");
            NativeMethods.SetActiveProfile(1);
        }

        private void LoadBaseConfig(string fileName)
        {
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
            //split into lines
            string[] lines = text.Split(Environment.NewLine);

            int currentRow = 0;
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
                string[] data = line.Split(' ');
                for (int i = 1; i < data.Length; i++)
                {
                    var btn = rows[currentRow][i - 1];
                    if (btn == null) { continue; }

                    //set value in dictionary
                    var keycode = btn.Name.ToString();
                    if (keycode[0].Equals('_'))
                    {
                        keycode = keycode.Substring(1);
                    }
                    int r = Convert.ToInt32(data[1].Substring(0, 2), 16);
                    int g = Convert.ToInt32(data[1].Substring(2, 2), 16);
                    int b = Convert.ToInt32(data[1].Substring(4, 2), 16);
                    if (defaultState.ContainsKey(keycode))
                    {
                        defaultState[keycode] = new[] { r, g, b };
                    }
                    else
                    {
                        defaultState.Add(keycode, new[] { r, g, b });
                    }

                    //show colour in UI
                    Color colour = Color.FromRgb(Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));
                    btn.Foreground = new SolidColorBrush(colour);

                }

                currentRow += 1;
            }

            //display config on keyboard
            Debug.WriteLine(fileName);//DEBUG
            Debug.WriteLine(NativeMethods.SetLayoutBase(fileName, 1));
        }

        private void LoadLayerConfig(string fileName, Dictionary<string, int[]> state)
        {
            //TODO; remove current state from UI
            //undo previous layer
            foreach (var item in keyboardState)
            {
                var value = defaultState[item.Key];
                var btn = (Button)this.FindName(item.Key);
                if (btn == null)
                {
                    btn = (Button)this.FindName("_" + item.Key);
                }
                //show colour in UI
                Color colour = Color.FromRgb(Convert.ToByte(value[0]), Convert.ToByte(value[1]), Convert.ToByte(value[2]));
                btn.Foreground = new SolidColorBrush(colour);
                //show colour on device
                NativeMethods.SetKeyColour(item.Key, value[0], value[1], value[2], 1); //TODO; optimize
            }
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
            //split into lines
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
                //Debug.Write(data[0]+" "); Debug.WriteLine(data[1]);//DEBUG

                //set value in dictionary
                int r = Convert.ToInt32(data[1].Substring(0, 2), 16);
                int g = Convert.ToInt32(data[1].Substring(2, 2), 16);
                int b = Convert.ToInt32(data[1].Substring(4, 2), 16);
                state.Add(data[0], new[] { r, g, b });

                var btn = (Button) this.FindName(data[0]);
                if (btn == null)
                {
                    btn = (Button)this.FindName("_"+data[0]);
                }

                if (btn != null)
                {
                    //show colour in UI
                    Color colour = Color.FromRgb(Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));
                    btn.Foreground = new SolidColorBrush(colour);
                }

            }

            //display config on keyboard
            Debug.WriteLine(fileName);//DEBUG
            Debug.WriteLine(NativeMethods.ApplyLayoutLayer(fileName, 1));
        }

        private void SaveConfig(object sender, RoutedEventArgs e)
        {
            var fileName = configBox.Text;
            if (fileName.EndsWith(".base"))
            {
                SaveBaseConfig();
            }
            else if (fileName.EndsWith(".conf"))
            {
                SaveLayerConfig();
            }
        }

        private void SaveBaseConfig()
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

            string fileName = configBox.Text;

            List<string> lines = new List<string>();

            foreach (var row in rows)
            {
                string line = "";
                foreach (var btn in row)
                {
                    if (btn == null) {
                        line += " 000000";
                        continue;
                    };

                    var keycode = btn.Name.ToString(); //TODO convert this to function
                    if (keycode[0].Equals('_'))
                    {
                        keycode = keycode.Substring(1);
                    }

                    var colour = defaultState[keycode];
                    line += " " + colour[0].ToString("x2") + colour[1].ToString("x2") + colour[2].ToString("x2");
                }
                if (!line.Equals(""))
                {
                    lines.Add(line);
                }
            }


            File.WriteAllLines(configBox.Text, lines);
        }

        private void SaveLayerConfig()
        {
            string fileName = configBox.Text;

            List<string> lines = new List<string>();
            foreach (var item in keyboardState)
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
            var keycode = (e.Source as Button).Name.ToString();
            if (keycode[0].Equals('_'))
            {
                keycode = keycode.Substring(1);
            }
            //show colour in UI
            if (defaultState.ContainsKey(keycode))
            {
                var values = defaultState[keycode];
                if (values[0] == -1 || values[1] == -1 || values[2] == -1)
                {
                    return;
                }
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
            var fileName = configBox.Text;
            if (fileName.EndsWith(".base"))
            {
                LoadBaseConfig(configBox.Text);
            }
            else if (fileName.EndsWith(".conf"))
            {
                LoadLayerConfig(configBox.Text, keyboardState);
            }
        }

        private void ButtonClicked(object sender, RoutedEventArgs e)
        {
            string keycode = (e.Source as Button).Name.ToString();
            if (keycode[0].Equals('_'))
            {
                keycode = keycode.Substring(1);
            }
            if (rBox.Text.Equals("")) { rBox.Text = "0"; }
            if (gBox.Text.Equals("")) { gBox.Text = "0"; } //simple validation
            if (bBox.Text.Equals("")) { bBox.Text = "0"; }
            //get colour
            int r = Int32.Parse(rBox.Text);
            int g = Int32.Parse(gBox.Text);
            int b = Int32.Parse(bBox.Text);
            //Debug.WriteLine(keycode + " "+r+" "+g+" "+b);//DEBUG

            keyboardState[keycode] = new[] { r, g, b };

            //write to device
            int temp = NativeMethods.SetKeyColour(keycode, r, g, b, 1);
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
