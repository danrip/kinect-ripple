using System.IO;
using System.Windows.Forms;
using RippleCommonUtilities;
using RippleDictionary;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RippleEditor.Utilities;
using ComboBox = System.Windows.Controls.ComboBox;
using HelperMethods = RippleEditor.Utilities.HelperMethods;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;
using UserControl = System.Windows.Controls.UserControl;

namespace RippleEditor.Controls
{
    /// <summary>
    /// Interaction logic for FloorPropertiesControl.xaml
    /// </summary>
    public partial class FloorPropertiesControl : UserControl
    {
        public FloorPropertiesControl()
        {
            InitializeComponent();
            InitializeControls();
        }

        #region Common Control Methods
        public void InitializeControls()
        {
            try
            {
                //Populate the tile types - default text
                CBTypeValue.Items.Clear();
                foreach (var typeName in Enum.GetNames(typeof(TileType)))
                {
                    CBTypeValue.Items.Add(typeName);
                }
                CBTypeValue.SelectedValue = TileType.Text.ToString();

                //Populate the Action types - default standard
                CBActionValue.Items.Clear();
                foreach (var actionName in Enum.GetNames(typeof(TileAction)))
                {
                    CBActionValue.Items.Add(actionName);
                }
                CBActionValue.SelectedValue = TileAction.Standard.ToString();

                //Initialize the text boxes
                ActionURIBrowseButton.IsEnabled = false;
                clrPicker.SelectedColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");
                TBTextValue.Text = "";
                TBActionURIValue.Text = "";
                TBContentValue.Text = "";
                TBActionURIValue.ToolTip = "";
                TBContentValue.ToolTip = "";
                ContentBrowseButton.IsEnabled = false;
                TBActionURIValue.IsReadOnly = true;
                TBActionURIValue.IsReadOnlyCaretVisible = true;
                TBActionURIValue.Text = "";
                TBActionURIValue.ToolTip = "";
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in InitializeControls for Floor Properties: {0}", ex.Message);
            }
        }

        private bool ValidateTextValue(object sender)
        {
            var tb = sender as TextBox;
            if (!String.IsNullOrEmpty(tb.Text))
            {
                if (tb.Text.Length > Constants.MaxCharForTileName)
                {
                    MessageBox.Show(String.Format("The name for the tile cannot exceed {0} characters", Constants.MaxCharForTileName));
                    tb.Text = "";
                    return false;
                }
            }
            return true;
        }

