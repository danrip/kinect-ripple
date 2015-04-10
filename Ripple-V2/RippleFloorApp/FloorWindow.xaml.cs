using System.Threading;
using RippleCommonUtilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RippleDictionary;
using System.Media;
using System.Windows.Media.Animation;
using System.IO;
using System.Drawing.Imaging;
using RippleFloorApp.Controls;
using RippleFloorApp.Utilities;
using HelperMethods = RippleFloorApp.Utilities.HelperMethods;
using TUC = RippleFloorApp.Controls.TileControl;
using SM = System.Windows.Media;
using Style = System.Windows.Style;
using WebBrowser = System.Windows.Forms.WebBrowser;

namespace RippleFloorApp
{
    public partial class FloorWindow
    {
        private List<TUC> _tileList;
        
        private static BackgroundWorker autoLogoutWorker = new BackgroundWorker();
        private static int _autoLockPeriodInSeconds;
        private static int _checkPeriodInSeconds;

        static String _lastSelectedOptionName;
        static int _waitDuration;
        static bool _qrMode;

        BackgroundWorker _waitThread;
        DateTime _now;
        ScriptingHelper _helper;
        
        //animation properties
        Storyboard _tileTransitionSb;
        Storyboard _liveTile;

        public static Floor FloorData;
        public WindowsFormsHost BrowserHost;
        public WebBrowser BrowserElement;

        public const string InnerContent = "InnerContent";
        public const string InnerTile = "InnerTile";
        public const string Label = "Label";


        /// <summary>
        /// Constructor for the class
        /// Used to accomplish the one time activities
        /// </summary>
        public FloorWindow(WindowsFormsHost host)
        {
            try
            {
                InitializeComponent();

                LoadData();
                InitializeTiles();
                InitializeAnimationStoryboards();

                //Start receiving messages
                MessageSender.StartReceivingMessages(this);

                var kinectHelper = new KinectHelper();
                kinectHelper.PropertyChanged += kinectHelper_PropertyChanged;

                //Initialize background wait thread to auto lock the system
                _autoLockPeriodInSeconds = FloorData.SystemAutoLockPeriod;
                _checkPeriodInSeconds = 60;

                autoLogoutWorker.DoWork += autoLogoutWorker_DoWork;
                autoLogoutWorker.RunWorkerCompleted += autoLogoutWorker_RunWorkerCompleted;

                //Start the auto - lock thread
                autoLogoutWorker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                //Exit and do nothing
                LoggingHelper.LogTrace(1, "Went wrong in Floor constructor {0}", ex.Message);
            }
        }

        /// <summary>
        /// Code will receive messages from the screen
        /// </summary>
        /// <param name="val"></param>
        public void OnMessageReceived(string val)
        {
            try
            {
                //Check for HTMl messages
                if (val.StartsWith("HTML"))
                {
                    OnHtmlMessagesReceived(val.Split(':')[1]);
                }
            }
            catch (Exception ex)
            {
                //Do nothing
                LoggingHelper.LogTrace(1, "Went wrong in On message received for Floor {0}", ex.Message);
            }


        }

        private void OnHtmlMessagesReceived(string p)
        {
            try
            {
                if (_helper != null && GetFloorTileForID(Globals.SelectedOptionFullName).Action == TileAction.HTML)
                {
                    _helper.MessageReceived(p);
                }

            }
            catch (Exception ex)
            {
                //Do nothing
                LoggingHelper.LogTrace(1, "Went wrong in OnHTMLMessagesReceived received for floor {0}", ex.Message);
            }
        }

        #region System Initialize

        private void LoadData()
        {
            try
            {
                var floorDataFolder = Path.Combine(Directory.GetCurrentDirectory(), "FloorData");
                FloorData = Dictionary.GetFloor(floorDataFolder);                    
            }
            catch(FileNotFoundException fEx)
            {
                LoggingHelper.LogTrace(1, "Unable to find floor data file", fEx.Message);
                throw;
            }
        }

        /// <summary>
        /// Method to initialize the tile layout dynamically
        /// </summary>
        private void InitializeTiles()
        {
            MainContainer.Children.Clear();

            //Populate the list of tiles
            if (_tileList != null) return;
            
            //Upper tile properties
            var upperVideoContentGrid = new UpperVideoControl(this)
            {
                ControlWidth = FloorData.UpperTile.Style.Width,
                ControlHeight = FloorData.UpperTile.Style.Height
            };

            upperVideoContentGrid.SetMargin(FloorData.UpperTile.Coordinate.X, FloorData.UpperTile.Coordinate.Y);

            _tileList = new List<TUC>();
            //Rest of the tiles including start
            foreach (var tile in FloorData.Tiles.Values)
            {
                var nTile = new TUC { TileBackground = tile.Color };
                nTile.TileIDName = nTile.TileIDName.Replace("TileID", tile.Id);
                nTile.InnerContentTileIDName = nTile.InnerContentTileIDName.Replace("TileID", tile.Id);
                nTile.TileIDLabelName = nTile.TileIDLabelName.Replace("TileID", tile.Id);
                nTile.TileWidth = tile.Style.Width;
                nTile.TileHeight = tile.Style.Height;

                if (tile.Name == "Start")
                    nTile.TileIDLabel.FontSize = 40;

                nTile.SetMargin(tile.Coordinate.X, tile.Coordinate.Y);
                nTile.TileIDLabelText = tile.Name;

                RegisterName(nTile.TileIDName, nTile.TileID);
                RegisterName(nTile.InnerContentTileIDName, nTile.InnerContentTileID);
                RegisterName(nTile.TileIDLabelName, nTile.TileIDLabel);

                _tileList.Add(nTile);
            }

            //Main Option grid properties
            var mainOptionGrid = new MainOptionTile(this);
                
            mainOptionGrid.SetMargin(FloorData.UpperTile.Style.Height, 0);
            mainOptionGrid.ControlWidth = FloorData.UpperTile.Style.Width;
            mainOptionGrid.ControlHeight = FloorData.UpperTile.Style.Height;

            //Overlay image properties
            var overlayImage = new OverlayImageControl(this);
            overlayImage.SetMargin(FloorData.UpperTile.Style.Height);

                
            MainContainer.Children.Add(upperVideoContentGrid);

            //Add the tile list to the main UI
            for (var i = _tileList.Count - 1; i >= 0; i--)
            {
                MainContainer.Children.Add(_tileList[i]);
            }

            //Add the overlay image and Main Option grid to the Main UI
            MainContainer.Children.Add(mainOptionGrid);
            MainContainer.Children.Add(overlayImage);

            UpdateLayout();
        }

