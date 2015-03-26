using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using RippleCommonUtilities;
using RippleDictionary;
using RippleEditor.Utilities;
using ComboBox = System.Windows.Controls.ComboBox;
using HelperMethods = RippleEditor.Utilities.HelperMethods;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace RippleEditor.Controls
{
    /// <summary>
    /// Interaction logic for ScreenPropertiesControl.xaml
    /// </summary>
    public partial class ScreenPropertiesControl : UserControl
    {
        public ScreenPropertiesControl()
        {
            InitializeComponent();
            InitializeControls();
        }

        #region Common Control methods
        public void InitializeControls()
        {
            try
            {
                //Initialize the loop video drop down
                LoopVideoValue.Items.Clear();
                LoopVideoValue.Items.Add("True");
                LoopVideoValue.Items.Add("False");
                LoopVideoValue.SelectedValue = "False";

                //Initialize the content type drop down
                CBContentTypeValue.Items.Clear();
                foreach (var contentType in Enum.GetNames(typeof(ContentType)))
                {
                    CBContentTypeValue.Items.Add(contentType);
                }
                CBContentTypeValue.SelectedValue = ContentType.Nothing.ToString();

                //Hide the loop video by default
                LoopVideoValue.Visibility = Visibility.Collapsed;
                LoopVideoLabel.Visibility = Visibility.Collapsed;

                //Set the header value.
                HeaderValue.Text = "";
                ContentValue.Text = "";

                ContentValue.IsReadOnlyCaretVisible = true;
                ContentValue.IsReadOnly = true;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in InitializeControls for Screen Properties: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Code to validate the screen properties control
        /// </summary>
        /// <returns></returns>
        public bool ValidateControl()
        {
            try
            {
                if (!ValidateHeaderValue())
                    return false;

                if (!ValidateContentValue())
                    return false;
                return true;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in ValidateControl for Screen Properties: {0}", ex.Message);
                return false;
            }
        }

        private bool ValidateContentValue()
        {
            var currentCBContentTypeSelection = CBContentTypeValue.SelectedValue.ToString();
            //Empty Content for anything except nothing
            if (String.IsNullOrEmpty(ContentValue.Text) && (!currentCBContentTypeSelection.Equals(ContentType.Nothing.ToString())))
            {
                MessageBox.Show("Content in Screen Properties cannot be left empty unless ContentType = Nothing");
                return false;
            }

            //Verify the file extensions for browse specifically, rest cannot be altered
            var actionContent = ContentValue.Text;
            if (currentCBContentTypeSelection.Equals(ContentType.HTML.ToString()))
            {
                //Can take both local and web hosted URIs
                if (!actionContent.StartsWith("http") && (!(actionContent.EndsWith(".htm") || actionContent.EndsWith(".html"))))
                {
                    MessageBox.Show("Please enter valid value for HTML based URI, it should have an extension html or htm for local files, for web hosted files it should start with http");
                    return false;
                }
            }

            return true;
        }

        private bool ValidateHeaderValue()
        {
            //Invalid header value
            if (!String.IsNullOrEmpty(HeaderValue.Text) && HeaderValue.Text.Length > Constants.MaxCharForHeaderName)
            {
                MessageBox.Show(String.Format("Header value cannot exceed {0} characters", Constants.MaxCharForHeaderName));
                return false;
            }
            return true;
        }

        public void SaveScreenProperties(Tile tile)
        {
            try
            {
                //Get the content Type
                var ct = (ContentType)CBContentTypeValue.SelectedIndex;
                var screenData = new ScreenContent(ct, tile.Id, HeaderValue.Text, ContentValue.Text, (LoopVideoValue.SelectedValue.ToString() == "False" ? false : true));
                MainPage.rippleData.Screen.CreateOrUpdateScreenContent(tile.Id, screenData);

                //Once successful update the corresponding screen content type for the floor tile
                HelperMethods.GetFloorTileForID(tile.Id).CorrespondingScreenContentType = ct;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in SaveScreenProperties for Screen Properties: {0}", ex.Message);
            }
        }

        public void SetScreenProperties(Tile tile)
        {
            try
            {
                if (MainPage.rippleData.Screen.ScreenContents.ContainsKey(tile.Id))
                {
                    //Get the screen data
                    var screenData = MainPage.rippleData.Screen.ScreenContents[tile.Id];

                    //Set the content type
                    CBContentTypeValue.SelectedValue = screenData.Type.ToString();

                    //Set the loop video visibility and value
                    LoopVideoValue.SelectedValue = (screenData.LoopVideo == null) ? "False" : (Convert.ToBoolean(screenData.LoopVideo) ? "True" : "False");
                    if (screenData.Type == ContentType.Video)
                    {
                        LoopVideoLabel.Visibility = Visibility.Visible;
                        LoopVideoValue.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        LoopVideoLabel.Visibility = Visibility.Collapsed;
                        LoopVideoValue.Visibility = Visibility.Collapsed;
                    }

                    //Set the content URI
                    if ((screenData.Type == ContentType.PPT || screenData.Type == ContentType.Image || screenData.Type == ContentType.Video || screenData.Type == ContentType.HTML) && screenData.Content.StartsWith(@"\Assets\"))
                        ContentValue.Text = HelperMethods.TargetAssetsRoot + screenData.Content;
                    else
                        ContentValue.Text = screenData.Content;

                    //Set the header text
                    HeaderValue.Text = screenData.Header;

                    //Set the browse button visibility
                    if (screenData.Type == ContentType.Text || screenData.Type == ContentType.HTML || screenData.Type == ContentType.Nothing)
                        ContentBrowseButton.IsEnabled = false;
                    else
                        ContentBrowseButton.IsEnabled = true;

                    //Set the content box properties
                    if (screenData.Type == ContentType.HTML || screenData.Type == ContentType.Text)
                    {
                        ContentValue.IsReadOnlyCaretVisible = false;
                        ContentValue.IsReadOnly = false;
                    }
                    else
                    {
                        ContentValue.IsReadOnlyCaretVisible = true;
                        ContentValue.IsReadOnly = true;
                    }
                }
                else
                {
                    //Just set the defaults
                    CBContentTypeValue.SelectedValue = ContentType.Nothing.ToString();
                    ContentValue.Text = "";
                    LoopVideoValue.SelectedValue = "False";
                    LoopVideoLabel.Visibility = Visibility.Collapsed;
                    LoopVideoValue.Visibility = Visibility.Collapsed;
                    HeaderValue.Text = "";
                    ContentBrowseButton.IsEnabled = false;
                    ContentValue.IsReadOnlyCaretVisible = true;
                    ContentValue.IsReadOnly = true;
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in SetScreenProperties for Screen Properties: {0}", ex.Message);
            }
        } 
        #endregion

        #region UI Methods
        private void ContentBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var localHTMLFiles = false;
                //Show the open file dialog box to select the content only if the content =! nothing
                var currentCBContentTypeSelection = CBContentTypeValue.SelectedValue.ToString();
                if (!currentCBContentTypeSelection.Equals(ContentType.Nothing.ToString()))
                {
                    var dlgBox = new OpenFileDialog();

                    //Set the filter
                    if (currentCBContentTypeSelection == ContentType.Image.ToString())
                        dlgBox.Filter = "Images(*.jpeg;*.png;*.jpg;*.bmp;)|*.jpeg;*.png;*.jpg;*.bmp;";
                    else if (currentCBContentTypeSelection == ContentType.Video.ToString())
                        dlgBox.Filter = "Videos(*.mp4;*.wmv;)|*.mp4;*.wmv";
                    else if (currentCBContentTypeSelection == ContentType.PPT.ToString())
                        dlgBox.Filter = "Presentation Files(*.ppt;*.pptx;)|*.ppt;*.pptx;";
                    else if (currentCBContentTypeSelection == ContentType.Text.ToString())
                        MessageBox.Show("Directly enter the text in the Content field");
                    else if (currentCBContentTypeSelection == ContentType.HTML.ToString())
                        dlgBox.Filter = "HTML Files(*.htm;*.html;)|*.htm;*.html;";

                    var res = dlgBox.ShowDialog();

                    if (res == DialogResult.OK)
                    {
                        var targetFolder = HelperMethods.TargetAssetsDirectory;
                        var fileExt = Path.GetExtension(dlgBox.FileName).ToLower();
                        if (fileExt.Equals(".mp4") || fileExt.Equals(".wmv"))
                            targetFolder += "\\Videos";
                        else if (fileExt.Equals(".jpeg") || fileExt.Equals(".png") || fileExt.Equals(".jpg") || fileExt.Equals(".bmp"))
                            targetFolder += "\\Images";
                        else if (fileExt.Equals(".html") || fileExt.Equals(".htm"))
                        {
                            localHTMLFiles = true;
                            targetFolder += "\\Animations";
                        }
                        else
                            targetFolder += "\\Docs";

                        //Get the complete fileName
                        var updatedFileName = dlgBox.FileName;
                        if (localHTMLFiles)
                        {
                            targetFolder = HelperMethods.CopyFolder(Path.GetDirectoryName(updatedFileName), targetFolder);
                            updatedFileName = targetFolder + "\\" + Path.GetFileName(updatedFileName);
                        }
                        else
                            updatedFileName = HelperMethods.CopyFile(dlgBox.FileName, targetFolder);

                        if (!String.IsNullOrEmpty(updatedFileName))
                        {
                            ContentValue.Text = updatedFileName;
                            ContentValue.ToolTip = updatedFileName;
                        }
                        else
                        {
                            ContentValue.Text = "";
                            ContentValue.ToolTip = "";
                        }
                    }

                }
                else
                {
                    MessageBox.Show("Please select ContentType as some other value");
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in ContentBrowseButton_Click for Screen Properties: {0}", ex.Message);
            }
        }

        private void CBContentTypeValue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var cb = sender as ComboBox;
                if (cb.SelectedValue != null)
                {
                    //Check visibility of loop video
                    if (cb.SelectedValue.ToString().Equals(ContentType.Video.ToString()))
                    {
                        LoopVideoValue.Visibility = Visibility.Visible;
                        LoopVideoLabel.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        LoopVideoValue.Visibility = Visibility.Collapsed;
                        LoopVideoLabel.Visibility = Visibility.Collapsed;
                    }

                    //Enable/Disable content browse button
                    if (cb.SelectedValue.ToString().Equals(ContentType.Text.ToString()) || cb.SelectedValue.ToString().Equals(ContentType.Nothing.ToString()))
                    {
                        ContentBrowseButton.IsEnabled = false;
                    }
                    else
                        ContentBrowseButton.IsEnabled = true;

                    //Enable/Disable the content textbox
                    if (cb.SelectedValue.ToString().Equals(ContentType.HTML.ToString()) || cb.SelectedValue.ToString().Equals(ContentType.Text.ToString()))
                    {
                        ContentValue.IsReadOnlyCaretVisible = false;
                        ContentValue.IsReadOnly = false;
                    }
                    else
                    {
                        ContentValue.IsReadOnlyCaretVisible = true;
                        ContentValue.IsReadOnly = true;
                    }

                    if(cb.SelectedValue.ToString().Equals(ContentType.Nothing.ToString()))
                    {
                        //Clear the header
                        HeaderValue.Text = "";
                    }

                    //Clear the content box
                    ContentValue.Text = "";
                    ContentValue.ToolTip = "";
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in CBContentTypeValue_SelectionChanged for Screen Properties: {0}", ex.Message);
            }
        }

        private void ContentValue_LostFocus(object sender, RoutedEventArgs e)
        {
            ValidateContentValue();
        }

        private void HeaderValue_LostFocus(object sender, RoutedEventArgs e)
        {
            ValidateHeaderValue();
        } 
        #endregion        
    }
}
