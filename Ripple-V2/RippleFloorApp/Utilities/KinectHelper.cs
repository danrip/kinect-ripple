using System;
using System.IO;
using System.Reflection;

using System.ComponentModel;
using Microsoft.Kinect;
using RippleCommonUtilities;
using System.Windows.Input;
using System.Timers;
using RippleDictionary;
using RippleFloorApp.Utilities.KinectGestures;
using RippleFloorApp.Utilities.KinectGestures.Segments;

namespace RippleFloorApp.Utilities
{
    public class KinectHelper : INotifyPropertyChanged
    {
        BodyFrameReader _reader;
        Body[] _bodies;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor _sensor;

        private int _currentLocation;
        private RippleSystemStates _currentState;
        private String _kinectConnectionState;
        private GestureTypes _kinectGestureDetected;

        private static double _frontDistance;
        private static double _backDistance;
        private static double _leftDistance;
        private static double _rightDistance;

        private static double[] _leftTileBoundary;
        private static double[] _topTileBoundary;

        private static double[] _rightTileBoundary;
        private static double[] _bottomTileBoundary;

        private GestureController _gestureController;

        
        Timer _clearTimer;

        public static DateTime LastUserVisibleTime = new DateTime();
        public static int TileCount = 0;
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }

        public int CurrentLocation
        {
            get { return _currentLocation; }
            set
            {
                _currentLocation = value;
                OnPropertyChanged(new PropertyChangedEventArgs("CurrentLocation"));
            }
        }

        public RippleSystemStates CurrentState
        {
            get { return _currentState; }
            set
            {
                _currentState = value;
                OnPropertyChanged(new PropertyChangedEventArgs("CurrentState"));
            }
        }