        private void InitializeAnimationStoryboards()
        {
            //create the Animation for tile transition
            CreateTileTransitionStoryboard(FloorData.Transition.Animation);

            //Set the keyframes based on the resolution for live tile
            _liveTile = (Storyboard)Main.FindResource("liveTile");
            
            const double originalHorizontalResolutionUsed = 1150;
            var ratio = Globals.CurrentResolution.HorizontalResolution / originalHorizontalResolutionUsed;
            double val;
            
            foreach (EasingDoubleKeyFrame sbItem in ((DoubleAnimationUsingKeyFrames)_liveTile.Children[0]).KeyFrames)
            {
                val = sbItem.Value * ratio;
                sbItem.SetValue(EasingDoubleKeyFrame.ValueProperty, val);
                //((EasingDoubleKeyFrame)((DoubleAnimationUsingKeyFrames)liveTile.Children[0]).KeyFrames[index]).SetValue(EasingDoubleKeyFrame.ValueProperty, val);
            }
            foreach (EasingDoubleKeyFrame sbItem in ((DoubleAnimationUsingKeyFrames)_liveTile.Children[1]).KeyFrames)
            {
                val = sbItem.Value * ratio;
                sbItem.SetValue(EasingDoubleKeyFrame.ValueProperty, val);
                //((EasingDoubleKeyFrame)((DoubleAnimationUsingKeyFrames)liveTile.Children[0]).KeyFrames[index++]).SetValue(EasingDoubleKeyFrame.ValueProperty, val);
            }
        }

        private void CreateTileTransitionStoryboard(string tileTransitionName)
        {
            var existingTileTransition = (Storyboard)Main.FindResource(tileTransitionName);

            _tileTransitionSb = new Storyboard();

            var doubleAnim = (DoubleAnimationUsingKeyFrames)existingTileTransition.Children[0];

            var colorAnim = (ColorAnimationUsingKeyFrames)existingTileTransition.Children[1];

            DoubleAnimationUsingKeyFrames dAnim = null;
            ColorAnimationUsingKeyFrames cAnim = null;
            _tileTransitionSb.Children.Clear();
            //Add the above two animations for every tile except start
            foreach (var tile in FloorData.Tiles.Keys)
            {
                if (!tile.Equals("Tile0"))
                {
                    dAnim = doubleAnim.Clone();
                    cAnim = colorAnim.Clone();
                    Storyboard.SetTarget(dAnim, (Grid)FindName(tile));
                    Storyboard.SetTarget(cAnim, (Grid)FindName(tile));
                    _tileTransitionSb.Children.Add(dAnim);
                    _tileTransitionSb.Children.Add(cAnim);
                }
            }
        }

        #region Unlock code

