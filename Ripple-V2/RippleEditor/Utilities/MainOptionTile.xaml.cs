using System.Windows;
using System.Windows.Controls;

namespace RippleEditor.Utilities
{
    /// <summary>
    /// Interaction logic for MainOptionTile.xaml
    /// </summary>
    public partial class MainOptionTile : UserControl
    {
        public MainOptionTile(MainPage floorInstance)
        {
            InitializeComponent();
            MainOptionGrid.Visibility = Visibility.Collapsed;
            floorInstance.RegisterName(MainOptionGrid.Name, MainOptionGrid);
            floorInstance.RegisterName(MainOptionGridLabel.Name, MainOptionGridLabel);
        }

        public double ControlHeight
        {
            set { Height = value * Constants.VRatio;}
        }

        public double ControlWidth
        {
            set { Width = value * Constants.HRatio; }
        }

        public void SetMargin(double top, double height)
        {
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;
            Margin = new Thickness(50,(top - height) * Constants.VRatio + 50, 0,0);
        }

        public void UnregisterNames(MainPage floorInstance)
        {
            floorInstance.UnregisterName(MainOptionGrid.Name);
            floorInstance.UnregisterName(MainOptionGridLabel.Name);
        }

    }
}
