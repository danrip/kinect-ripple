using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RippleCommonUtilities;

namespace RippleFloorApp.Controls
{
    /// <summary>
    /// Interaction logic for TileControl.xaml
    /// </summary>
    public partial class TileControl : UserControl
    {
        public TileControl()
        {
            InitializeComponent();
        }

        public Color TileBackground
        {
            set { TileID.Background = new SolidColorBrush(value); }
        }

        public double TileWidth
        {
            set { TileID.Height = value * Globals.CurrentResolution.VerticalResolution; }
        }

        public double TileHeight
        {
            set { TileID.Width = value * Globals.CurrentResolution.HorizontalResolution; }
        }

        public void SetMargin(double left, double top)
        {
            HorizontalAlignment = HorizontalAlignment.Right;
            VerticalAlignment = VerticalAlignment.Top;
            TileID.Margin = new Thickness(0, left * Globals.CurrentResolution.VerticalResolution, top * Globals.CurrentResolution.HorizontalResolution, 0);
        }

        public String TileIDName
        {
            get { return TileID.Name; }
            set { TileID.Name = value; }
        }

        public String TileIDLabelName
        {
            get { return TileIDLabel.Name; }
            set { TileIDLabel.Name = value; }
        }

        public String InnerContentTileIDName
        {
            get { return InnerContentTileID.Name; }
            set { InnerContentTileID.Name = value; }
        }

        public String TileIDLabelText
        {
            get { return TileIDLabel.Text; }
            set { TileIDLabel.Text = value; }
        }
    }
}
