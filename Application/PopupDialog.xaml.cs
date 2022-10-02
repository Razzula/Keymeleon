//  Copyright (C) 2022  Jack Gillespie  https://github.com/Razzula/Keymeleon/blob/main/LICENSE.md

using System.Windows;

namespace Keymeleon
{
    public partial class PopupDialog : Window
    {

        public PopupDialog(string title, string msg)
        {
            InitializeComponent();

            this.Title = title;
            msgBlock.Text = msg;
        }

        private void OKButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }
    }
}