        //Handles messages sent by HTML animations
        void helper_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                var scriptingHelper = sender as ScriptingHelper;
                if (scriptingHelper != null)
                {
                    if (e.PropertyName == "SystemUnlocked")
                    {
                        //Check if the system is configured to be unlocked through WPF animations, and its been unlocked
                        if (scriptingHelper.SystemUnlocked && FloorData.Start.Unlock.Mode == Mode.HTML)
                        {
                            if (_helper != null)
                            {
                                _helper.PropertyChanged -= helper_PropertyChanged;
                                _helper = null;
                            }

                            UnlockRippleSystem();
                        }
                    }

                    else if (e.PropertyName == "ExitOnStart")
                    {
                        if (scriptingHelper.ExitOnStart && Globals.currentAppState == RippleSystemStates.UserPlayingAnimations)
                        {
                            //End the game and go to start
                            if (_helper != null)
                            {
                                _helper.PropertyChanged -= helper_PropertyChanged;

                                if (BrowserElement != null)
                                    BrowserElement.Dispose();
                                BrowserElement = null;

                                if (BrowserHost != null)
                                    BrowserHost.Dispose();
                                BrowserHost = null;

                                _helper = null;
                            }
                            //Start the video
                            ((MediaElement)FindName("FloorVideoControl")).Play();

                            _lastSelectedOptionName = "Tile0";

                            //Show the main options
                            OnStartSelected();
                        }
                    }

                    else if (e.PropertyName == "ExitGame")
                    {
                        if (scriptingHelper.ExitGame && Globals.currentAppState == RippleSystemStates.UserPlayingAnimations)
                        {
                            //End the game and go to start with the main options laid out
                            if (_helper != null)
                            {
                                _helper.PropertyChanged -= helper_PropertyChanged;

                                if (BrowserElement != null)
                                    BrowserElement.Dispose();
                                BrowserElement = null;

                                if (BrowserHost != null)
                                    BrowserHost.Dispose();
                                BrowserHost = null;

                                _helper = null;
                            }
                            //Start the video
                            ((MediaElement)FindName("FloorVideoControl")).Play();

                            //Send the screen a message
                            MessageSender.SendMessage("GotoStart");

                            //Set the system state
                            Globals.currentAppState = RippleSystemStates.UserWaitToGoOnStart;

                            Globals.CurrentlySelectedParent = 0;

                            //Show the start options
                            ArrangeFloor();
                        }
                    }

                    else if (e.PropertyName == "SendMessage")
                    {
                        if ((!String.IsNullOrEmpty(scriptingHelper.SendMessage)))
                        {
                            //Send the screen a message for HTML parameter passing
                            MessageSender.SendMessage(scriptingHelper.SendMessage);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in helper property changed event {0}", ex.Message);
            }

        }

        private void UnlockRippleSystem()
        {
            //Set the system state - Updated the State
            Globals.currentAppState = RippleSystemStates.UserDetected;
            //Globals.currentAppState = RippleSystemStates.UserWaitToGoOnStart;

            //Send a message to the screen to start the system along with the user name if present
            MessageSender.SendMessage("System Start:" + (String.IsNullOrEmpty(Globals.UserName) ? Globals.EmpAlias : Globals.UserName));

            //ArrangeFloor();
            //Wait for the screen to finish the start process for the defined duration
            _waitThread = new BackgroundWorker();
            //Get the wait value
            _waitDuration = FloorData.Start.IntroVideoWaitPeriod;
            _waitThread.DoWork += waitThread_DoWork;
            _waitThread.RunWorkerCompleted += waitThread_RunWorkerCompleted;
            _waitThread.RunWorkerAsync();
        }
        #endregion

        /// <summary>
        /// Function to reset the UI and take it to No User mode
        /// </summary>
        public void ResetUi()
        {
            try
            {
                //Reset the globals
                Globals.ResetGlobals();
                _lastSelectedOptionName = "";
                _qrMode = false;

                //Dispose the objects
                //if (player != null)
                //{
                //    //Stop the flash.
                //    player.Stop();
                //    player.Width = 0;
                //    player.Height = 0;
                //    player.Dispose();
                //    host.Dispose();
                //    player = null;
                //    host = null;
                //}

                //else 
                if (BrowserElement != null)
                {
                    BrowserElement.Dispose();
                    BrowserElement = null;

                    if (BrowserHost != null)
                        BrowserHost.Dispose();
                    BrowserHost = null;

                    _helper = null;
                }

                FlashContainer.Children.Clear();

                ProjectAnimationContentOnFloor(FloorData.Start.Animation.Content);

                UpdateLayout();

            }
            catch (Exception ex)
            {
                //Do nothing
                LoggingHelper.LogTrace(1, "Went wrong in Reset UI {0} where floor content is {1}", ex.Message, FloorData.Start.Animation.Content);
            }

        }

        void waitThread_DoWork(object sender, DoWorkEventArgs e)
        {
            //Wait in backgroud for video duration period
            Thread.Sleep(_waitDuration * 1000);
        }

        void waitThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Set the system state
            Globals.currentAppState = RippleSystemStates.UserWaitToGoOnStart;

            //Show the floor
            ArrangeFloor();
        }

        void autoLogoutWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            //Wait for Auto Lock period
            Thread.Sleep(_checkPeriodInSeconds * 1000);
        }

        void autoLogoutWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //if the system is in options mode
            if (Globals.currentAppState == RippleSystemStates.Start || Globals.currentAppState == RippleSystemStates.UserWaitToGoOnStart || Globals.currentAppState == RippleSystemStates.UserPlayingAnimations || Globals.currentAppState == RippleSystemStates.OptionSelected || Globals.currentAppState == RippleSystemStates.UserDetected)
            {
                //Check when was the last user reported
                var currentTime = DateTime.Now;
                if ((currentTime - KinectHelper.LastUserVisibleTime).TotalSeconds > Convert.ToDouble(_autoLockPeriodInSeconds))
                {
                    //The last time user was visible has elapsed the auto-lock period
                    //Lock the system
                    ProcessTileActionForLogout();
                }
            }

