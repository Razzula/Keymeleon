//  Copyright (C) 2022  Jack Gillespie  https://github.com/Razzula/Keymeleon/blob/main/LICENSE.md

using System.Windows;

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