        public bool ValidateControl()
        {
            try
            {
                //Text value can be empty, or any value less than Utilities.Constants.MaxCharValue
                if (!ValidateTextValue(TBTextValue))
                    return false;

                //Color value can be any value right now

                //Combo box for Type
                //Content is mandatory for anything except Text and blank
                if (!CBTypeValue.SelectedValue.Equals(TileType.Text.ToString()))
                {
                    if (String.IsNullOrEmpty(TBContentValue.Text))
                    {
                        MessageBox.Show("Please select a valid URI for Tile Content in Floor Properties");
                        return false;
                    }
                }

                //Combo box for Action
                //Content is mandatory for Animation and QRCode
                if (CBActionValue.SelectedValue.ToString().Equals(TileAction.HTML.ToString()) || CBActionValue.SelectedValue.Equals(TileAction.QRCode.ToString()))
                {
                    if (String.IsNullOrEmpty(TBActionURIValue.Text))
                    {
                        MessageBox.Show("Please select a valid URI for Tile Action Content in Floor Properties");
                        return false;
                    }

                    //Validate the content value
                    var actionContent = TBActionURIValue.Text;

                    //Animation - local and web
                    if(CBActionValue.SelectedValue.ToString().Equals(TileAction.HTML.ToString()))
                    {
                        if (!actionContent.StartsWith("http") && (!(actionContent.EndsWith(".htm") || actionContent.EndsWith(".html"))))
                        {
                            MessageBox.Show("Please enter valid value for HTML based URI, it should have an extension html or htm for local files, for web hosted files it should start with http");
                            return false;
                        }
                    }

                    //QRCode - web urls only
                    if (CBActionValue.SelectedValue.ToString().Equals(TileAction.QRCode.ToString()))
                    {
                        if (!actionContent.StartsWith("http"))
                        {
                            MessageBox.Show("Please enter valid value for HTML based URI, it should start with http");
                            return false;
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in ValidateControl for Floor Properties: {0}", ex.Message);
                return false;
            }
        }

        public void SaveFloorProperties(Tile tile)
        {
            try
            {
                //Name
                tile.Name = TBTextValue.Text;
                //Tile type
                tile.TileType = (TileType)CBTypeValue.SelectedIndex;
                //Content
                tile.Content = TBContentValue.Text;
                //Color
                tile.Color = clrPicker.SelectedColor;
                //Action Type
                tile.Action = (TileAction)CBActionValue.SelectedIndex;
                //Action Content
                tile.ActionURI = TBActionURIValue.Text;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in SaveFloorProperties for Floor Properties: {0}", ex.Message);
            }
        }

        public void SetFloorProperties(Tile tile)
        {
            try
            {
                //Text
                TBTextValue.Text = tile.Name;
            
                //Color
                clrPicker.SelectedColor = tile.Color;

                //Tile Type
                CBTypeValue.SelectedValue = tile.TileType.ToString();

                //Tile content
                if ((!String.IsNullOrEmpty(tile.Content)) && (tile.Content.StartsWith(@"\Assets\")))
                    TBContentValue.Text = HelperMethods.TargetAssetsRoot + tile.Content;
                else
                    TBContentValue.Text = tile.Content;

                //Action type
                CBActionValue.SelectedValue = tile.Action.ToString();                

                //Action content
                if ((!String.IsNullOrEmpty(tile.ActionURI)) && (tile.ActionURI.StartsWith(@"\Assets\")))
                    TBActionURIValue.Text = HelperMethods.TargetAssetsRoot + tile.ActionURI;
                else
                    TBActionURIValue.Text = tile.ActionURI;

                //UI settings
                //Content browse button
                if (CBTypeValue.SelectedValue.ToString().Equals(TileType.Text.ToString()))
                {
                    ContentBrowseButton.IsEnabled = false;
                }
                else
                {
                    ContentBrowseButton.IsEnabled = true;
                }

                //Action URI browse button and textbox
                if (CBActionValue.SelectedValue.ToString().Equals(TileAction.HTML.ToString()))
                {
                    ActionURIBrowseButton.IsEnabled = true;
                    TBActionURIValue.IsReadOnly = true;
                    TBActionURIValue.IsReadOnlyCaretVisible = true;
                }
                else if (CBActionValue.SelectedValue.ToString().Equals(TileAction.QRCode.ToString()))
                {
                    ActionURIBrowseButton.IsEnabled = false;
                    TBActionURIValue.IsReadOnly = false;
                    TBActionURIValue.IsReadOnlyCaretVisible = false;
                }
                else
                {
                    ActionURIBrowseButton.IsEnabled = false;
                    TBActionURIValue.IsReadOnly = true;
                    TBActionURIValue.IsReadOnlyCaretVisible = true;
                }

                //Custom disablement for start
                if (tile.Id.Equals("Tile0"))
                {
                    TBTextValue.IsEnabled = false;
                    CBTypeValue.IsEnabled = false;
                    CBActionValue.IsEnabled = false;
                }
                else
                {
                    TBTextValue.IsEnabled = true;
                    CBTypeValue.IsEnabled = true;
                    CBActionValue.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in SetFloorProperties for Floor Properties: {0}", ex.Message);                
            }
        } 
        #endregion

        #region UI Methods
        private void CBTypeValue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var cb = sender as ComboBox;
                if (cb.SelectedValue != null)
                {
                    if (cb.SelectedValue.ToString().Equals(TileType.Text.ToString()))
                    {
                        ContentBrowseButton.IsEnabled = false;
                    }
                    else
                    {
                        ContentBrowseButton.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in CBTypeValue_SelectionChanged for Floor Properties: {0}", ex.Message);
            }
        }

        private void CBActionValue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var cb = sender as ComboBox;
                if (cb.SelectedValue != null)
                {
                    if (cb.SelectedValue.ToString().Equals(TileAction.HTML.ToString()))
                    {
                        ActionURIBrowseButton.IsEnabled = true;
                        TBActionURIValue.IsReadOnlyCaretVisible = false;
                        TBActionURIValue.IsReadOnly = false;
                        TBActionURIValue.Text = "";
                        TBActionURIValue.ToolTip = "";
                    }
                    else if (cb.SelectedValue.ToString().Equals(TileAction.QRCode.ToString()))
                    {
                        ActionURIBrowseButton.IsEnabled = false;
                        TBActionURIValue.IsReadOnlyCaretVisible = false;
                        TBActionURIValue.IsReadOnly = false;
                        TBActionURIValue.Text = "";
                        TBActionURIValue.ToolTip = "";
                    }
                    else
                    {
                        ActionURIBrowseButton.IsEnabled = false;
                        TBActionURIValue.IsReadOnly = true;
                        TBActionURIValue.IsReadOnlyCaretVisible = true;
                        TBActionURIValue.Text = "";
                        TBActionURIValue.ToolTip = "";
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in CBActionValue_SelectionChanged for Floor Properties: {0}", ex.Message);
            }
        }

        private void ActionURIBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Show the dialog box only if Animation mode
                if (CBActionValue.SelectedValue.ToString() == TileAction.HTML.ToString())
                {
                    var dlgBox = new OpenFileDialog();
                    dlgBox.Filter = "Animation files(*.swf;*.html;*.htm;)|*.swf;*.html;*.htm;";
                    var res = dlgBox.ShowDialog();
                    if (res == DialogResult.OK)
                    {
                        //Get the complete fileName
                        var updatedFileName = dlgBox.FileName;
                        if (Path.GetExtension(updatedFileName).ToLower().Equals(".swf"))
                        {
                            updatedFileName = HelperMethods.CopyFile(dlgBox.FileName, HelperMethods.TargetAssetsDirectory + "\\Animations");
                        }
                        else
                        {
                            var targetfolder = HelperMethods.CopyFolder(Path.GetDirectoryName(updatedFileName), HelperMethods.TargetAssetsDirectory + "\\Animations");
                            updatedFileName = targetfolder + "\\" + Path.GetFileName(updatedFileName);
                        }
                        if (!String.IsNullOrEmpty(updatedFileName))
                        {
                            TBActionURIValue.Text = updatedFileName;
                            TBActionURIValue.ToolTip = updatedFileName;
                        }
                        else
                        {
                            TBActionURIValue.Text = "";
                            TBActionURIValue.ToolTip = "";
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Browse applicable only for Action = Animation");
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in ActionURIBrowseButton_Click for Floor Properties: {0}", ex.Message);
            }
        }

        private void ContentBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Applicable only for types other than text
                if (!(CBTypeValue.SelectedValue.ToString() == TileType.Text.ToString()))
                {
                    var dlgBox = new OpenFileDialog();

                    if (CBTypeValue.SelectedValue.ToString() == TileType.OnlyMedia.ToString())
                        dlgBox.Filter = "Media Files(*.mp4;*.wmv;*.jpeg;*.png;*.jpg;*.bmp;)|*.mp4;*.wmv;*.jpeg;*.png;*.jpg;*.bmp;|Videos(*.mp4;*.wmv;)|*.mp4;*.wmv;|Images(*.jpeg;*.png;*.jpg;*.bmp;)|*.jpeg;*.png;*.jpg;*.bmp;";
                    else
                        dlgBox.Filter = "Images(*.jpeg;*.png;*.jpg;*.bmp;)|*.jpeg;*.png;*.jpg;*.bmp;";

                    var res = dlgBox.ShowDialog();
                    if (res == DialogResult.OK)
                    {
                        var targetFolder = HelperMethods.TargetAssetsDirectory;
                        var fileExt = Path.GetExtension(dlgBox.FileName).ToLower();
                        if (fileExt.Equals(".mp4") || fileExt.Equals(".wmv"))
                            targetFolder += "\\Videos";
                        else
                            targetFolder += "\\Images";
                        //Get the complete fileName
                        var updatedFileName = HelperMethods.CopyFile(dlgBox.FileName, targetFolder);
                        if (!String.IsNullOrEmpty(updatedFileName))
                        {
                            TBContentValue.Text = updatedFileName;
                            TBContentValue.ToolTip = updatedFileName;
                        }
                        else
                        {
                            TBContentValue.Text = "";
                            TBContentValue.ToolTip = "";
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Browse/Content not applicable for TileType = Text");
                }
            }
            catch (Exception ex)
            {

                LoggingHelper.LogTrace(1, "Went wrong in ContentBrowseButton_Click for Floor Properties: {0}", ex.Message);
            }
        }

        private void TBTextValue_LostFocus(object sender, RoutedEventArgs e)
        {
            ValidateTextValue(sender);
        }
        #endregion
    }
}