            //Keep looping either way
            autoLogoutWorker.RunWorkerAsync();
        }

        private void FloorVideoControl_MediaEnded(object sender, RoutedEventArgs e)
        {
            //Play the Ripple video
            ((MediaElement)FindName("FloorVideoControl")).Source = new Uri(HelperMethods.GetAssetUri(FloorData.UpperTile.Content));
            ((MediaElement)FindName("FloorVideoControl")).Play();
        }

        #endregion

        #region Kinect Handlers
        /// <summary>
        /// Whenever Kinect detects some location value for the skeleton or some gesture
        /// This function gets triggered
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void kinectHelper_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var kinectInfo = sender as KinectHelper;
            if (kinectInfo != null)
            {
                if (e.PropertyName == "CurrentLocation")
                {
                    OnSelectedBox(kinectInfo.CurrentLocation);
                }
                else if (e.PropertyName == "KinectSwipeDetected")
                {
                    OnGestureDetected(kinectInfo.KinectGestureDetected);
                }
            }
        }

        /// <summary>
        /// On detection of hand swipe gestures
        /// </summary>
        /// <param name="type"></param>
        private void OnGestureDetected(GestureTypes type)
        {
            try
            {
                LoggingHelper.LogTrace(1, "GestureDetected {0}", type.ToString());

                //Pass the message to the HTML content opened on the floor
                if (_helper != null)
                {
                    _helper.GestureReceived(type);
                }

                //To handle unlock if defined in the XML
                //Check if unlock mode is Gesture and is the current gesture defined there to unlock the system
                if (Globals.currentAppState == RippleSystemStates.NoUser && FloorData.Start.Unlock.Mode == Mode.Gesture && FloorData.Start.Unlock.UnlockType == type.ToString())
                {
                    UnlockRippleSystem();
                }
                #region Not needed anymore
                //To exit user animations using right swipe
                //else if (Globals.currentAppState == RippleSystemStates.UserPlayingAnimations && type == GestureTypes.RightSwipe)
                //{
                //    //Dispose the objects
                //    if (player != null)
                //    {
                //        //Stop the flash.
                //        player.Stop();
                //        player.Width = 0;
                //        player.Height = 0;
                //        player.Dispose();
                //        host.Dispose();
                //        player = null;
                //        host = null;
                //    }
                //    else if(helper != null)
                //    {
                //        //Exit the game
                //        if (helper != null)
                //        {
                //            helper.PropertyChanged -= helper_PropertyChanged;
                //            if(browserElement != null)
                //                browserElement.Dispose();
                //            browserElement = null;
                //            helper = null;
                //        }
                //    }

                //    //Start the video
                //    ((MediaElement)this.FindName("FloorVideoControl")).Play();

                //    //Send the screen a message
                //    Utilities.MessageSender.SendMessage("GotoStart");

                //    //Set the system state
                //    Globals.currentAppState = RippleSystemStates.UserWaitToGoOnStart;

                //    Globals.CurrentlySelectedParent = 0;

                //    //Show the start options
                //    ArrangeFloor();
                //} 
                #endregion
                //Check if these are gestures which need to be sent to the screen
                else if (Globals.currentAppState == RippleSystemStates.OptionSelected || Globals.currentAppState == RippleSystemStates.Start)
                {
                    var t = GetFloorTileForID(Globals.SelectedOptionFullName);
                    //PPT accepts left and right gestures
                    if (t.CorrespondingScreenContentType == ContentType.PPT)
                    {
                        if (type == GestureTypes.LeftSwipe || type == GestureTypes.RightSwipe)
                            MessageSender.SendMessage("Gesture:" + type.ToString());
                    }
                    //Browser accepts zoom in, zoom out and scrolling
                    else if (t.CorrespondingScreenContentType == ContentType.HTML && t.Action != TileAction.HTML)
                    {
                        //if (type == GestureTypes.SwipeDown || type == GestureTypes.SwipeUp || type == GestureTypes.ZoomIn || type == GestureTypes.ZoomOut)
                        MessageSender.SendMessage("Gesture:" + type.ToString());
                    }

                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in OnGestureDetected {0}", ex.Message);
            }
        }

        #endregion

        #region Options Code
        private void OnSelectedBox(int value)
        {
            try
            {
                //Check if the value is not relevant
                if (value < 0)
                {
                    //Reset the time stamp and do nothing
                    Globals.currentBoxTimeStamp = DateTime.Now;
                    StopAllAnimations();
                    
                    return;
                }
                Globals.PreviouslySelectedBox = Globals.CurrentlySelectedBox;
                Globals.CurrentlySelectedBox = value;
                Globals.currentUserTimestamp = DateTime.Now;
                //Check if the same box has been selected
                if (Globals.PreviouslySelectedBox == value)
                {
                    //Check if the lockin period has elapsed
                    _now = DateTime.Now;
                    if ((_now - Globals.currentBoxTimeStamp).TotalSeconds >= Convert.ToDouble(FloorData.LockingPeriod))
                    {
                        #region Box selected

                        //Get the tile action type
                        var tileAction = TileAction.Standard;
                        //Main Option
                        if (Globals.currentAppState == RippleSystemStates.Start)
                            tileAction = FloorData.Tiles["Tile" + value].Action;
                        else if (Globals.currentAppState == RippleSystemStates.OptionSelected && value != 0)
                            tileAction = FloorData.Tiles["Tile" + Globals.CurrentlySelectedParent].SubTiles["Tile" + Globals.CurrentlySelectedParent + "SubTile" + value].Action;

                        //Return for static tiles
                        //Return in case the Floor is in QR Mode or if tile does nothing
                        if (tileAction == TileAction.Nothing)
                            return;

                        if (tileAction != TileAction.NothingOnFloor)
                            StartAnimationInBox(value, false);

                        //Check if same as the last selected option
                        if (_lastSelectedOptionName == "Tile" + value && Globals.currentAppState != RippleSystemStates.UserWaitToGoOnStart)
                            return;

                        _lastSelectedOptionName = "Tile" + value;

                        //Start selected
                        if (value == 0)
                        {
                            OnStartSelected();
                        }

                        //Boxes selected
                        //Stop tracking of other boxes during QR code mode
                        else if (!_qrMode)
                        {
                            OnOptionSelected(value);
                        }
                        #endregion
                    }
                    else
                        return;
                }
                else
                {
                    //Reset the time stamp and start the animation on the floor
                    Globals.currentBoxTimeStamp = DateTime.Now;
                    StartAnimationInBox(value, true);
                }


            }
            catch (Exception ex)
            {
                //Do nothing
                LoggingHelper.LogTrace(1, "Went wrong in on selected box {0}", ex.Message);
            }
        }

        public void OnStartSelected()
        {
            if (Globals.currentAppState == RippleSystemStates.UserWaitToGoOnStart || Globals.currentAppState == RippleSystemStates.OptionSelected || Globals.currentAppState == RippleSystemStates.UserPlayingAnimations)
            {
                _qrMode = false;
                Globals.CurrentlySelectedParent = 0;
                Globals.SelectedOptionFullName = _lastSelectedOptionName;
                ShowStartOptions();
                Globals.currentAppState = RippleSystemStates.Start;
                MessageSender.SendMessage("Tile0");
            }
        }

        public void OnOptionSelected(int BoxNumber)
        {
            try
            {
                String tileID = null;
                TileAction action;
                var g = String.Empty;

                //User selected Main Option
                if (Globals.currentAppState == RippleSystemStates.Start)
                {
                    tileID = "Tile" + BoxNumber;
                    Globals.CurrentlySelectedParent = BoxNumber;
                    Globals.currentAppState = RippleSystemStates.OptionSelected;
                    Globals.SelectedOptionFullName = tileID;

                    action = FloorData.Tiles[tileID].Action;

                    //Call the tile transition for Standard Action tiles
                    if (action == TileAction.Standard)
                    {
                        //Show the main grid
                        ((Grid)FindName("MainOptionGrid")).Visibility = Visibility.Visible;
                        ((TextBlock)FindName("MainOptionGridLabel")).Text = FloorData.Tiles[tileID].Name;
                        //Layout the options
                        LayoutTiles(BoxNumber);
                    }
                    else if (action == TileAction.Logout)
                    {
                        ProcessTileActionForLogout();
                    }
                    else if (action == TileAction.HTML)
                    {
                        ProcessTileActionForAnimation(FloorData.Tiles[tileID].ActionURI);
                    }
                    else if (action == TileAction.QRCode)
                    {
                        //Show the main grid
                        ((Grid)FindName("MainOptionGrid")).Visibility = Visibility.Visible;
                        ((TextBlock)FindName("MainOptionGridLabel")).Text = FloorData.Tiles[tileID].Name;
                        g = ProcessTileActionForQrCode(FloorData.Tiles[tileID].ActionURI);
                        //No tiles would be laid out since its QR code, so even if user specifies options, it would not make sense.
                        //LayoutTiles(BoxNumber);
                    }
                    else if (action == TileAction.Nothing || action == TileAction.NothingOnFloor)
                    {
                        Globals.currentAppState = RippleSystemStates.Start;
                        Globals.CurrentlySelectedParent = 0;
                    }
                    UpdateLayout();

                    //Send Message to the Screen if the tile does something
                    if (action != TileAction.Logout && action != TileAction.Nothing)
                    {
                        MessageSender.SendMessage(tileID);
                    }
                    if (_qrMode)
                    {
                        //Send HTML Message to the Screen in case it is HTML
                        MessageSender.SendMessage("HTML:SessionID," + g);
                    }

                }

                //User selected Sub Option
                else if (Globals.currentAppState == RippleSystemStates.OptionSelected)
                {
                    var parentTileID = "Tile" + Globals.CurrentlySelectedParent;
                    tileID = "Tile" + Globals.CurrentlySelectedParent + "SubTile" + BoxNumber;
                    action = FloorData.Tiles[parentTileID].SubTiles[tileID].Action;
                    Globals.SelectedOptionFullName = tileID;

                    //Call the tile transition for Standard Action tiles
                    if (action == TileAction.Standard)
                    {
                        DoTileTransitionForMainOption(Globals.CurrentlySelectedParent);
                    }
                    else if (action == TileAction.Nothing || action == TileAction.NothingOnFloor)
                    { }
                    else if (action == TileAction.Logout)
                    {
                        ProcessTileActionForLogout();
                    }
                    else if (action == TileAction.HTML)
                    {
                        ProcessTileActionForAnimation(FloorData.Tiles[parentTileID].SubTiles[tileID].ActionURI);
                    }
                    else if (action == TileAction.QRCode)
                    {
                        ((TextBlock)FindName("MainOptionGridLabel")).Text = FloorData.Tiles[parentTileID].SubTiles[tileID].Name;
                        g = ProcessTileActionForQrCode(FloorData.Tiles[parentTileID].SubTiles[tileID].ActionURI);
                    }

                    UpdateLayout();

                    //Send Message to the Screen if the tile does something
                    if (action != TileAction.Logout && action != TileAction.Nothing)
                    {
                        MessageSender.SendMessage(tileID);
                    }

                    if (_qrMode)
                    {
                        //Send HTML Message to the Screen in case it is HTML - Check with smarth on how to pass the message
                        MessageSender.SendMessage("HTML:SessionID," + g);
                    }
                }

            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in OnOptionSelected for box number {1}: {0}", ex.Message, BoxNumber);
            }

        }

        #endregion

        #region Floor Content Processing

        public void ArrangeFloor()
        {
            try
            {
                //Dispose if the initial animation is still active, in case the unlock happened through any method except gesture or animation
                //if (floorData.Start.Animation.AnimType == AnimationType.Flash && player != null)
                //{
                //    //Stop the flash.
                //    player.Stop();
                //    player.Width = 0;
                //    player.Height = 0;
                //    player.Dispose();
                //    host.Dispose();
                //    player = null;
                //    host = null;
                //}

                //else 
                if (FloorData.Start.Animation.AnimType == AnimationType.HTML && BrowserElement != null)
                {
                    if (BrowserElement != null)
                        BrowserElement.Dispose();
                    BrowserElement = null;

                    if (BrowserHost != null)
                        BrowserHost.Dispose();
                    BrowserHost = null;

                    _helper = null;
                }

                FlashContainer.Children.Clear();
                MainContainer.Visibility = Visibility.Visible;
                FlashContainer.Visibility = Visibility.Collapsed;
                ((Grid)FindName("MainOptionGrid")).Visibility = Visibility.Collapsed;
                ((Image)FindName("OverlayImage")).Visibility = Visibility.Collapsed;

                //Reset the tile content
                LayoutTiles(-1);

                //Play the Ripple video after attaching the event handler
                ((MediaElement)FindName("FloorVideoControl")).MediaEnded += FloorVideoControl_MediaEnded;
                ((MediaElement)FindName("FloorVideoControl")).Source = new Uri(HelperMethods.GetAssetUri(FloorData.UpperTile.Content));
                ((MediaElement)FindName("FloorVideoControl")).Play();

                //Update the UI
                UpdateLayout();
                Focus();
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in Arrange Floor {0} {1}", ex.Message, ex.StackTrace);
            }

            //DoTileTransitionForMainOption(0);

            //Click on the screen
            RippleCommonUtilities.HelperMethods.ClickOnFloorToGetFocus();

        }

        public void ShowStartOptions()
        {
            try
            {
                //Show the floor options
                MainContainer.Visibility = Visibility.Visible;
                FlashContainer.Visibility = Visibility.Collapsed;
                ((Image)FindName("OverlayImage")).Visibility = Visibility.Collapsed;
                ((Grid)FindName("MainOptionGrid")).Visibility = Visibility.Collapsed;

                LayoutTiles(0);

                //Update the UI
                UpdateLayout();
                Focus();

                //Click on the screen
                RippleCommonUtilities.HelperMethods.ClickOnFloorToGetFocus();
            }
            catch (Exception ex)
            {
                //Do nothing
                LoggingHelper.LogTrace(1, "Went wrong in ShowStartOptions UI {0}", ex.Message);
            }

        }

        private void LayoutTiles(int parent)
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
                foreach (var item in FloorData.Tiles.Values)
                {
                    try
                    {
                        //Set the label for the tile
                        SetAttributesForWindowsControls<TextBlock>(item.Id + Label, "Text", item.Name);

                        //Clear the inner content grid either way
                        ((Grid)FindName(InnerContent + item.Id)).Children.Clear();

                        //Clear the animation if any
                        try
                        {
                            var sbt = (Storyboard)FindName(item.Id + "SB");
                            sbt.Stop();
                        }
                        catch (Exception) { }


                        //Set the content for the tile if the tile type is not text
                        if (item.TileType != TileType.Text && (!String.IsNullOrEmpty(item.Content)))
                            ShowTileContent(item.Id, item.TileType, HelperMethods.GetAssetUri(item.Content));
                    }
                    catch (Exception)
                    {
                        //Do nothing
                    }
                }
            }
            else if (parent > 0)
            {
                foreach (var item in FloorData.Tiles["Tile" + parent].SubTiles.Values)
                {
                    try
                    {
                        tileID = item.Id.Substring(item.Id.LastIndexOf("Tile"));

                        //Set the label for the tile
                        SetAttributesForWindowsControls<TextBlock>(tileID + Label, "Text", item.Name);

                        //Clear the inner content grid either way
                        ((Grid)FindName(InnerContent + tileID)).Children.Clear();

                        //Clear the animation if any
                        try
                        {
                            var sbt = (Storyboard)FindName(tileID + "SB");
                            sbt.Stop();
                        }
                        catch (Exception) { }

                        //Set the content for the tile if the tile type is not text
                        if (item.TileType != TileType.Text && (!String.IsNullOrEmpty(item.Content)))
                            ShowTileContent(tileID, item.TileType, HelperMethods.GetAssetUri(item.Content));
                    }
                    catch (Exception)
                    {
                        //Do nothing
                    }
                }
            }
            UpdateLayout();

            //Call tile transitions
            DoTileTransitionForMainOption(Globals.CurrentlySelectedParent);
        }

        private void ShowTileContent(String tileID, TileType tileType, String contentURI)
        {
            try
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
                        //Add live tile animation
                        //Set for image
                        var sb = _liveTile.Clone();
                        sb.Name = tileID + "SB";
                        try
                        {
                            RegisterName(sb.Name, sb);
                        }
                        catch (Exception)
                        {

                            UnregisterName(sb.Name);
                            RegisterName(sb.Name, sb);
                        }
                        Storyboard.SetTarget(sb.Children[0], tile_img);
                        Storyboard.SetTarget(sb.Children[1], ((TextBlock)FindName(tileID + Label)));
                        Storyboard.SetTarget(sb.Children[2], ((TextBlock)FindName(tileID + Label)));
                        //Set keytimes to random values
                        //double val = rnd.NextDouble() * 2;
                        ////round off
                        //val = Math.Round(val, 2);
                        //TimeSpan tp = TimeSpan.FromSeconds(val);
                        //KeyTime newVal = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0));
                        //foreach (EasingDoubleKeyFrame sbItem in ((DoubleAnimationUsingKeyFrames)sb.Children[0]).KeyFrames)
                        //{
                        //    newVal = KeyTime.FromTimeSpan(sbItem.KeyTime.TimeSpan + tp);
                        //    sbItem.SetValue(EasingDoubleKeyFrame.KeyTimeProperty, newVal);
                        //}
                        //foreach (EasingDoubleKeyFrame sbItem in ((DoubleAnimationUsingKeyFrames)sb.Children[1]).KeyFrames)
                        //{
                        //    newVal = KeyTime.FromTimeSpan(sbItem.KeyTime.TimeSpan + tp);
                        //    sbItem.SetValue(EasingDoubleKeyFrame.KeyTimeProperty, newVal);
                        //}
                        //foreach (DiscreteObjectKeyFrame sbItem in ((ObjectAnimationUsingKeyFrames)sb.Children[2]).KeyFrames)
                        //{
                        //    newVal = KeyTime.FromTimeSpan(sbItem.KeyTime.TimeSpan + tp);
                        //    sbItem.SetValue(DiscreteObjectKeyFrame.KeyTimeProperty, newVal);
                        //}
                        sb.Begin();
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in ShowTileContent for TileID {0} and content URI {2} : {1}", tileID, ex.Message, contentURI);
            }
        }

        private void ClearFloorLabels()
        {
            foreach (var item in FloorData.Tiles.Values)
            {
                SetAttributesForWindowsControls<TextBlock>(item.Id + Label, "Text", "");
                ((Grid)FindName(InnerContent + item.Id)).Children.Clear();
            }

            //Set the start label
            ((TextBlock)FindName("Tile0Label")).Text = FloorData.Tiles["Tile0"].Name;
        }

        #region TileActions
        private void ProcessTileActionForAnimation(string actionURI)
        {
            try
            {
                //Set the application state
                Globals.currentAppState = RippleSystemStates.UserPlayingAnimations;

                //Stop the video
                ((MediaElement)FindName("FloorVideoControl")).Stop();

                //Project the HTML or flash content onto the floor
                ProjectAnimationContentOnFloor(actionURI);
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in ProcessTileActionForAnimation for URI {1}: {0}", ex.Message, actionURI);
            }
        }

        private String ProcessTileActionForQrCode(string actionURI)
        {
            try
            {
                //Set the mode
                _qrMode = true;

                //TODO = Hide other options
                ClearFloorLabels();

                //Show QR Code on the floor
                //Add session Hint to QRCode - TODO Later 
                var g = Guid.NewGuid();
                var URL = actionURI + "?SessionID=" + g.ToString();
                var bitmap = RippleCommonUtilities.HelperMethods.GenerateQRCode(URL);
                BitmapImage bitmapImage;
                using (var memory = new MemoryStream())
                {
                    bitmap.Save(memory, ImageFormat.Png);
                    memory.Position = 0;
                    bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                }

                ((Image)FindName("OverlayImage")).Visibility = Visibility.Visible;
                ((Image)FindName("OverlayImage")).Source = bitmapImage;

                DoTileTransitionForMainOption(Globals.CurrentlySelectedParent);

                return g.ToString();
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in QR code generation {0}", ex.Message);
                return String.Empty;
            }
        }

        private void ProcessTileActionForLogout()
        {
            ResetUi();

            //Send message to the screen
            MessageSender.SendMessage("Reset");
        }

        private void ProjectAnimationContentOnFloor(string actionUri)
        {
            try
            {
                //Set the visibility
                MainContainer.Visibility = Visibility.Collapsed;
                FlashContainer.Visibility = Visibility.Visible;

                //if (Path.GetExtension(actionUri).ToLower().Equals(".swf"))
                //{
                //    //Play flash animation
                //    host = new WindowsFormsHost();
                //    player = new FlashAxControl();
                //    host.Child = player;
                //    FlashContainer.Children.Clear();
                //    FlashContainer.Children.Add(host);

                //    //set size - based on the resolution
                //    player.Width = (int)Globals.CurrentResolution.HorizontalResolution;
                //    player.Height = (int)Globals.CurrentResolution.VerticalResolution;
                //    //load & play the movie
                //    player.LoadMovie(Utilities.HelperMethods.GetAssetURI(actionUri));
                //    player.Play();

                //    //Get the window focus
                //    this.Focus();
                //}
                //else
                {
                    //Play HTML animations
                    _helper = new ScriptingHelper(this);
                    _helper.PropertyChanged += helper_PropertyChanged;

                    BrowserElement = new WebBrowser
                    {
                        ScriptErrorsSuppressed = true, 
                        ObjectForScripting = _helper
                    };

                    BrowserHost = new WindowsFormsHost
                    {
                        Child = BrowserElement
                    };

                    FlashContainer.Children.Clear();
                    FlashContainer.Children.Add(BrowserHost);

                    
                    var fileLocation = HelperMethods.GetAssetUri(actionUri);
                    var pageUri = String.Empty;

                    if (File.Exists(fileLocation))
                    {
                        var pathParts = fileLocation.Split(new char[] { ':' });
                        pageUri = "file://127.0.0.1/" + pathParts[0] + "$" + pathParts[1];
                    }
                    else
                    {
                        pageUri = actionUri;
                    }

                    BrowserElement.Navigate(pageUri);
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in ProjectAnimationContentOnFloor for URI {1}: {0}", ex.Message, actionUri);
            }
        }

        #endregion

        #endregion

        #region Color and Animation Helpers

        /// <summary>
        /// Code to start animation on the box, for the locking period
        /// </summary>
        /// <param name="boxNumber"></param>
        /// <param name="isAnimation"></param>
        public void StartAnimationInBox(int boxNumber, bool isAnimation)
        {
            //Reset all the floor and start animation only on box "value"
            StopAllAnimations();

            //Get the tile action type
            var tileAction = TileAction.Standard;
            
            //Main Option
            if (Globals.currentAppState == RippleSystemStates.Start)
                tileAction = FloorData.Tiles["Tile" + boxNumber].Action;
            else if (Globals.currentAppState == RippleSystemStates.OptionSelected && boxNumber != 0)
                tileAction = FloorData.Tiles["Tile" + Globals.CurrentlySelectedParent].SubTiles["Tile" + Globals.CurrentlySelectedParent + "SubTile" + boxNumber].Action;

            //Return for static tiles
            
            //Return in case the Floor is in QR Mode or if tile does nothing
            if (tileAction == TileAction.Nothing || tileAction == TileAction.NothingOnFloor || _qrMode)
                return;

            ((Grid)FindName("Tile" + boxNumber)).Background = isAnimation ? new SolidColorBrush(Colors.Yellow) : new SolidColorBrush(Colors.Red);
        }

        public void StopAllAnimations()
        {
            ResetColorsForMainOption(Globals.CurrentlySelectedParent);

            //Update the UI
            UpdateLayout();
        }

        private void DoTileTransitionForMainOption(int Parent)
        {
            try
            {
                #region AnimateFloor
                ((Grid)FindName("Tile0")).Background = new SolidColorBrush(FloorData.Tiles["Tile0"].Color);
                List<Tile> tileList = null;

                //Set the colors
                if (Parent > 0)
                {
                    ((Grid)FindName("MainOptionGrid")).Background = new SolidColorBrush(FloorData.Tiles["Tile" + Parent].Color);
                    tileList = FloorData.Tiles["Tile" + Parent].SubTiles.Values.ToList<Tile>();
                }
                else if (Parent == 0)
                {
                    tileList = FloorData.Tiles.Values.ToList<Tile>();
                }


                var val = 0;
                EasingColorKeyFrame kFrame;
                foreach (var item in tileList)
                {
                    val = (Convert.ToInt32(item.Id.Substring(item.Id.LastIndexOf('e') + 1)) * 2) - 1;
                    if (val >= 0)
                    {
                        kFrame = (EasingColorKeyFrame)((ColorAnimationUsingKeyFrames)_tileTransitionSb.Children[val]).KeyFrames[0].Clone();
                        kFrame.Value = item.Color;
                        ((ColorAnimationUsingKeyFrames)_tileTransitionSb.Children[val]).KeyFrames[0] = kFrame;
                    }

                }

                //Play the music
                using (var soundPlayer = new SoundPlayer(HelperMethods.GetAssetUri(FloorData.Transition.Music)))
                {
                    soundPlayer.Play(); // can also use soundPlayer.PlaySync()
                }

                //Start the tile transition
                _tileTransitionSb.Begin();

                #endregion
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in DoTileTransition for parent {0}: {1}", Parent, ex.Message);
            }

        }

        private void ResetColorsForMainOption(int Parent)
        {
            try
            {
                //Set teh background value for Start
                ((Grid)FindName("Tile0")).Background = new SolidColorBrush(FloorData.Tiles["Tile0"].Color);
                //Set the colors
                if (Parent > 0)
                {
                    ((Grid)FindName("MainOptionGrid")).Background = new SolidColorBrush(FloorData.Tiles["Tile" + Parent].Color);

                    foreach (var item in FloorData.Tiles["Tile" + Parent].SubTiles.Values)
                    {
                        SetAttributesForWindowsControls<Grid>(item.Id.Substring(item.Id.LastIndexOf("Tile")), "Background", new SolidColorBrush(item.Color));
                    }
                }
                else if (Parent == 0)
                {
                    foreach (var item in FloorData.Tiles.Values)
                    {
                        SetAttributesForWindowsControls<Grid>(item.Id, "Background", new SolidColorBrush(item.Color));
                    }
                }

                UpdateLayout();
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in ResetColorsForMainOption {0}, {1}", Parent, ex.Message);
            }
        }
        #endregion

        #region Helpers

        private Tile GetFloorTileForID(string tileId)
        {
            Tile result = null;

            if (FloorData.Tiles.ContainsKey(tileId))
            {
                result = FloorData.Tiles[tileId];
                return result;
            }

            // check the subtiles
            var subTileId = tileId.Substring(0, tileId.LastIndexOf("SubTile", StringComparison.Ordinal));
            if (FloorData.Tiles[subTileId].SubTiles.ContainsKey(tileId))
            {
                result = FloorData.Tiles[subTileId].SubTiles[tileId];
            }

            return result;
        }

        private void SetAttributesForWindowsControls<TInstanceType>(string objectName, string propertyName, object propertyValue)
        {
            var objectInstanceType = (TInstanceType)FindName(objectName);

            if (objectInstanceType != null)
            {
                var prop = typeof(TInstanceType).GetProperty(propertyName);
                prop.SetValue(objectInstanceType, propertyValue, null);
            }
        }

        #endregion

        private void FloorWindow1_Loaded(object sender, RoutedEventArgs e)
        {
            ResetUi();
            //Globals.currentAppState = RippleSystemStates.UserDetected;
            //ArrangeFloor();
            //Globals.currentAppState = RippleSystemStates.UserWaitToGoOnStart;
            //ShowStartOptions();
            //Globals.currentAppState = RippleSystemStates.Start;
            //Globals.CurrentlySelectedParent = 5;
            //OnOptionSelected(5);
            //BackgroundWorker vf = new BackgroundWorker();
            //vf.DoWork += vf_DoWork;
            //vf.RunWorkerCompleted += vf_RunWorkerCompleted;
            //vf.RunWorkerAsync();

            ////Globals.CurrentlySelectedParent = 7;
            ////Globals.SelectedOptionFullName = "Tile7";
            //OnGestureDetected(GestureTypes.RightSwipe);
            //////OnStartSelected();
            ////OnOptionSelected(8);
        }
    }
}
