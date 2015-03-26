using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RippleEditor.Utilities
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
            set { TileID.Width = value * Constants.HRatio; }
        }

        public double TileHeight
        {
            set { TileID.Height = value * Constants.VRatio; }
        }

        public void SetMargin(double left, double top)
        {
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;
            TileID.Margin = new Thickness(left * Constants.HRatio + 50, top * Constants.VRatio + 50, 0, 0);
        }

        public void SetNames(String tileID, MainPage mainInstance)
        {
            TileIDName = TileIDName.Replace("TileID", tileID);
            InnerTileIDName = InnerTileIDName.Replace("TileID", tileID);
            TileIDLabelName = TileIDLabelName.Replace("TileID", tileID);
            InnerContentTileIDName = InnerContentTileIDName.Replace("TileID", tileID);
            TileIDButtonName = TileIDButtonName.Replace("TileID", tileID);

            mainInstance.RegisterName(TileIDName, TileID);
            mainInstance.RegisterName(InnerContentTileIDName, InnerContentTileID);
            mainInstance.RegisterName(TileIDLabelName, TileIDLabel);
            mainInstance.RegisterName(InnerTileIDName, InnerTileID);
            mainInstance.RegisterName(TileIDButtonName, TileIDButton);
        }

        public String TileIDName 
        {
            get { return TileID.Name; }
            set { TileID.Name = value; }
        }

        public String TileIDButtonName
        {
            get { return TileIDButton.Name; }
            set { TileIDButton.Name = value; }
        }

        public String TileIDLabelName
        {
            get { return TileIDLabel.Name; }
            set { TileIDLabel.Name = value; }
        }

        public String InnerTileIDName
        {
            get { return InnerTileID.Name; }
            set { InnerTileID.Name = value; }
        }

        public String InnerContentTileIDName
        {
            get { return InnerContentTileID.Name; }
            set { InnerContentTileID.Name = value; }
        }

        public String TileIDLabelText
        {
            get { return TileIDLabel.Name; }
            set { TileIDLabel.Text = value; }
        }

        public void UnregisterNames(MainPage mainInstance)
        {
            mainInstance.UnregisterName(TileIDName);
            mainInstance.UnregisterName(InnerContentTileIDName);
            mainInstance.UnregisterName(TileIDLabelName);
            mainInstance.UnregisterName(InnerTileIDName);
            mainInstance.UnregisterName(TileIDButtonName);
        }
    }
}
