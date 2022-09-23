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

        public ZoneListItem(Action<ZoneListItem> removalAction)
        {
            InitializeComponent();
            this.removalAction = removalAction;
        }

        public void SetRectangle(Rectangle rect)
        {
            rectangleUI = rect;
        }

        public Rectangle GetRectangle()
        {
            return rectangleUI;
        }

        public int[] GetOrigin()
        {
            return new int[2] { Int32.Parse(originX.Text), Int32.Parse(originY.Text) };
        }

        public int[] GetTarget()
        {
            return new int[2] { Int32.Parse(targetX.Text), Int32.Parse(targetY.Text) };
        }

        private void RemoveZone(object sender, RoutedEventArgs e)
        {
            removalAction.Invoke(this);
        }

    }
}
