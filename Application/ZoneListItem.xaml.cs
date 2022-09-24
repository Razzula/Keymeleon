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

namespace Keymeleon
{

    public partial class ZoneListItem : UserControl
    {
        Rectangle rectangleUI;
        Action<ZoneListItem> removalAction;
        Action<ZoneListItem, bool> setFieldAction;

        Double[] originCoords = new Double[2];
        Double[] targetCoords = new Double[2];

        public ZoneListItem(Action<ZoneListItem, bool> setFieldAction, Action<ZoneListItem> removalAction)
        {
            InitializeComponent();
            this.removalAction = removalAction;
            this.setFieldAction = setFieldAction;
            
        }

        public void SetRectangle(Rectangle rect)
        {
            rectangleUI = rect;
        }

        public Rectangle GetRectangle()
        {
            return rectangleUI;
        }

        public void SetOrigin(Double x, Double y)
        {
            originCoords[0] = x;
            originCoords[1] = y;

            originX.Text = ((int)(x * 1920f)).ToString();
            originY.Text = ((int)(y * 1080f)).ToString();
        }

        public void SetTarget(Double x, Double y)
        {
            targetCoords[0] = x;
            targetCoords[1] = y;

            targetX.Text = ((int)(x * 1920f)).ToString();
            targetY.Text = ((int)(y * 1080f)).ToString();
        }

        public Double[] GetOrigin()
        {
            return originCoords;
        }

        public Double[] GetTarget()
        {
            return targetCoords;
        }

        private void RemoveZone(object sender, RoutedEventArgs e)
        {
            removalAction.Invoke(this);
        }

        private void TextBoxSelected(object sender, EventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                setFieldAction.Invoke(this, textBox.Name.Contains("origin"));
            }
        }

        public string? GetKey()
        {
            if (keyList.SelectedItem != null)
            {
                return keyList.SelectedItem.ToString();
            }
            return null;
        }

        public void SetKeyList(string[] list)
        {
            keyList.ItemsSource = list;
        }

        public void SetKey(string key)
        {
            keyList.SelectedItem = key;
        }

        private void ChangeColour(object sender, MouseEventArgs e)
        {
            Random rng = new();
            var zoneColour = new SolidColorBrush(Color.FromArgb((byte)rng.Next(0, 255), (byte)rng.Next(0, 255), (byte)rng.Next(0, 255), 0));
            colourRect.Fill = zoneColour;
            rectangleUI.Fill = zoneColour;
        }

    }
}
