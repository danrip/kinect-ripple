using System.Configuration;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using RippleCommonUtilities;
using RippleDictionary;
using RippleEditor.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Button = System.Windows.Controls.Button;
using Globals = RippleEditor.Utilities.Globals;
using HelperMethods = RippleEditor.Utilities.HelperMethods;
using MessageBox = System.Windows.MessageBox;
using RippleSystemStates = RippleEditor.Utilities.RippleSystemStates;
using Style = System.Windows.Style;
using TUC = RippleEditor.Utilities.TileControl;
using WebBrowser = System.Windows.Forms.WebBrowser;

namespace RippleEditor
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Window
    {
        public static Ripple rippleData;
        private List<TUC> tileList = null;
        private UpperVideoControl UpperVideoContentGrid = null;
        private static MainOptionTile mainOptionGrid;
        private Tile prevSelectedTile = null;

        private static ContentType currentScreenContent = ContentType.Nothing;
        private static String currentVideoURI = String.Empty;
        private static TextBlock tbElement = new TextBlock();
        private static TextBlock fullScreenTbElement = new TextBlock();
        private static Image imgElement = new Image();
        private static Image fullScreenImgElement = new Image();
        public WindowsFormsHost host;
        public WebBrowser browserElement;
        public const string InnerContent = "InnerContent";
        public const string InnerTile = "InnerTile";
        public const string Label = "Label";

        public MainPage()
        {
            try
            {
                InitializeComponent();

                MenuBar.Width = SystemParameters.PrimaryScreenWidth;

                var fileLocation = ConfigurationManager.AppSettings["LogFileLocation"];
                if (String.IsNullOrEmpty(fileLocation))
                    LoggingHelper.StartLogging("RippleEditor");
                else
                    LoggingHelper.StartLogging("RippleEditor", fileLocation);

                InitializeScreenPreviewControls();

                ResetUI();
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in Constructor: {0}", ex.Message);
            }
        }

        private void InitializeScreenPreviewControls()
        {
            try
            {
                //Set image elements properties
                imgElement.Stretch = Stretch.Fill;
                fullScreenImgElement.Stretch = Stretch.Fill;
                //Set text block properties
                tbElement.FontSize = 15;
                tbElement.Margin = new Thickness(20, 0, 20, 0);
                tbElement.VerticalAlignment = VerticalAlignment.Center;
                tbElement.TextWrapping = TextWrapping.Wrap;
                fullScreenTbElement.FontSize = 15;
                fullScreenTbElement.Margin = new Thickness(20, 0, 20, 0);
                fullScreenTbElement.VerticalAlignment = VerticalAlignment.Center;
                fullScreenTbElement.TextWrapping = TextWrapping.Wrap;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in InitializeScreenPreviewControls: {0}", ex.Message);
            }
        }

        private void ResetUI()
        {
            try
            {
                //Hide the properties windows after resetting them
                AppPropControl.Visibility = Visibility.Collapsed;
                AppPropControl.InitializeControls();
                FloorPropControl.Visibility = Visibility.Collapsed;
                FloorPropControl.InitializeControls();
                ScreenPropControl.Visibility = Visibility.Collapsed;
                ScreenPropControl.InitializeControls();

                DefaultView.Visibility = Visibility.Visible;

                //Reset the screen preview
                ScreenUI.Visibility = Visibility.Collapsed;
                PlainScreen.Visibility = Visibility.Visible;
                ScreenPreviewLabel.Visibility = Visibility.Collapsed;
                RefreshButton.Visibility = Visibility.Collapsed;

                Globals.ResetGlobals();

                UnregisterNames();

                Globals.currentAppState = RippleSystemStates.None;
                prevSelectedTile = null;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in ResetUI: {0}", ex.Message);
            }
        }

        #region Menu Clicks
        private void NewFile_Click(object sender, RoutedEventArgs e)
        {
            //Check whether anything is open right now
            if (Globals.IsProjectOpen)
            {
                if (!SaveExistingProject())
                    return;
                ResetUI();
            }

            //Which type of template to select
            var customBox = new NewTemplateSelectorBox();
            var resNew = Convert.ToBoolean(customBox.ShowDialog());
            if (resNew)
            {
                //Get the selected template
                Globals.CurrentTemplate = customBox.SelectedItem;
                Globals.CurrentFileLocation = GetRippleXMLFileLocation();
                Globals.IsProjectOpen = true;
                //Get the basic XML for the selected template
                var xmlFilePath = HelperMethods.GetXMLFileForTemplate(Globals.CurrentTemplate);
                //Get the basic object for the given XML
                rippleData = Dictionary.GetRippleDictionaryFromFile(xmlFilePath);
                //Render the UI
                ArrangeFloor();

            }

        }

        private bool SaveExistingProject()
        {
            try
            {
                //IF yes, prompt to close the same before creating a new one
                var msgBoxRes = MessageBox.Show("Saving the exisiting one");
                //IF yes, save the current dictionary object to a XML file designated by CurrentFileLocation
                if (msgBoxRes == MessageBoxResult.OK)
                {
                    //Verify the assets and make all the paths relative in the XML
                    VerifyAssetPaths();

                    if (!RippleXMLWriter.TryWriteToXML(rippleData, Globals.CurrentFileLocation))
                    {
                        MessageBox.Show("Please try again, it failed");
                        return false;
                    }
                }
                else
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in SaveExistingProject: {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Method to make assets path relative
        /// </summary>
        private void VerifyAssetPaths()
        {
            try
            {
                //Verify Animation Content
                if ((!String.IsNullOrEmpty(rippleData.Floor.Start.Animation.Content)) && (!rippleData.Floor.Start.Animation.Content.StartsWith(@"\Assets\")) && (rippleData.Floor.Start.Animation.Content.Contains(@"\Assets\")))
                {
                    rippleData.Floor.Start.Animation.Content = rippleData.Floor.Start.Animation.Content.Substring(rippleData.Floor.Start.Animation.Content.LastIndexOf(@"\Assets\"));
                }

                //Verify transition music
                if ((!String.IsNullOrEmpty(rippleData.Floor.Transition.Music)) && (!rippleData.Floor.Transition.Music.StartsWith(@"\Assets\")) && (rippleData.Floor.Transition.Music.Contains(@"\Assets\")))
                {
                    rippleData.Floor.Transition.Music = rippleData.Floor.Transition.Music.Substring(rippleData.Floor.Transition.Music.LastIndexOf(@"\Assets\"));
                }

                //Verify Upper tile content
                if ((!String.IsNullOrEmpty(rippleData.Floor.UpperTile.Content)) && (!rippleData.Floor.UpperTile.Content.StartsWith(@"\Assets\")) && (rippleData.Floor.UpperTile.Content.Contains(@"\Assets\")))
                {
                    rippleData.Floor.UpperTile.Content = rippleData.Floor.UpperTile.Content.Substring(rippleData.Floor.UpperTile.Content.LastIndexOf(@"\Assets\"));
                }

                //Verify Tiles
                foreach (var root in rippleData.Floor.Tiles.Values)
                {
                    //Content
                    if ((!String.IsNullOrEmpty(root.Content)) && (!root.Content.StartsWith(@"\Assets\")) && (root.Content.Contains(@"\Assets\")))
                    {
                        root.Content = root.Content.Substring(root.Content.LastIndexOf(@"\Assets\"));
                    }
                    //Action URI
                    if ((!String.IsNullOrEmpty(root.ActionURI)) && (!root.ActionURI.StartsWith(@"\Assets\")) && (root.ActionURI.Contains(@"\Assets\")))
                    {
                        root.ActionURI = root.ActionURI.Substring(root.ActionURI.LastIndexOf(@"\Assets\"));
                    }

                    if (root.SubTiles == null)
                        continue;

                    foreach (var sub in root.SubTiles.Values)
                    {
                        //Content
                        if ((!String.IsNullOrEmpty(sub.Content)) && (!sub.Content.StartsWith(@"\Assets\")) && (sub.Content.Contains(@"\Assets\")))
                        {
                            sub.Content = sub.Content.Substring(sub.Content.LastIndexOf(@"\Assets\"));
                        }
                        //Action URI
                        if ((!String.IsNullOrEmpty(sub.ActionURI)) && (!sub.ActionURI.StartsWith(@"\Assets\")) && (sub.ActionURI.Contains(@"\Assets\")))
                        {
                            sub.ActionURI = sub.ActionURI.Substring(sub.ActionURI.LastIndexOf(@"\Assets\"));
                        }
                    }
                }

                //Verify screen contents
                foreach (var sc in rippleData.Screen.ScreenContents.Values)
                {
                    //Validate content
                    if ((!String.IsNullOrEmpty(sc.Content)) && (!sc.Content.StartsWith(@"\Assets\")) && (sc.Content.Contains(@"\Assets\")))
                    {
                        sc.Content = sc.Content.Substring(sc.Content.LastIndexOf(@"\Assets\"));
                    }
                    //Validate corresponding screen content type - applicable only for tiles
                    if (sc.Id.StartsWith("Tile"))
                        HelperMethods.GetFloorTileForID(sc.Id).CorrespondingScreenContentType = sc.Type;
                }

            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in VerifyAssetPaths: {0}", ex.Message);
            }
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            //Check whether anything is open right now
            if (Globals.IsProjectOpen)
            {
                SaveExistingProject();
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Check whether anything is open right now
                if (Globals.IsProjectOpen)
                {
                    if (!SaveExistingProject())
                        return;
                    ResetUI();
                }

                //Open a browse dialog box to select xml, validate the xml
                //Opportunity to browse for content files
                var dlgBox = new OpenFileDialog();
                dlgBox.Filter = "XML Files(*.xml)|*.xml";
                var res = dlgBox.ShowDialog();
                if (res == System.Windows.Forms.DialogResult.OK)
                {
                    Globals.CurrentFileLocation = dlgBox.FileName;
                    Globals.IsProjectOpen = true;
                    //Get the basic XML for the selected template
                    var xmlFilePath = Globals.CurrentFileLocation;

                    try
                    {
                        //Get the basic object for the given XML
                        rippleData = Dictionary.GetRippleDictionaryFromFile(xmlFilePath);

                        if (rippleData != null)
                            ArrangeFloor();
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Invalid XML");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in OpenFile_Click: {0}", ex.Message);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            //Check whether anything is open right now
            if (Globals.IsProjectOpen)
            {
                if (!SaveExistingProject())
                    return;
            }

            //Directly exit
            Close();
        }

        private void AboutUs_Click(object sender, RoutedEventArgs e)
        {
            //Show a message box
            MessageBox.Show("This is Ripple :-)");
        }
        #endregion

        /// <summary>
        /// Method to dispose old UI tiles and un register the names
        /// </summary>
        private void UnregisterNames()
        {
            try
            {
                MainContainer.Children.Clear();

                if (UpperVideoContentGrid != null)
                {
                    UpperVideoContentGrid.FloorVideoControl.Stop();
                    UpperVideoContentGrid.UnregisterNames(this);
                    UpperVideoContentGrid = null;
                }

                if (tileList != null)
                {
                    foreach (var tile in tileList)
                    {
                        tile.UnregisterNames(this);
                    }
                    tileList = null;
                }

                if (mainOptionGrid != null)
                {
                    mainOptionGrid.UnregisterNames(this);
                    mainOptionGrid = null;
                }

                UpdateLayout();
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in Unregister Names {0}", ex.Message);
            }
        }

        /// <summary>
        /// Function that renders the floor UI
        /// </summary>
        private void ArrangeFloor()
        {
            try
            {
                MainContainer.Children.Clear();
                Dictionary<String, Tile> inputDictionary = null;

                CopyDefaultAssets();

                inputDictionary = rippleData.Floor.Tiles;

                tileList = new List<TUC>();
                //Add the upper tile
                TUC nTile;
                //Upper tile properties
                UpperVideoContentGrid = new UpperVideoControl(this);
                UpperVideoContentGrid.ControlWidth = rippleData.Floor.UpperTile.Style.Width;
                UpperVideoContentGrid.ControlHeight = rippleData.Floor.UpperTile.Style.Height;
                UpperVideoContentGrid.SetMargin(rippleData.Floor.UpperTile.Coordinate.X, rippleData.Floor.UpperTile.Coordinate.Y);
                UpperVideoContentGrid.FloorVideoControl.Source = new Uri(HelperMethods.CurrentDirectory + "\\.." + rippleData.Floor.UpperTile.Content);
                UpperVideoContentGrid.FloorVideoControl.MediaEnded += FloorVideoControl_MediaEnded;
                UpperVideoContentGrid.FloorVideoControl.Play();

                //Add rest of the tiles
                foreach (var tile in inputDictionary.Values)
                {
                    nTile = new TUC();
                    nTile.SetNames(tile.Id, this);
                    nTile.TileBackground = tile.Color;
                    nTile.TileIDButton.Click += TileIDButton_Click;
                    nTile.TileWidth = tile.Style.Width;
                    nTile.TileHeight = tile.Style.Height;
                    nTile.SetMargin(tile.Coordinate.X, tile.Coordinate.Y);
                    nTile.TileIDLabelText = tile.Name;
                    tileList.Add(nTile);
                }

                //Main Option grid properties
                mainOptionGrid = new MainOptionTile(this);
                mainOptionGrid.SetMargin(rippleData.Floor.Tiles["Tile0"].Coordinate.Y, rippleData.Floor.Tiles["Tile0"].Style.Height);
                mainOptionGrid.ControlWidth = rippleData.Floor.UpperTile.Style.Width;
                mainOptionGrid.ControlHeight = rippleData.Floor.UpperTile.Style.Height;

                //Overlay Image
                //Controls.OverlayImageControl overlayImage = new Controls.OverlayImageControl(this);
                //overlayImage.SetMargin(rippleData.Floor.Tiles["Tile0"].Style.Height);

                //Add upper video tile to the main UI
                MainContainer.Children.Add(UpperVideoContentGrid);

                //Add the tile list to the main UI
                foreach (var tile in tileList)
                {
                    MainContainer.Children.Add(tile);
                }

                //Add the Main Option grid to the Main UI
                MainContainer.Children.Add(mainOptionGrid);

                //Add the overlay image
                //this.MainContainer.Children.Add(overlayImage);

                //Show application properties
                AppPropControl.SetApplicationProperties();
                AppPropControl.Visibility = Visibility.Visible;
                ScreenPreviewLabel.Visibility = Visibility.Visible;
                RefreshButton.Visibility = Visibility.Visible;
                DefaultView.Visibility = Visibility.Collapsed;

                //Set the globals
                Globals.CurrentlySelectedParent = 0;
                Globals.currentAppState = RippleSystemStates.Start;
                ResetColorsForMainOption(0);

                PromptBlock.Text = "";

                UpdateLayout();
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Could not layout the floor, went wrong in ArrangeFloor {0}", ex.Message);
            }

        }

        void FloorVideoControl_MediaEnded(object sender, RoutedEventArgs e)
        {
            UpperVideoContentGrid.FloorVideoControl.Source = new Uri(HelperMethods.CurrentDirectory + "\\.." + rippleData.Floor.UpperTile.Content);
            UpperVideoContentGrid.FloorVideoControl.Play();
        }

        private void CopyDefaultAssets()
        {
            try
            {
                HelperMethods.CopyFolder(HelperMethods.DefaultAssetsDirectory + "\\Animations\\WaterRipples", HelperMethods.TargetAssetsDirectory + "\\Animations");
                HelperMethods.CopyFile(HelperMethods.DefaultAssetsDirectory + "\\Audio\\transition_music.wav", HelperMethods.TargetAssetsDirectory + "\\Audio");
                HelperMethods.CopyFile(HelperMethods.DefaultAssetsDirectory + "\\Docs\\PrinterReceipt.pptx", HelperMethods.TargetAssetsDirectory + "\\Docs");
                HelperMethods.CopyFile(HelperMethods.DefaultAssetsDirectory + "\\Images\\default_start.png", HelperMethods.TargetAssetsDirectory + "\\Images");
                HelperMethods.CopyFile(HelperMethods.DefaultAssetsDirectory + "\\Images\\pptend.png", HelperMethods.TargetAssetsDirectory + "\\Images");
                HelperMethods.CopyFile(HelperMethods.DefaultAssetsDirectory + "\\Images\\GoToStart.png", HelperMethods.TargetAssetsDirectory + "\\Images");
                HelperMethods.CopyFile(HelperMethods.DefaultAssetsDirectory + "\\Videos\\RippleIntro.mp4", HelperMethods.TargetAssetsDirectory + "\\Videos");
                HelperMethods.CopyFile(HelperMethods.DefaultAssetsDirectory + "\\Videos\\Ripple_IntroUnlock.mp4", HelperMethods.TargetAssetsDirectory + "\\Videos");
                HelperMethods.CopyFile(HelperMethods.DefaultAssetsDirectory + "\\Videos\\ripple_start.mp4", HelperMethods.TargetAssetsDirectory + "\\Videos");
                HelperMethods.CopyFile(HelperMethods.DefaultAssetsDirectory + "\\Videos\\GoToStart.mp4", HelperMethods.TargetAssetsDirectory + "\\Videos");
                HelperMethods.CopyFile(HelperMethods.DefaultAssetsDirectory + "\\Videos\\Ripple_wave.mp4", HelperMethods.TargetAssetsDirectory + "\\Videos");
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in CopyDefaultAssets: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Handles clicks for all UI elements
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void TileIDButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Get the tile ID
                var TileID = (sender as Button).Name;
                TileID = TileID.Substring(0, TileID.LastIndexOf("Button"));
                OnTileSelected(TileID);
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in Tile Click for TileID {0}: {1}", (sender as Button).Name, ex.Message);
            }
        }

        private void OnTileSelected(string TileID)
        {
            if (Globals.currentAppState == RippleSystemStates.ActionContent && (!TileID.Equals("Tile0")))
            {
                PromptBlock.Text = "Please click on Start to get to the main options";
            }
            else
            {
                PromptBlock.Text = "";
                OnSelectedBox(Convert.ToInt16(TileID.Substring(TileID.LastIndexOf("Tile") + 4)));
            }
        }

        private bool ShowPropertiesForGivenTile(Tile iTile)
        {
            try
            {
                //Save the existing after validation
                if (prevSelectedTile != null && AppPropControl.ValidateControl() && FloorPropControl.ValidateControl() && ScreenPropControl.ValidateControl())
                {
                    ScreenPropControl.SaveScreenProperties(prevSelectedTile);
                    FloorPropControl.SaveFloorProperties(prevSelectedTile);
                    AppPropControl.SaveApplicationProperties();

                    AppPropControl.SetApplicationProperties();
                    ScreenPropControl.SetScreenProperties(iTile);
                    FloorPropControl.SetFloorProperties(iTile);
                    prevSelectedTile = iTile;
                }
                else if (prevSelectedTile == null)
                {
                    AppPropControl.SetApplicationProperties();
                    ScreenPropControl.SetScreenProperties(iTile);
                    FloorPropControl.SetFloorProperties(iTile);
                    prevSelectedTile = iTile;
                }
                else
                {
                    MessageBox.Show(String.Format("Unable to validate values for Tile ID: {0}", prevSelectedTile.Id));
                    return false;
                }

                AppPropControl.Visibility = Visibility.Visible;
                FloorPropControl.Visibility = Visibility.Visible;
                ScreenPropControl.Visibility = Visibility.Visible;
                ShowContentPreview(iTile.Id);
                return true;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in ShowProperties for Tile {0} : {1}", iTile.Id, ex.Message);
                return false;
            }
        }

        #region Options Code
        private void OnSelectedBox(int value)
        {
            try
            {
                Globals.PreviouslySelectedBox = Globals.CurrentlySelectedBox;
                Globals.CurrentlySelectedBox = value;
                #region Box selected
                //Start selected
                if (value == 0)
                {
                    OnStartSelected();
                }
                //Boxes selected
                else
                {
                    OnOptionSelected(value);
                }
                #endregion
            }
            catch (Exception ex)
            {
                //Do nothing
                LoggingHelper.LogTrace(1, "Went wrong in on selected box {0}", ex.Message);
            }
        }

        public void OnStartSelected()
        {
            if (Globals.currentAppState == RippleSystemStates.Start || Globals.currentAppState == RippleSystemStates.OptionSelected || Globals.currentAppState == RippleSystemStates.ActionContent)
            {
                if (ShowPropertiesForGivenTile(HelperMethods.GetFloorTileForID("Tile0")))
                {
                    Globals.currentAppState = RippleSystemStates.Start;
                    Globals.CurrentlySelectedParent = 0;
                    ShowStartOptions();
                }
                else
                    Globals.CurrentlySelectedBox = Globals.PreviouslySelectedBox;
            }
        }

        public void OnOptionSelected(int BoxNumber)
        {
            try
            {
                String tileID = null;
                TileAction action;

                //User selected Main Option
                if (Globals.currentAppState == RippleSystemStates.Start)
                {
                    tileID = "Tile" + BoxNumber;
                    if (!ShowPropertiesForGivenTile(HelperMethods.GetFloorTileForID(tileID)))
                    {
                        Globals.CurrentlySelectedBox = Globals.PreviouslySelectedBox;
                        return;
                    }
                    Globals.CurrentlySelectedParent = BoxNumber;
                    Globals.currentAppState = RippleSystemStates.OptionSelected;

                    action = rippleData.Floor.Tiles[tileID].Action;

                    //Call the tile transition for Standard Action tiles
                    if (action == TileAction.Standard)
                    {
                        //Layout the options
                        LayoutTiles(BoxNumber);
                    }
                    else if (action == TileAction.Logout)
                    {
                        ProcessTileActionForLogout();
                    }
                    else if (action == TileAction.HTML)
                    {
                        //Layout the options
                        LayoutTiles(BoxNumber);
                        ProcessTileActionForAnimation(rippleData.Floor.Tiles[tileID].ActionURI);
                    }
                    else if (action == TileAction.QRCode)
                    {
                        //Layout the options
                        LayoutTiles(BoxNumber);
                        //Show the main grid
                        ((Grid)FindName("MainOptionGrid")).Visibility = Visibility.Visible;
                        ((TextBlock)FindName("MainOptionGridLabel")).Text = rippleData.Floor.Tiles[tileID].Name;
                        ProcessTileActionForQRCode(rippleData.Floor.Tiles[tileID].ActionURI);
                    }
                    else if (action == TileAction.Nothing || action == TileAction.NothingOnFloor)
                    {
                        //Layout the options
                        //LayoutTiles(BoxNumber);
                        Globals.currentAppState = RippleSystemStates.Start;
                    }

                    UpdateLayout();
                }

                //User selected Sub Option
                else if (Globals.currentAppState == RippleSystemStates.OptionSelected)
                {
                    var parentTileID = "Tile" + Globals.CurrentlySelectedParent;
                    tileID = "Tile" + Globals.CurrentlySelectedParent + "SubTile" + BoxNumber;
                    action = rippleData.Floor.Tiles[parentTileID].SubTiles[tileID].Action;
                    //Globals.currentAppState = RippleSystemStates.SubOptionSelected;

                    if (!ShowPropertiesForGivenTile(HelperMethods.GetFloorTileForID(tileID)))
                    {
                        Globals.CurrentlySelectedBox = Globals.PreviouslySelectedBox;
                        return;
                    }

                    //Call the tile transition for Standard Action tiles
                    if (action == TileAction.Standard)
                    {
                        LayoutTiles(Globals.CurrentlySelectedParent);
                    }
                    else if (action == TileAction.Nothing || action == TileAction.NothingOnFloor)
                    {
                        //LayoutTiles(Globals.CurrentlySelectedParent);
                    }
                    else if (action == TileAction.Logout)
                    {
                        ProcessTileActionForLogout();
                    }
                    else if (action == TileAction.HTML)
                    {
                        LayoutTiles(Globals.CurrentlySelectedParent);
                        ProcessTileActionForAnimation(rippleData.Floor.Tiles[parentTileID].SubTiles[tileID].ActionURI);
                    }
                    else if (action == TileAction.QRCode)
                    {
                        LayoutTiles(Globals.CurrentlySelectedParent);
                        ((TextBlock)FindName("MainOptionGridLabel")).Text = rippleData.Floor.Tiles[parentTileID].SubTiles[tileID].Name;
                        ProcessTileActionForQRCode(rippleData.Floor.Tiles[parentTileID].SubTiles[tileID].ActionURI);
                    }

                    UpdateLayout();
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in OnOptionSelected for box number {0}: {1}", BoxNumber, ex.Message);
            }

        }
        #endregion

        #region FloorContentProcessing
        public void ShowStartOptions()
        {
            try
            {
                //Show the floor options
                //((System.Windows.Controls.Image)this.FindName("OverlayImage")).Visibility = Visibility.Collapsed;
                ((Grid)FindName("MainOptionGrid")).Visibility = Visibility.Collapsed;

                LayoutTiles(0);

                //Update the UI
                UpdateLayout();
            }
            catch (Exception ex)
            {
                //Do nothing
                LoggingHelper.LogTrace(1, "Went wrong in ShowStartOptions UI {0}", ex.Message);
            }

        }

        private void LayoutTiles(int parent)
        {
            try
            {
                String tileID = null;
                //Reset the labels to blank to setup a blank floor
                if (parent < 0)
                {
                    //Reset the labels
                    ClearFloorLabels();
                }
                else if (parent == 0)
                {
                    foreach (var item in rippleData.Floor.Tiles.Values)
                    {
                        try
                        {
                            //Set the label for the tile
                            SetAttributesForWindowsControls<TextBlock>(item.Id + Label, "Text", item.Name);

                            //Clear the inner content grid either way
                            ((Grid)FindName(InnerContent + item.Id)).Children.Clear();

                            //Set the content for the tile if the tile type is not text
                            if (item.TileType != TileType.Text && (!String.IsNullOrEmpty(item.Content)))
                                ShowTileContent(item.Id, item.TileType, item.Content);
                        }
                        catch (Exception)
                        {
                            //Do nothing
                        }
                    }
                }
                else if (parent > 0)
                {
                    foreach (var item in rippleData.Floor.Tiles["Tile" + parent].SubTiles.Values)
                    {
                        try
                        {
                            tileID = item.Id.Substring(item.Id.LastIndexOf("Tile"));

                            //Set the label for the tile
                            SetAttributesForWindowsControls<TextBlock>(tileID + Label, "Text", item.Name);

                            //Clear the inner content grid either way
                            ((Grid)FindName(InnerContent + tileID)).Children.Clear();

                            //Set the content for the tile if the tile type is not text
                            if (item.TileType != TileType.Text && (!String.IsNullOrEmpty(item.Content)))
                                ShowTileContent(tileID, item.TileType, item.Content);
                        }
                        catch (Exception ex)
                        {
                            //Do nothing
                            LoggingHelper.LogTrace(1, "Tile Layout failed for {0} : {1}", item.Id, ex.Message);
                        }
                    }
                    ((Grid)FindName("MainOptionGrid")).Visibility = Visibility.Visible;
                    ((TextBlock)FindName("MainOptionGridLabel")).Text = rippleData.Floor.Tiles["Tile" + Globals.CurrentlySelectedParent].Name;
                }
                UpdateLayout();

                //Call tile transitions
                ResetColorsForMainOption(Globals.CurrentlySelectedParent);
                //DoTileTransitionForMainOption(Globals.CurrentlySelectedParent);
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Layout Tiles failed for parent {0} : {1}", parent, ex.Message);
            }
        }

        private void ShowTileContent(String tileID, TileType tileType, String contentURI)
        {
            switch (tileType)
            {
                case TileType.OnlyMedia:
                    //Check the content type
                    //Image
                    if (contentURI.Contains("\\Assets\\Images\\"))
                    {
                        var img = new Image();
                        img.Source = new BitmapImage(new Uri(contentURI));
                        img.Style = (Style)App.Current.FindResource("TileImageStyle");
                        ((Grid)FindName(InnerContent + tileID)).Children.Add(img);
                    }
                    //Video
                    else if (contentURI.Contains("\\Assets\\Videos\\"))
                    {
                        var video = new MediaElement();
                        video.Source = new Uri(contentURI);
                        ((Grid)FindName(InnerContent + tileID)).Children.Add(video);
                        video.Play();
                    }
                    break;
                case TileType.TextThumbnail:
                    var thum_img = new Image();
                    thum_img.Source = new BitmapImage(new Uri(contentURI));
                    thum_img.Style = (Style)App.Current.FindResource("ThumbnailImageStyle");
                    ((Grid)FindName(InnerContent + tileID)).Children.Add(thum_img);
                    break;
                case TileType.LiveTile:
                    var tile_img = new Image();
                    tile_img.Name = "ImageAndText" + InnerContent + tileID;
                    tile_img.Source = new BitmapImage(new Uri(contentURI));
                    tile_img.Style = (Style)App.Current.FindResource("TileImageStyle");
                    ((Grid)FindName(InnerContent + tileID)).Children.Add(tile_img);
                    UpdateLayout();
                    break;
            }
        }

        private void ClearFloorLabels()
        {
            foreach (var item in rippleData.Floor.Tiles.Values)
            {
                SetAttributesForWindowsControls<TextBlock>(item.Id + Label, "Text", "");
                ((Grid)FindName(InnerContent + item.Id)).Children.Clear();
            }

            //Set the start label
            ((TextBlock)FindName("Tile0Label")).Text = rippleData.Floor.Tiles["Tile0"].Name;
        }

        private void ResetColorsForMainOption(int Parent)
        {
            //Set the background value for Start
            ((Button)FindName("Tile0Button")).Background = new SolidColorBrush(rippleData.Floor.Tiles["Tile0"].Color);
            //Set the colors
            if (Parent > 0)
            {
                ((Grid)FindName("MainOptionGrid")).Background = new SolidColorBrush(rippleData.Floor.Tiles["Tile" + Parent].Color);

                foreach (var item in rippleData.Floor.Tiles["Tile" + Parent].SubTiles.Values)
                {
                    SetAttributesForWindowsControls<Button>(item.Id.Substring(item.Id.LastIndexOf("Tile")) + "Button", "Background", new SolidColorBrush(item.Color));
                }
            }
            else if (Parent == 0)
            {
                foreach (var item in rippleData.Floor.Tiles.Values)
                {
                    SetAttributesForWindowsControls<Button>(item.Id + "Button", "Background", new SolidColorBrush(item.Color));
                }
            }

            UpdateLayout();
        }

        private void SetAttributesForWindowsControls<InstanceType>(string objectName, string propertyName, object propertyValue)
        {
            var objectInstanceType = (InstanceType)FindName(objectName);

            if (objectInstanceType != null)
            {
                var prop = typeof(InstanceType).GetProperty(propertyName);
                prop.SetValue(objectInstanceType, propertyValue, null);
            }
        }

        #region TileActions
        private void ProcessTileActionForAnimation(string actionURI)
        {
            Globals.currentAppState = RippleSystemStates.ActionContent;
            ResetColorsForMainOption(Globals.CurrentlySelectedParent);
            ClearFloorLabels();
            PromptBlock.Text = "Animation would show up on the floor from location " + actionURI;
        }

        private void ProcessTileActionForQRCode(string actionURI)
        {
            Globals.currentAppState = RippleSystemStates.ActionContent;
            ResetColorsForMainOption(Globals.CurrentlySelectedParent);
            ClearFloorLabels();
            PromptBlock.Text = "QRCode would show up on the floor for URI: " + actionURI;
        }

        private void ProcessTileActionForLogout()
        {
            Globals.currentAppState = RippleSystemStates.ActionContent;
            ResetColorsForMainOption(Globals.CurrentlySelectedParent);
            ClearFloorLabels();
            PromptBlock.Text = "System will simply logout";
        }

        #endregion
        #endregion

        #region Content Projection methods

        private void ShowContentPreview(string tileID)
        {
            try
            {
                if (rippleData.Screen.ScreenContents.ContainsKey(tileID) && rippleData.Screen.ScreenContents[tileID].Type != ContentType.Nothing)
                {
                    ScreenUI.Visibility = Visibility.Visible;
                    PlainScreen.Visibility = Visibility.Collapsed;
                    ProjectContent(rippleData.Screen.ScreenContents[tileID]);
                }
                else
                {
                    VideoControl.Source = null;
                    FullScreenVideoControl.Source = null;

                    //Clear the header text
                    TitleLabel.Text = "";

                    ScreenUI.Visibility = Visibility.Collapsed;
                    PlainScreen.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in ShowContentPreview {0} : {1}", tileID, ex.Message);
            }
        }

        /// <summary>
        /// Identifies the content type and project accordingly
        /// </summary>
        /// <param name="screenContent"></param>
        private void ProjectContent(ScreenContent screenContent)
        {
            VideoControl.Source = null;
            FullScreenVideoControl.Source = null;

            //Clear the header text
            TitleLabel.Text = "";

            if (browserElement != null)
                browserElement.Dispose();
            browserElement = null;
            if (host != null)
                host.Dispose();
            host = null;


            currentScreenContent = screenContent.Type;
            var contentLocation = String.Empty;
            if (screenContent.Content.StartsWith(@"\Assets\"))
            {
                contentLocation = HelperMethods.TargetAssetsRoot + screenContent.Content;
            }
            else
            {
                contentLocation = screenContent.Content;
            }
            switch (screenContent.Type)
            {
                case ContentType.HTML:
                    ShowBrowser(contentLocation, screenContent.Header);
                    break;
                case ContentType.Image:
                    ShowImage(contentLocation, screenContent.Header);
                    break;
                case ContentType.PPT:
                    ShowPPT(contentLocation, screenContent.Header);
                    break;
                case ContentType.Text:
                    ShowText(screenContent.Content, screenContent.Header);
                    break;
                case ContentType.Video:
                    ShowVideo(contentLocation, screenContent.Header);
                    break;
            }
        }

        /// <summary>
        /// Code to project a video
        /// If the Header value is not provided, the content is projected in full screen mode
        /// </summary>
        /// <param name="Content"></param>
        /// <param name="header"></param>
        private void ShowVideo(String Content, String header)
        {
            try
            {
                //Check if header is null
                //Null - Show full screen content
                if (String.IsNullOrEmpty(header))
                {
                    //Show the full screen video control 
                    currentVideoURI = Content;
                    FullScreenVideoControl.Source = new Uri(currentVideoURI);
                    FullScreenContentGrid.Visibility = Visibility.Collapsed;
                    FullScreenVideoGrid.Visibility = Visibility.Visible;
                    FullScreenVideoControl.Play();
                }
                else
                {
                    TitleLabel.Text = header;
                    currentVideoURI = Content;
                    VideoControl.Source = new Uri(currentVideoURI);
                    ContentGrid.Visibility = Visibility.Collapsed;
                    FullScreenContentGrid.Visibility = Visibility.Collapsed;
                    FullScreenVideoGrid.Visibility = Visibility.Collapsed;
                    VideoControl.Visibility = Visibility.Visible;
                    VideoGrid.Visibility = Visibility.Visible;
                    VideoControl.Play();
                }
                UpdateLayout();
            }
            catch (Exception)
            {
            }

        }

        /// <summary>
        /// Code to display text
        /// If the Header value is not provided, the content is projected in full screen mode
        /// </summary>
        /// <param name="Content"></param>
        /// <param name="header"></param>
        private void ShowText(String Content, String header)
        {
            try
            {
                //Check if header is null
                //Null - Show full screen content
                if (String.IsNullOrEmpty(header))
                {
                    //Show the full screen control with text  
                    fullScreenTbElement.Text = Content;
                    FullScreenContentGrid.Children.Clear();
                    FullScreenContentGrid.Children.Add(fullScreenTbElement);
                    FullScreenContentGrid.Visibility = Visibility.Visible;
                    FullScreenVideoGrid.Visibility = Visibility.Collapsed;
                }
                else
                {
                    TitleLabel.Text = header;
                    tbElement.Text = Content;
                    ContentGrid.Children.Clear();
                    ContentGrid.Children.Add(tbElement);
                    ContentGrid.Visibility = Visibility.Visible;
                    FullScreenContentGrid.Visibility = Visibility.Collapsed;
                    FullScreenVideoGrid.Visibility = Visibility.Collapsed;
                    VideoGrid.Visibility = Visibility.Collapsed;
                }
                UpdateLayout();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Code to project a PPT
        /// If the Header value is not provided, the content is projected in full screen mode
        /// </summary>
        /// <param name="Content"></param>
        /// <param name="header"></param>
        private void ShowPPT(String Content, String header)
        {
            try
            {
                ShowText(String.Format("PPT {0} Will show up", Content), null);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Code to project an image
        /// If the Header value is not provided, the content is projected in full screen mode
        /// </summary>
        /// <param name="Content"></param>
        /// <param name="header"></param>
        private void ShowImage(String Content, String header)
        {
            try
            {
                //Check if header is null
                //Null - Show full screen content
                if (String.IsNullOrEmpty(header))
                {
                    //Show the full screen control with text  
                    fullScreenImgElement.Source = new BitmapImage(new Uri(Content));
                    FullScreenContentGrid.Children.Clear();
                    FullScreenContentGrid.Children.Add(fullScreenImgElement);
                    FullScreenContentGrid.Visibility = Visibility.Visible;
                    FullScreenVideoGrid.Visibility = Visibility.Collapsed;
                }
                else
                {
                    TitleLabel.Text = header;
                    imgElement.Source = new BitmapImage(new Uri(Content));
                    ContentGrid.Children.Clear();
                    ContentGrid.Children.Add(imgElement);
                    ContentGrid.Visibility = Visibility.Visible;
                    FullScreenContentGrid.Visibility = Visibility.Collapsed;
                    FullScreenVideoGrid.Visibility = Visibility.Collapsed;
                    VideoGrid.Visibility = Visibility.Collapsed;
                }
                UpdateLayout();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Code to show browser based content, applicable for URL's
        /// If the Header value is not provided, the content is projected in full screen mode
        /// </summary>
        /// <param name="Content"></param>
        /// <param name="header"></param>
        private void ShowBrowser(String Content, String header)
        {
            try
            {
                //Check if header is null
                //Null - Show full screen content
                if (String.IsNullOrEmpty(header))
                {
                    //Show the full screen video control  
                    //Display HTML content
                    host = new WindowsFormsHost();
                    browserElement = new WebBrowser();
                    browserElement.ScriptErrorsSuppressed = true;
                    host.Child = browserElement;
                    FullScreenContentGrid.Children.Clear();
                    FullScreenContentGrid.Children.Add(host);
                    FullScreenContentGrid.Visibility = Visibility.Visible;
                    FullScreenVideoGrid.Visibility = Visibility.Collapsed;
                }
                else
                {
                    TitleLabel.Text = header;
                    host = new WindowsFormsHost();
                    browserElement = new WebBrowser();
                    browserElement.ScriptErrorsSuppressed = true;
                    host.Child = browserElement;
                    ContentGrid.Children.Clear();
                    ContentGrid.Children.Add(host);
                    ContentGrid.Visibility = Visibility.Visible;
                    FullScreenContentGrid.Visibility = Visibility.Collapsed;
                    FullScreenVideoGrid.Visibility = Visibility.Collapsed;
                    VideoGrid.Visibility = Visibility.Collapsed;
                }
                var fileLocation = Content;
                var pageUri = String.Empty;
                //Local file
                if (File.Exists(fileLocation))
                {
                    var PathParts = fileLocation.Split(new char[] { ':' });
                    pageUri = "file://127.0.0.1/" + PathParts[0] + "$" + PathParts[1];
                }
                //Web hosted file
                else
                {
                    pageUri = Content;
                }
                browserElement.Navigate(pageUri);
                UpdateLayout();
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in Show Browser {0}", ex.Message);
            }
        }
        #endregion

        #region Helper Methods
        private string GetRippleXMLFileLocation()
        {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\..\\RippleXML.xml";
        }
        #endregion

        private void VideoControl_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (currentScreenContent == ContentType.Video && (!String.IsNullOrEmpty(currentVideoURI)))
            {
                //Replay the video
                VideoControl.Source = new Uri(currentVideoURI);
                VideoControl.Play();
            }
        }

        private void FullScreenVideoControl_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (currentScreenContent == ContentType.Video && (!String.IsNullOrEmpty(currentVideoURI)))
            {
                //Replay the video
                FullScreenVideoControl.Source = new Uri(currentVideoURI);
                FullScreenVideoControl.Play();
            }
        }

        #region Header Drag Bar
        private void CloseButtonMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //Stop Logging
            LoggingHelper.StopLogging();
            Close();
        }

        private void MinimizeButtonMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        #endregion

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Globals.CurrentlySelectedBox > 0)
                {
                    //Save tile properties, Update the properties and shows the screen
                    if (ShowPropertiesForGivenTile(prevSelectedTile))
                    {
                        Globals.currentAppState = RippleSystemStates.OptionSelected;
                        if (prevSelectedTile.Action == TileAction.Standard)
                        {
                            //Show floor properties if the above is successful
                            LayoutTiles(Globals.CurrentlySelectedParent);
                        }
                        else if (prevSelectedTile.Action == TileAction.QRCode)
                        {
                            LayoutTiles(Globals.CurrentlySelectedParent);
                            ((TextBlock)FindName("MainOptionGridLabel")).Text = prevSelectedTile.Name;
                            ProcessTileActionForQRCode(prevSelectedTile.ActionURI);
                        }
                        else if (prevSelectedTile.Action == TileAction.Logout)
                        {
                            ProcessTileActionForLogout();
                        }
                        else if (prevSelectedTile.Action == TileAction.HTML)
                        {
                            LayoutTiles(Globals.CurrentlySelectedParent);
                            ProcessTileActionForAnimation(prevSelectedTile.ActionURI);
                        }
                        else
                        {
                            //Do nothing for Action = Action.Nothing and Action.NothingOnFloor
                        }
                    }
                }
                else
                    OnTileSelected("Tile0");
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in Refresh button click {0}", ex.Message);
            }
        }
    }
}
