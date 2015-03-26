using System.Windows;
using System.Windows.Controls;
using RippleCommonUtilities;

namespace RippleFloorApp.Controls
{
    /// <summary>
    /// Interaction logic for UpperVideoControl.xaml
    /// </summary>
    public partial class UpperVideoControl : UserControl
    {
        public UpperVideoControl(FloorWindow floorInstance)
        {
            InitializeComponent();

            //Register the names
            floorInstance.RegisterName(UpperTile.Name, UpperTile);
            floorInstance.RegisterName(FloorVideoControl.Name, FloorVideoControl);
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
            HorizontalAlignment = HorizontalAlignment.Right;
            VerticalAlignment = VerticalAlignment.Top;
            Margin = new Thickness(0, left * Globals.CurrentResolution.VerticalResolution, top * Globals.CurrentResolution.HorizontalResolution, 0);
        }
    }
}
