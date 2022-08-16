using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace Keymeleon
{
    internal class ConfigManager
    {
        Dictionary<string, int[]> baseState = new Dictionary<string, int[]>();
        Dictionary<string, int[]> layerState = new Dictionary<string, int[]>();
        string[][] headers;

        public ConfigManager()
        {
            string[][] headers = {
                new[] { "Esc", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12" },
                new[] { "Tilde", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "Minus", "Equals", "Num_Lock", "Num_Slash", "Num_Asterisk" },
                new[] { "Tab", "q", "w", "e", "r", "t", "y", "u", "i", "o", "p", "BracketL", "BracketR", "Num_7", "Num_8", "Num_9" },
                new[] { "CapsLock", "a", "s", "d", "f", "g", "h", "j", "k", "l", "Semicolon", "Apostrophe", "Hash", "Num_4", "Num_5", "Num_6" },
                new[] { "LShift", "z", "x", "c", "v", "b", "n", "m", "Comma", "Period", "Slash", "RShift", "Enter", "Num_1", "Num_2", "Num_3" },
                new[] { "LCtrl", "Super", "LAlt", "Space", "RAlt", "Fn", "Menu", "RCtrl", "Left", "Down", "Up", "Right", "Backspace", "Num_0", "Num_Period", "Num_Enter" },
                new[] { "Backslash", "PrtSc", "ScrLk", "Pause", null, "Insert", "Home", "PgUp", "Delete", "End", "PgDn", "Num_Minus", "Num_Plus" }
            };
            this.headers = headers;

            foreach (var row in headers)
            {
                foreach (var keycode in row)
                {
                    if (keycode != null)
                    {
                        baseState.Add(keycode, new[] {0, 0, 0});
                    }
                }
            }
        }

        public Dictionary<string, int[]> LoadBaseConfig(string fileName)
        {
            StreamReader streamReader;
            try
            {
                streamReader = File.OpenText(fileName);
            }
            catch (System.IO.FileNotFoundException)
            {
                //TODO; add error msg
                return null;
            }

            layerState.Clear();

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
                    string keycode = headers[currentRow][i - 1];
                    if (keycode == null) { continue; }

                    //set value in dictionary
                    if (keycode[0].Equals('_'))
                    {
                        keycode = keycode.Substring(1);
                    }
                    int r = Convert.ToInt32(data[i].Substring(0, 2), 16);
                    int g = Convert.ToInt32(data[i].Substring(2, 2), 16);
                    int b = Convert.ToInt32(data[i].Substring(4, 2), 16);
                    if (baseState.ContainsKey(keycode))
                    {
                        baseState[keycode] = new[] { r, g, b };
                    }
                    else
                    {
                        baseState.Add(keycode, new[] { r, g, b });
                    }

                }

                currentRow += 1;
            }

            return baseState;
        }

        public Dictionary<string, int[]> LoadLayerConfig(string fileName)
        {
            layerState.Clear();

            StreamReader streamReader;
            try
            {
                streamReader = File.OpenText(fileName);
            }
            catch (System.IO.FileNotFoundException)
            {
                //TODO; add error msg
                return null;
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
                layerState.Add(data[0], new[] { r, g, b });
            }

            return layerState;
        }

        public void UpdateLayer(string keycode, int r, int g, int b)
        {
            if (layerState.ContainsKey(keycode))
            {
                layerState[keycode] = new[] { r, g, b };
            }
            else
            {
                layerState.Add(keycode, new[] { r, g, b });
            }
        }

        public int[] RemoveKey(string keycode)
        {
            if (layerState.ContainsKey(keycode))
            {
                layerState.Remove(keycode);
            }

            if (baseState.ContainsKey(keycode))
            {
                return baseState[keycode];
            }
            return new[] { 0, 0, 0 };
        }

        public void SaveBaseConfig(string fileName)
        {
            //update defaultState
            foreach (var item in layerState)
            {
                if (baseState.ContainsKey(item.Key))
                {
                    baseState[item.Key] = item.Value;
                }
                else
                {
                    baseState.Add(item.Key, item.Value);
                }
            }

            List<string> lines = new List<string>();

            foreach (var row in headers)
            {
                string line = "";
                foreach (var keycode in row)
                {
                    if (keycode == null)
                    {
                        line += " 000000";
                        continue;
                    };

                    var colour = baseState[keycode];
                    line += " " + colour[0].ToString("x2") + colour[1].ToString("x2") + colour[2].ToString("x2");
                }
                if (!line.Equals(""))
                {
                    lines.Add(line);
                }
            }


            File.WriteAllLines(fileName, lines);
        }

        public void SaveLayerConfig(string fileName)
        {

            List<string> lines = new List<string>();
            foreach (var item in layerState)
            {
                if (item.Value[0] == -1 || item.Value[1] == -1 || item.Value[2] == -1) //transparent
                {
                    //TODO; default colour
                    continue;
                }
                lines.Add(item.Key + '\t' + item.Value[0].ToString("x2") + item.Value[1].ToString("x2") + item.Value[2].ToString("x2"));
            }

            File.WriteAllLines(fileName, lines);
        }

        public void SaveInverseConfig(string fileName)
        {
            var deltaState = GetStatesDelta();

            List<string> lines = new List<string>();
            foreach (var item in deltaState)
            {
                if (item.Value[0] == -1 || item.Value[1] == -1 || item.Value[2] == -1) //transparent
                {
                    //TODO; default colour
                    continue;
                }
                lines.Add(item.Key + '\t' + item.Value[0].ToString("x2") + item.Value[1].ToString("x2") + item.Value[2].ToString("x2"));
            }

            File.WriteAllLines(fileName, lines);
        }

        public Dictionary<string, int[]> GetStatesDelta()
        {
            Dictionary<string, int[]> statesDelta = new Dictionary<string, int[]>();

            foreach (var item in layerState)
            {
                if (baseState.ContainsKey(item.Key))
                {
                    statesDelta.Add(item.Key, baseState[item.Key]);
                }
            }

            return statesDelta;
        }
    }
}