        public KinectHelper()
        {
            //Get the callibration data
            var config = Dictionary.GetFloorConfigurations(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            _frontDistance = config.FrontDistance;
            _backDistance = config.BackDistance;
            _leftDistance = config.LeftDistance;
            _rightDistance = config.RightDistance;


            //Get the floor tile data
            TileCount = FloorWindow.FloorData.Tiles.Count;

            var tileOriginX = new double[TileCount];
            var tileOriginY = new double[TileCount];

            var tileWidth = new double[TileCount];
            var tileHeight = new double[TileCount];

            var floorWidth = new double[TileCount];
            var floorHeight = new double[TileCount];

            _leftTileBoundary = new double[TileCount];
            _topTileBoundary = new double[TileCount];

            _rightTileBoundary = new double[TileCount];
            _bottomTileBoundary = new double[TileCount];

            for (var i = 0; i < TileCount; i++)
            {
                // Origin at top left (towards the side of the video)
                tileOriginX[i] = FloorWindow.FloorData.Tiles["Tile" + i].Coordinate.X;
                tileOriginY[i] = FloorWindow.FloorData.Tiles["Tile" + i].Coordinate.Y;

                tileWidth[i] = FloorWindow.FloorData.Tiles["Tile" + i].Style.Width;
                tileHeight[i] = FloorWindow.FloorData.Tiles["Tile" + i].Style.Height;

                floorWidth[i] = _rightDistance - _leftDistance;
                floorHeight[i] = _backDistance - _frontDistance;

                _leftTileBoundary[i] = _leftDistance + tileOriginX[i] * floorWidth[i];
                _topTileBoundary[i] = _frontDistance + tileOriginY[i] * floorHeight[i];

                _rightTileBoundary[i] = _leftDistance + tileOriginX[i] * floorWidth[i] + tileWidth[i] * floorWidth[i];
                _bottomTileBoundary[i] = _frontDistance + tileOriginY[i] * floorHeight[i] + tileHeight[i] * floorHeight[i];
            }


            //if (KinectSensor.KinectSensors.Count > 0)
            //{

            _sensor = KinectSensor.GetDefault();
            if (_sensor != null)
            {
                Initialize();
            }
            else
            {
                KinectConnectionState = "Disconnected";
            }
            //}
            //else
            //{
            //    KinectConnectionState = "Disconnected";
            //}
            // activeRecognizer = CreateRecognizer();
        }

        #region Event Handlers

        public String KinectConnectionState
        {
            get { return _kinectConnectionState; }
            set
            {
                _kinectConnectionState = value;
                OnPropertyChanged(new PropertyChangedEventArgs("KinectConnectionState"));
            }
        }

        public GestureTypes KinectGestureDetected
        {
            get { return _kinectGestureDetected; }
            set
            {
                _kinectGestureDetected = value;
                OnPropertyChanged(new PropertyChangedEventArgs("KinectSwipeDetected"));
            }
        }

        void Initialize()
        {
            if (_sensor != null)
            {
                // open the sensor
                _sensor.Open();

                //bodies = new Body[sensor.BodyFrameSource.BodyCount];

                // open the reader for the body frames
                _reader = _sensor.BodyFrameSource.OpenReader();

                if (_reader != null)
                {
                    _reader.FrameArrived += sensor_SkeletonFrameReady;
                }


                KinectConnectionState = "Connected";
                // add timer for clearing last detected gesture
                _clearTimer = new Timer(2000);
                _clearTimer.Elapsed += new ElapsedEventHandler(clearTimer_Elapsed);
                // initialize the gesture recognizer
                _gestureController = new GestureController();
                _gestureController.GestureRecognized += OnGestureRecognized;

                // register the gestures for this demo
                RegisterGestures();
            }
            //if (sensor != null)
            //{
            //    this.sensor.SkeletonStream.Enable();
            //    this.sensor.SkeletonFrameReady += sensor_SkeletonFrameReady;
            //    this._FrameSkeletons = new Skeleton[this.sensor.SkeletonStream.FrameSkeletonArrayLength];
            //    sensor.Start();
            //    KinectConnectionState = "Connected";
            //}

        }

        void Unitialize()
        {
            if (_reader != null)
            {
                // BodyFrameReder is IDisposable
                _reader.Dispose();
                _reader = null;
            }

            // Body is IDisposable
            if (_bodies != null)
            {
                _bodies = null;
                //foreach (Body body in this.bodies)
                //{
                //    if (body != null)
                //    {
                //        body.Dispose();
                //    }
                //}
            }

            if (_sensor != null)
            {
                _sensor.Close();
                _sensor = null;
            }

            if (_gestureController != null)
            {
                _gestureController.GestureRecognized -= OnGestureRecognized;
                _gestureController = null;
            }
        }

        #region Improve the code
        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Gesture event arguments.</param>
        private void OnGestureRecognized(object sender, GestureEventArgs e)
        {
            switch (e.GestureName)
            {
                case "JoinedHands":
                    //KinectSwipeDetected = GestureTypes.JoinedHands;
                    break;
                case "SwipeLeft":
                    KinectGestureDetected = GestureTypes.RightSwipe;
                    break;
                case "SwipeRight":
                    KinectGestureDetected = GestureTypes.LeftSwipe;
                    break;
                case "SwipeUp":
                    //KinectSwipeDetected = GestureTypes.SwipeUp;
                    break;
                default:
                    break;
            }

            _clearTimer.Start();
        }

        /// <summary>
        /// Helper function to register all available 
        /// </summary>
        private void RegisterGestures()
        {

            var joinedhandsSegments = new IRelativeGestureSegment[20];
            var joinedhandsSegment = new JoinedHandsSegment1();
            for (var i = 0; i < 20; i++)
            {
                // gesture consists of the same thing 10 times 
                joinedhandsSegments[i] = joinedhandsSegment;
            }
            _gestureController.AddGesture("JoinedHands", joinedhandsSegments);

            var swipeleftSegments = new IRelativeGestureSegment[3];
            swipeleftSegments[0] = new SwipeLeftSegment1();
            swipeleftSegments[1] = new SwipeLeftSegment2();
            swipeleftSegments[2] = new SwipeLeftSegment3();
            _gestureController.AddGesture("SwipeLeft", swipeleftSegments);

            var swiperightSegments = new IRelativeGestureSegment[3];
            swiperightSegments[0] = new SwipeRightSegment1();
            swiperightSegments[1] = new SwipeRightSegment2();
            swiperightSegments[2] = new SwipeRightSegment3();
            _gestureController.AddGesture("SwipeRight", swiperightSegments);

            var swipeUpSegments = new IRelativeGestureSegment[3];
            swipeUpSegments[0] = new SwipeUpSegment1();
            swipeUpSegments[1] = new SwipeUpSegment2();
            swipeUpSegments[2] = new SwipeUpSegment3();
            _gestureController.AddGesture("SwipeUp", swipeUpSegments);

        }

        /// <summary>
        /// Clear text after some time
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void clearTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _kinectGestureDetected = GestureTypes.None;
            _clearTimer.Stop();
        }
        #endregion

