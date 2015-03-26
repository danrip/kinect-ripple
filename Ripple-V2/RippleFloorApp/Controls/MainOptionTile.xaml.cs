using System.Windows;
using System.Windows.Controls;
using RippleCommonUtilities;

namespace RippleFloorApp.Controls
{
    /// <summary>
    /// Interaction logic for MainOptionTile.xaml
    /// </summary>
    public partial class MainOptionTile : UserControl
    {
        public MainOptionTile(FloorWindow floorInstance)
        {
            InitializeComponent();
            floorInstance.RegisterName(MainOptionGrid.Name, MainOptionGrid);
            floorInstance.RegisterName(MainOptionGridLabel.Name, MainOptionGridLabel);
        }

        public double ControlWidth
        {
            set { Height = value * Globals.CurrentResolution.VerticalResolution; }
        }

        public double ControlHeight
        {
            set { Width = value * Globals.CurrentResolution.HorizontalResolution; }
        }

        public void SetMargin(double left, double top)
        {
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;
            Margin = new Thickness(left * Globals.CurrentResolution.HorizontalResolution,0,0,0);
        }

    }
}
