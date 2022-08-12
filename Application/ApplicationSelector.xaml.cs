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

using System.IO;

namespace Keymeleon
{
    public partial class ApplicationSelector : Window
    {
        EditorWindow editor;

        public ApplicationSelector(EditorWindow owner)
        {
            InitializeComponent();
            editor = owner;

            //fill list with applications
        }

        private void Submit(object sender, RoutedEventArgs e)
        {
            string fileName = "layouts/"+inputFileName.Text;
            if (fileName.EndsWith(".conf") || fileName.EndsWith(".base"))
            {
                if (!File.Exists(fileName))
                {
                    editor.CreateConfig(inputFileName.Text);
                    Close();
                }
                else
                {
                    //file already exists
                }
            }
            else
            {
                //invalid name
            }
        }
    }
}
