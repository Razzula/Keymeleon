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
        }

        private void OpenAverages(object sender, RoutedEventArgs e)
        {
            AveragesWindow averages = new();
            averages.Show();
        }

        private void OpenResolutions(object sender, RoutedEventArgs e)
        {
            ResolutionsWindow resolutions = new();
            resolutions.Show();
        }

        private void OpenLive(object sender, RoutedEventArgs e)
        {
            LiveWindow live = new();
            live.Show();
        }
    }
}
