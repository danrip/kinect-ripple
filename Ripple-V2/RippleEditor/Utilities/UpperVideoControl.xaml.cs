using System.Windows;
using System.Windows.Controls;

namespace RippleEditor.Utilities
{
    // <summary>
    /// Interaction logic for UpperVideoControl.xaml
    /// </summary>
    public partial class UpperVideoControl : UserControl
    {
        public UpperVideoControl(MainPage floorInstance)
        {
            InitializeComponent();

            //Register the names
            floorInstance.RegisterName(UpperTile.Name, UpperTile);
            floorInstance.RegisterName(FloorVideoControl.Name, FloorVideoControl);
        }

        public double ControlHeight
        {
            set { Height = value * Constants.VRatio; }
        }

        public double ControlWidth
        {
            set { Width = value * Constants.HRatio; }
        }

        public void SetMargin(double left, double top)
        {
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;
            Margin = new Thickness(left * Constants.HRatio + 50, top * Constants.VRatio + 50, 0, 0);
        }

        public void UnregisterNames(MainPage floorInstance)
        {
            floorInstance.UnregisterName(UpperTile.Name);
            floorInstance.UnregisterName(FloorVideoControl.Name);
        }
    }
}
