using System.Windows;
using System.Windows.Controls;
using RippleEditor.Utilities;

namespace RippleEditor.Controls
{
    /// <summary>
    /// Interaction logic for OverlayImageControl.xaml
    /// </summary>
    public partial class OverlayImageControl : UserControl
    {
        public OverlayImageControl(MainPage main)
        {
            InitializeComponent();
            main.RegisterName(OverlayImage.Name, OverlayImage);
        }

        public void SetMargin(double tileHeight, double tileWidth)
        {
            VerticalAlignment = VerticalAlignment.Top;
            HorizontalAlignment = HorizontalAlignment.Left;
            Width = tileWidth;
            Height = tileHeight;
            Margin = new Thickness(Constants.OverlayImageMargin + 50, (tileHeight * Constants.VRatio + Constants.OverlayImageMargin),0,0);
        }
    }
}
