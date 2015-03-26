using System.Windows;
using System.Windows.Controls;
using RippleCommonUtilities;
using RippleFloorApp.Utilities;

namespace RippleFloorApp.Controls
{
    /// <summary>
    /// Interaction logic for OverlayImageControl.xaml
    /// </summary>
    public partial class OverlayImageControl : UserControl
    {
        public OverlayImageControl(FloorWindow floorInstance)
        {
            InitializeComponent();
            floorInstance.RegisterName(OverlayImage.Name, OverlayImage);
        }

        public void SetMargin(double tileHeight)
        {
            Margin = new Thickness((2 * tileHeight * Globals.CurrentResolution.HorizontalResolution + Constants.OverlayImageMargin), Constants.OverlayImageMargin, (tileHeight * Globals.CurrentResolution.HorizontalResolution + Constants.OverlayImageMargin), Constants.OverlayImageMargin);
        }
    }
}
