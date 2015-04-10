using System.Reflection;
using System.Threading;
using System.Windows.Forms.Integration;
using RippleCommonUtilities;
using RippleDictionary;
using RippleScreenApp.Utilities;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HelperMethods = RippleScreenApp.DocumentPresentation.HelperMethods;
using WebBrowser = System.Windows.Forms.WebBrowser;

namespace RippleScreenApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ScreenWindow : Window
    {
        internal static Ripple RippleData;
        internal static String PersonName;

        private static TextBlock tbElement = new TextBlock();
        private static TextBlock fullScreenTbElement = new TextBlock();
        private static Image imgElement = new Image();
        private static Image fullScreenImgElement = new Image();
        private static String _currentVideoUri = String.Empty;
        private static ContentType _currentScreenContent = ContentType.Nothing;
        private static bool _loopVideo;
        
        private BackgroundWorker _myBackgroundWorker;
        private BackgroundWorker _pptWorker;
        private ScriptingHelper _helper;
        public WindowsFormsHost Host;
        public WebBrowser BrowserElement;

        public static String SessionGuid = String.Empty;

        private long _prevRow;
        private Tile _currentTile;
        private bool _startVideoPlayed;

        
        public ScreenWindow()
        {
           
            try
            {
                InitializeComponent();

                LoadData();

                SetObjectProperties();

                //Start receiving messages
                MessageReceiver.StartReceivingMessages(this);

            }
            catch (Exception ex)
            {
                //Exit and do nothing
                LoggingHelper.LogTrace(1, "Went wrong in Screen {0}", ex.Message);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                WindowState = WindowState.Maximized;
                ResetUI();
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in Window Loaded {0}", ex.Message);
            }
        }

        private void SetObjectProperties()
        {
            //Initialize video properties
            IntroVideoControl.Source = new Uri(Helper.GetAssetURI(RippleData.Screen.ScreenContents["IntroVideo"].Content));
            IntroVideoControl.ScrubbingEnabled = true;
            //Set image elements properties
            imgElement.Stretch = Stretch.Fill;
            fullScreenImgElement.Stretch = Stretch.Fill;
            //Set text block properties
            tbElement.FontSize = 50;
            tbElement.Margin = new Thickness(120, 120, 120, 0);
            tbElement.TextWrapping = TextWrapping.Wrap;
            fullScreenTbElement.FontSize = 50;
            fullScreenTbElement.Margin = new Thickness(120, 120, 120, 0);
            fullScreenTbElement.TextWrapping = TextWrapping.Wrap;
        }

        /// <summary>
        /// Method that loads the configured data for the Screen, right now the Source is XML
        /// </summary>
        private void LoadData()
        {
            try
            {
                //Loads the local dictionary data from the configuration XML
                RippleData = Dictionary.GetRipple(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in Load Data for Screen {0}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Resets the UI to System locked mode.
        /// </summary>
        private void ResetUI()
        {
            try
            {
                Globals.ResetGlobals();
                _currentVideoUri = String.Empty;
                _currentScreenContent = ContentType.Nothing;
                _loopVideo = false;
                _startVideoPlayed = false;

                SessionGuid = String.Empty;

                //Pick up content based on the "LockScreen" ID 
                ProjectContent(RippleData.Screen.ScreenContents["LockScreen"]);

                //Commit the telemetry data
                TelemetryWriter.CommitTelemetryAsync();
            }
            catch (Exception ex)
            {
                //Do nothing
                LoggingHelper.LogTrace(1, "Went wrong in Reset UI for Screen {0}", ex.Message);
            }

        }

        /// <summary>
        /// Code will receive messages from the floor
        /// Invoke appropriate content projection based on the tile ID passed
        /// </summary>
        /// <param name="val"></param>
        public void OnMessageReceived(string val)
        {
            try
            {
                //Check for reset
                if (val.Equals("Reset"))
                {
                    //Update the previous entry
                    TelemetryWriter.UpdatePreviousEntry();

                    //Reset the system
                    ResetUI();
                }
                //Check for System start
                //Check for System start
                else if (val.StartsWith("System Start"))
                {
                    //Load the telemetry Data
                    TelemetryWriter.RetrieveTelemetryData();

                    //The floor has asked the screen to start the system
                    //Get the User Name
                    Globals.UserName = val.Split(':')[1];

                    //Get the person identity for the session
                    PersonName = String.IsNullOrEmpty(Globals.UserName) ? Convert.ToString(Guid.NewGuid()) : Globals.UserName;

                    TelemetryWriter.AddTelemetryRow(RippleData.Floor.SetupID, PersonName, "Unlock", val, "Unlock");

                    //Set the system state
                    Globals.currentAppState = RippleSystemStates.UserDetected;

                    //Play the Intro Content
                    ProjectIntroContent(RippleData.Screen.ScreenContents["IntroVideo"]);
                }
                //Check for gestures
                else if (val.StartsWith("Gesture"))
                {
                    OnGestureInput(val.Split(':')[1]);
                }
                //Check for HTML messages
                else if (val.StartsWith("HTML"))
                {
                    OnHTMLMessagesReceived(val.Split(':')[1]);
                }
                //Check for options- TODO need to figure out
                else if (val.StartsWith("Option"))
                {
                    //Do nothing
                }
                //Check if a content - tile mapping or in general content tag exists
                else
                {
                    if (RippleData.Screen.ScreenContents.ContainsKey(val) && RippleData.Screen.ScreenContents[val].Type != ContentType.Nothing)
                    {
                        //Set the system state
                        Globals.currentAppState = RippleSystemStates.OptionSelected;

                        ProjectContent(RippleData.Screen.ScreenContents[val]);

                        LoggingHelper.LogTrace(1, "In Message Received {0} {1}:{2}", TelemetryWriter.telemetryData.Tables[0].Rows.Count, TelemetryWriter.telemetryData.Tables[0].Rows[TelemetryWriter.telemetryData.Tables[0].Rows.Count - 1].ItemArray[6], DateTime.Now);

                        //Update the end time for the previous
                        TelemetryWriter.UpdatePreviousEntry();

                        //Insert the new entry
                        TelemetryWriter.AddTelemetryRow(RippleData.Floor.SetupID, PersonName, ((_currentTile = GetFloorTileForID(val))==null)?"Unknown":_currentTile.Name, val, (val == "Tile0") ? "Start" : "Option");
                    }
                    else
                    {
                        //Stop any existing projections
                        HelperMethods.StopPresentation();
                        FullScreenContentGrid.Children.Clear();
                        ContentGrid.Children.Clear();

                        //Set focus for screen window also
                        Helper.ClickOnScreenToGetFocus();

                        //Stop any existing videos
                        _loopVideo = false;
                        VideoControl.Source = null;
                        FullScreenVideoControl.Source = null;

                        //Clean the images
                        fullScreenImgElement.Source = null;
                        imgElement.Source = null;

                        //Clear the header text
                        TitleLabel.Text = "";

                        //Dispose the objects
                        if(BrowserElement != null)
                            BrowserElement.Dispose();
                        BrowserElement = null;

                        if (Host != null)
                            Host.Dispose();
                        Host = null;

                        if (_helper != null)
                            _helper.PropertyChanged -= helper_PropertyChanged;
                        _helper = null;

                        _currentScreenContent = ContentType.Nothing;

                        ShowText("No content available for this option, Please try some other tile option", "No Content");
                    }
                }
            }
            catch (Exception ex)
            {
                //Do nothing
                LoggingHelper.LogTrace(1, "Went wrong in On message received for Screen {0}", ex.Message);
            }


        }

        private void OnHTMLMessagesReceived(string p)
        {
            try
            {
                if(p.StartsWith("SessionID,"))
                {
                    SessionGuid = p;
                    return;
                }

                if (_helper != null && _currentScreenContent == ContentType.HTML)
                {
                    _helper.MessageReceived(p);
                }

            }
            catch (Exception ex)
            {
                //Do nothing
                LoggingHelper.LogTrace(1, "Went wrong in OnHTMLMessagesReceived received for Screen {0}", ex.Message);
            }
        }

        private void OnGestureInput(string inputGesture)
        {
            try
            {
                //PPT Mode - left and right swipe
                if (inputGesture == GestureTypes.LeftSwipe.ToString() && _currentScreenContent == ContentType.PPT)
                {
                    //Acts as previous
                    HelperMethods.GotoPrevious();
                }
                else if (inputGesture == GestureTypes.RightSwipe.ToString() && _currentScreenContent == ContentType.PPT)
                {
                    //Check again, Means the presentation ended on clicking next
                    if (!HelperMethods.HasPresentationStarted())
                    {
                        //Change the screen
                        //ShowText("Your presentation has ended, Select some other option", "Select some other option");
                        ShowImage(@"\Assets\Images\pptend.png", "Presentation Ended");

                        //Set focus for screen window also
                        Helper.ClickOnScreenToGetFocus();
                    }

                    //Acts as next
                    HelperMethods.GotoNext();

                    //Check again, Means the presentation ended on clicking next
                    if (!HelperMethods.HasPresentationStarted())
                    {
                        //Change the screen text
                        //ShowText("Your presentation has ended, Select some other option", "Select some other option");
                        ShowImage(@"\Assets\Images\pptend.png", "Presentation Ended");

                        //Set focus for screen window also
                        Helper.ClickOnScreenToGetFocus();
                    }
                }
                //Browser mode
                else if (_currentScreenContent == ContentType.HTML)
                {
                    OnHTMLMessagesReceived(inputGesture.ToString());
                }

                //Set focus for screen window also
                //Utilities.Helper.ClickOnScreenToGetFocus();
            }
            catch (Exception ex)
            {
                //Do nothing
                LoggingHelper.LogTrace(1, "Went wrong in OnGestureInput received for Screen {0}", ex.Message);
            }
        }

        #region Content Projection methods
        /// <summary>
        /// Identifies the content type and project accordingly
        /// </summary>
        /// <param name="screenContent"></param>
        private void ProjectContent(ScreenContent screenContent)
        {
            try
            {
                if (screenContent.Type == ContentType.HTMLMessage)
                {
                    if (_helper != null && _currentScreenContent == ContentType.HTML)
                    {
                        _helper.MessageReceived(screenContent.Content);
                        return;
                    }
                }

                //Stop any existing projections
                HelperMethods.StopPresentation();
                FullScreenContentGrid.Children.Clear();
                ContentGrid.Children.Clear();

                //Set focus for screen window also
                Helper.ClickOnScreenToGetFocus();

                //Stop any existing videos
                _loopVideo = false;
                VideoControl.Source = null;
                FullScreenVideoControl.Source = null;

                //Clean the images
                fullScreenImgElement.Source = null;
                imgElement.Source = null;

                //Clear the header text
                TitleLabel.Text = "";

                //Dispose the objects
                if (BrowserElement != null)
                    BrowserElement.Dispose();
                BrowserElement = null;

                if (Host != null)
                    Host.Dispose();
                Host = null;

                if (_helper != null)
                    _helper.PropertyChanged -= helper_PropertyChanged;
                _helper = null;

                _currentScreenContent = screenContent.Type;

                if (screenContent.Id == "Tile0" && _startVideoPlayed)
                {
                    _currentScreenContent = ContentType.Image;
                    ShowImage("\\Assets\\Images\\default_start.png", screenContent.Header);
                    return;
                }

                switch (screenContent.Type)
                {
                    case ContentType.HTML:
                        ShowBrowser(screenContent.Content, screenContent.Header);
                        break;
                    case ContentType.Image:
                        ShowImage(screenContent.Content, screenContent.Header);
                        break;
                    case ContentType.PPT:
                        ShowPPT(screenContent.Content, screenContent.Header);
                        break;
                    case ContentType.Text:
                        ShowText(screenContent.Content, screenContent.Header);
                        break;
                    case ContentType.Video:
                        _loopVideo = (screenContent.LoopVideo == null) ? false : Convert.ToBoolean(screenContent.LoopVideo);
                        if (screenContent.Id == "Tile0")
                            _startVideoPlayed = true;
                        ShowVideo(screenContent.Content, screenContent.Header);
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in ProjectContent Method for screen {0}", ex.Message);
            }
        }

        private void ProjectIntroContent(ScreenContent screenContent)
        {
            try
            {
                //Dispose the previous content
                //Stop any existing projections
                HelperMethods.StopPresentation();
                FullScreenContentGrid.Children.Clear();
                ContentGrid.Children.Clear();

                //Set focus for screen window also
                Helper.ClickOnScreenToGetFocus();

                //Stop any existing videos
                _loopVideo = false;
                VideoControl.Source = null;
                FullScreenVideoControl.Source = null;

                //Clean the images
                fullScreenImgElement.Source = null;
                imgElement.Source = null;

                //Clear the header text
                TitleLabel.Text = "";

                if (BrowserElement != null)
                    BrowserElement.Dispose();
                BrowserElement = null;
                if (Host != null)
                    Host.Dispose();
                Host = null;
                if (_helper != null)
                    _helper.PropertyChanged -= helper_PropertyChanged;
                _helper = null;

                //Play the Intro video 
                TitleLabel.Text = "";
                ContentGrid.Visibility = Visibility.Collapsed;
                FullScreenContentGrid.Visibility = Visibility.Collapsed;
                FullScreenVideoGrid.Visibility = Visibility.Collapsed;
                IntroVideoControl.Visibility = Visibility.Visible;
                VideoControl.Visibility = Visibility.Collapsed;
                VideoGrid.Visibility = Visibility.Visible;
                IntroVideoControl.Play();
                UpdateLayout();

                _myBackgroundWorker = new BackgroundWorker();
                _myBackgroundWorker.DoWork += myBackgroundWorker_DoWork;
                _myBackgroundWorker.RunWorkerCompleted += myBackgroundWorker_RunWorkerCompleted;
                _myBackgroundWorker.RunWorkerAsync(RippleData.Floor.Start.IntroVideoWaitPeriod);
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in ProjectIntroContent Method for screen {0}", ex.Message);
            }
        }

        private void myBackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //System has been started, it just finished playing the intro video
            if (Globals.currentAppState == RippleSystemStates.UserDetected)
            {
                IntroVideoControl.Stop();
                //this.IntroVideoControl.Source = null;
                IntroVideoControl.Visibility = Visibility.Collapsed;
                ShowGotoStartContent();
            }
        }

        private void myBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(Convert.ToInt16(e.Argument) * 1000);
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
                    _currentVideoUri = Helper.GetAssetURI(Content);
                    FullScreenVideoControl.Source = new Uri(_currentVideoUri);
                    FullScreenContentGrid.Visibility = Visibility.Collapsed;
                    FullScreenVideoGrid.Visibility = Visibility.Visible;
                    FullScreenVideoControl.Play();
                }
                else
                {
                    TitleLabel.Text = header;
                    _currentVideoUri = Helper.GetAssetURI(Content);
                    VideoControl.Source = new Uri(_currentVideoUri);
                    ContentGrid.Visibility = Visibility.Collapsed;
                    FullScreenContentGrid.Visibility = Visibility.Collapsed;
                    FullScreenVideoGrid.Visibility = Visibility.Collapsed;
                    VideoControl.Visibility = Visibility.Visible;
                    VideoGrid.Visibility = Visibility.Visible;
                    VideoControl.Play();
                }
                UpdateLayout();
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in Show Video method {0}", ex.Message);
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
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in Show Text method {0}", ex.Message);
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
                //Check if header is null
                //Null - Show full screen content
                if (String.IsNullOrEmpty(header))
                {
                    //Show the full screen video control 
                    FullScreenContentGrid.Visibility = Visibility.Visible;
                    FullScreenVideoGrid.Visibility = Visibility.Collapsed;
                }
                else
                {
                    TitleLabel.Text = header;
                    ContentGrid.Visibility = Visibility.Visible;
                    FullScreenContentGrid.Visibility = Visibility.Collapsed;
                    FullScreenVideoGrid.Visibility = Visibility.Collapsed;
                    VideoGrid.Visibility = Visibility.Collapsed;
                }
                UpdateLayout();
                HelperMethods.StartPresentation(Helper.GetAssetURI(Content));

                //ShowText("Please wait while we load your presentation", header);
                //ShowImage(@"\Assets\Images\loading.png", header);
                //this.UpdateLayout();
                //pptWorker = new BackgroundWorker();
                //pptWorker.DoWork += pptWorker_DoWork;
                //pptWorker.RunWorkerAsync(Content);
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in Show PPT method {0}", ex.Message);
            }
        }

        void pptWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            HelperMethods.StartPresentation(Helper.GetAssetURI(e.Argument.ToString()));
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
                    fullScreenImgElement.Source = new BitmapImage(new Uri(Helper.GetAssetURI(Content)));
                    FullScreenContentGrid.Children.Clear();
                    FullScreenContentGrid.Children.Add(fullScreenImgElement);
                    FullScreenContentGrid.Visibility = Visibility.Visible;
                    FullScreenVideoGrid.Visibility = Visibility.Collapsed;
                }
                else
                {
                    TitleLabel.Text = header;
                    imgElement.Source = new BitmapImage(new Uri(Helper.GetAssetURI(Content)));
                    ContentGrid.Children.Clear();
                    ContentGrid.Children.Add(imgElement);
                    ContentGrid.Visibility = Visibility.Visible;
                    FullScreenContentGrid.Visibility = Visibility.Collapsed;
                    FullScreenVideoGrid.Visibility = Visibility.Collapsed;
                    VideoGrid.Visibility = Visibility.Collapsed;
                }
                UpdateLayout();
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in Show Image {0}", ex.Message);
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
                    Host = new WindowsFormsHost();
                    BrowserElement = new WebBrowser();
                    BrowserElement.ScriptErrorsSuppressed = true;
                    _helper = new ScriptingHelper(this);
                    BrowserElement.ObjectForScripting = _helper;
                    Host.Child = BrowserElement;
                    _helper.PropertyChanged += helper_PropertyChanged;
                    FullScreenContentGrid.Children.Clear();
                    FullScreenContentGrid.Children.Add(Host);
                    FullScreenContentGrid.Visibility = Visibility.Visible;
                    FullScreenVideoGrid.Visibility = Visibility.Collapsed;
                }
                else
                {
                    TitleLabel.Text = header;
                    Host = new WindowsFormsHost();
                    BrowserElement = new WebBrowser();
                    BrowserElement.ScriptErrorsSuppressed = true;
                    _helper = new ScriptingHelper(this);
                    Host.Child = BrowserElement;
                    BrowserElement.ObjectForScripting = _helper;
                    _helper.PropertyChanged += helper_PropertyChanged;
                    ContentGrid.Children.Clear();
                    ContentGrid.Children.Add(Host);
                    ContentGrid.Visibility = Visibility.Visible;
                    FullScreenContentGrid.Visibility = Visibility.Collapsed;
                    FullScreenVideoGrid.Visibility = Visibility.Collapsed;
                    VideoGrid.Visibility = Visibility.Collapsed;
                }
                var fileLocation = Helper.GetAssetURI(Content);
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
                BrowserElement.Navigate(pageUri);
                UpdateLayout();
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in Show Browser {0}", ex.Message);
            }
        }
        #endregion

        #region Helpers
        private Tile GetFloorTileForID(string TileID)
        {
            Tile reqdTile = null;
            try
            {
                reqdTile = RippleData.Floor.Tiles[TileID];
            }
            catch (Exception)
            {
                try
                {
                    reqdTile = RippleData.Floor.Tiles[TileID.Substring(0, TileID.LastIndexOf("SubTile"))].SubTiles[TileID];
                }
                catch (Exception)
                {
                    return null;
                }
            }
            return reqdTile;
        }
        #endregion

        private void VideoControl_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (_currentScreenContent == ContentType.Video && _loopVideo && (!String.IsNullOrEmpty(_currentVideoUri)))
            {
                //Replay the video
                VideoControl.Source = new Uri(_currentVideoUri);
                VideoControl.Play();
            }
        }

        private void FullScreenVideoControl_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (_currentScreenContent == ContentType.Video && _loopVideo && (!String.IsNullOrEmpty(_currentVideoUri)))
            {
                //Replay the video
                FullScreenVideoControl.Source = new Uri(_currentVideoUri);
                FullScreenVideoControl.Play();
            }
        }

        private void ShowGotoStartContent()
        {
            //Set the system state
            Globals.currentAppState = RippleSystemStates.UserWaitToGoOnStart;
            ProjectContent(RippleData.Screen.ScreenContents["GotoStart"]);
        }

        private void IntroVideoControl_MediaEnded(object sender, RoutedEventArgs e)
        {
            //System has been started, it just finished playing the intro video
            //if (Globals.currentAppState == RippleSystemStates.UserDetected)
            //{
            //    this.IntroVideoControl.Stop();
            //    ShowGotoStartContent();
            //}
        }

        private void Window_Closing_1(object sender, CancelEventArgs e)
        {
            LoggingHelper.StopLogging();
        }

        //Handles messages sent by HTML animations
        void helper_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                var scriptingHelper = sender as ScriptingHelper;
                if (scriptingHelper != null)
                {
                    if (e.PropertyName == "SendMessage")
                    {
                        if ((!String.IsNullOrEmpty(scriptingHelper.SendMessage)) && _currentScreenContent == ContentType.HTML)
                        {
                            //Send the screen a message for HTML parameter passing
                            MessageReceiver.SendMessage("HTML:" + scriptingHelper.SendMessage);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in helper property changed event {0}", ex.Message);
            }

        }

    }
}