        void sensor_SkeletonFrameReady(object sender, BodyFrameArrivedEventArgs e)
        {
            var Spine = new Joint();
            var LeftFeet = new Joint();
            var RightFeet = new Joint();
            var delta = 0.00f;
            var leftFeetXPosition = 0.00f;
            var rightFeetXPosition = 0.00f;

            try
            {
                var frameReference = e.FrameReference;

                var frame = frameReference.AcquireFrame();


                if (frame != null)
                {
                    // BodyFrame is IDisposable
                    using (frame)
                    {
                        if (_bodies == null)
                        {
                            _bodies = new Body[frame.BodyCount];
                        }
                        // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                        // As long as those body objects are not disposed and not set to null in the array,
                        // those body objects will be re-used.
                        frame.GetAndRefreshBodyData(_bodies);

                        var skeleton = GetPrimarySkeleton(_bodies);

                        if (skeleton != null)
                        {

                            if (skeleton.IsTracked)
                            {
                                delta = 0.00f;
                                Spine = skeleton.Joints[JointType.SpineMid];
                                LeftFeet = skeleton.Joints[JointType.FootLeft];
                                RightFeet = skeleton.Joints[JointType.FootRight];
                                leftFeetXPosition = LeftFeet.Position.X - delta;
                                rightFeetXPosition = RightFeet.Position.X + delta;
                            }
                            //else if (skeleton.TrackingState == SkeletonTrackingState.PositionOnly)
                            //{
                            //    delta = 0.08f;
                            //    LeftFeet.Position = skeleton.Position;
                            //    RightFeet.Position = skeleton.Position;
                            //    Spine.Position = skeleton.Position;
                            //    leftFeetXPosition = LeftFeet.Position.X - delta;
                            //    rightFeetXPosition = RightFeet.Position.X + delta;
                            //}

                            if (skeleton.IsTracked)
                            {
                                //Recognize Gestures either way
                                // update the gesture controller
                                _gestureController.UpdateAllGestures(skeleton);

                                //User being detected
                                LastUserVisibleTime = DateTime.Now;

                                if (Globals.currentAppState == RippleSystemStates.UserPlayingAnimations || Globals.currentAppState == RippleSystemStates.NoUser || Globals.currentAppState == RippleSystemStates.UserDetected)
                                {
                                    //Run Mouse Interop
                                    #region Calibrated MouseInterop
                                    if ((LeftFeet.Position.Z > _frontDistance && LeftFeet.Position.Z < _backDistance) && (LeftFeet.Position.X > (_leftDistance) && LeftFeet.Position.X < _rightDistance))
                                    {
                                        var CursorX = (((LeftFeet.Position.Z + RightFeet.Position.Z) / 2 - (_frontDistance)) / (_backDistance - _frontDistance)) * Globals.CurrentResolution.HorizontalResolution;
                                        CursorX = Globals.CurrentResolution.HorizontalResolution - CursorX;
                                        var CursorY = (((LeftFeet.Position.X + RightFeet.Position.X) / 2 - (_leftDistance)) / (_rightDistance - _leftDistance)) * Globals.CurrentResolution.VerticalResolution;
                                        var x = Convert.ToInt32(CursorX);
                                        var y = Convert.ToInt32(CursorY);
                                        Mouse.OverrideCursor = Cursors.None;

                                        //if (count == 0)
                                        //{
                                        //    RippleCommonUtilities.OSNativeMethods.SendMouseInput(x, y, (int)Globals.CurrentResolution.HorizontalResolution, (int)Globals.CurrentResolution.VerticalResolution, true);
                                        //    count = 10;
                                        //}
                                        //count--;
                                        //RippleCommonUtilities.OSNativeMethods.SendMouseInput(x, y, (int)Globals.CurrentResolution.HorizontalResolution, (int)Globals.CurrentResolution.VerticalResolution, true);
                                        OSNativeMethods.SendMouseInput(x, y, (int)Globals.CurrentResolution.HorizontalResolution, (int)Globals.CurrentResolution.VerticalResolution, false);
                                    }
                                    #endregion
                                }
                                //Run block identification only if not in above mode
                                else
                                {
                                    #region Calibrated Tile Detection

                                    var locationChanged = false;
                                    for (var i = 0; i < TileCount; i++)
                                    {
                                        if ((LeftFeet.Position.Z > _topTileBoundary[i] && LeftFeet.Position.Z < _bottomTileBoundary[i]) && (RightFeet.Position.Z > _topTileBoundary[i] && RightFeet.Position.Z < _bottomTileBoundary[i]) && (leftFeetXPosition > (_leftTileBoundary[i]) && leftFeetXPosition < _rightTileBoundary[i]) && (rightFeetXPosition > (_leftTileBoundary[i]) && rightFeetXPosition < _rightTileBoundary[i]))
                                        {
                                            CurrentLocation = i;
                                            locationChanged = true;
                                            i = 0;
                                            break;
                                        }
                                    }
                                    if (locationChanged == false)
                                    {
                                        CurrentLocation = -1;
                                    }

                                    #endregion
                                }
                            }
                            else
                            {
                                CurrentLocation = -1;
                            }
                        }
                        else
                        {
                            CurrentLocation = -1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Do nothing
                LoggingHelper.LogTrace(1, "Went wrong in Kinect helper {0}", ex.Message);
            }


        }

        private static Body GetPrimarySkeleton(Body[] skeletons)
        {

            Body skeleton = null;
            if (skeletons != null)
            {
                // Find the closest skeleton
                for (var i = 0; i < skeletons.Length; i++)
                {
                    if (skeletons[i].IsTracked)
                    {
                        var joints = skeletons[i].Joints;
                        double averagefeetdistanceZ = ((joints[JointType.FootLeft].Position.Z + joints[JointType.FootRight].Position.Z) / 2);
                        double averagefeetdistanceX = ((joints[JointType.FootLeft].Position.X + joints[JointType.FootRight].Position.X) / 2);

                        #region Calibrated Boundary
                        if ((averagefeetdistanceZ > _frontDistance && averagefeetdistanceZ < _backDistance) && (averagefeetdistanceX > _leftDistance && averagefeetdistanceX < _rightDistance))
                        {
                            if (skeleton != null)
                            {
                                var jointsold = skeleton.Joints;
                                double averagefeetdistanceoldZ = ((jointsold[JointType.FootLeft].Position.Z + jointsold[JointType.FootRight].Position.Z) / 2);
                                double averagefeetdistanceoldX = ((jointsold[JointType.FootLeft].Position.X + jointsold[JointType.FootRight].Position.X) / 2);

                                if (((averagefeetdistanceoldZ > _frontDistance && averagefeetdistanceoldZ < _backDistance) && (averagefeetdistanceoldX > (_leftDistance) && averagefeetdistanceoldX < _rightDistance)))
                                {
                                    if (averagefeetdistanceoldZ > averagefeetdistanceZ)
                                        skeleton = skeletons[i];
                                }
                            }
                            else
                            {
                                skeleton = skeletons[i];
                            }

                        }
                        #endregion
                        //    }
                    }
                }
            }
            return skeleton;
        }

        #endregion Event Handlers

    }
}
