//  Copyright (C) 2022  Jack Gillespie  https://github.com/Razzula/Keymeleon/blob/main/LICENSE.md

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

namespace Keymeleon
{
    public partial class PopupConfirmation : Window
    {

        public bool result = false;

        public PopupConfirmation(string title, string msg)
        {
            InitializeComponent();

            this.Title = title;
            msgBlock.Text = msg;
        }

        private void Affirmative_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            result = true;
            Close();
        }

        private void Negative_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }
    }
}
