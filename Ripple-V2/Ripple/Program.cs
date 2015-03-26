using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using RippleCommonUtilities;

namespace Ripple
{
    //Launches both the applications
    class Program
    {
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool Handler(CtrlType sig)
        {
            switch (sig)
            {
                case CtrlType.CTRL_C_EVENT:
                case CtrlType.CTRL_BREAK_EVENT:
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                case CtrlType.CTRL_CLOSE_EVENT:
                    //Stop the logging
                    LoggingHelper.StopLogging();
                    break;
                default:
                    break;
            }
            return true;
        }

        static void Main(string[] args)
        {
            //Handler to handle close window events
            //_handler += new EventHandler(Handler);
            //SetConsoleCtrlHandler(_handler, true);

            //Attach global exception handler
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            //Start the logging
            //RippleCommonUtilities.LoggingHelper.StartLogging("RippleApp");

            var screenList = Screen.AllScreens;

            //Check whether only 2 displays connected
            Console.WriteLine("Checking for connected displays...\r\n");
            if (screenList.Length < 2)
            {
                Console.WriteLine("There should at least be 2 displays connected. \r\n");
                Console.WriteLine("Press enter to exit\r\n");
                Console.Read();
                return;
            }

            Console.WriteLine("Displays verified successfully\r\n\r\n");


            //Get the resolution for both the displays
            Console.WriteLine("Checking for resolution for the displays...\r\n");
            var sample = String.Empty;

            //Get individual resolutions
            var floorAppResolution = new RippleScreenResoultion();
            var screenAppResolution = new RippleScreenResoultion();
            
            foreach (var scr in screenList)
            {
                //It means its the floor
                if (scr.Primary)
                {
                    floorAppResolution.ScreenName = scr.DeviceName;
                    floorAppResolution.HorizontalResolution = SystemParameters.PrimaryScreenWidth;
                    floorAppResolution.VerticalResolution = SystemParameters.PrimaryScreenHeight;
                    floorAppResolution.XOrigin = scr.Bounds.Left;
                    floorAppResolution.YOrigin = scr.Bounds.Top;
                }
                //It means its the screen, since only 2 displays are connected
                else
                {
                    screenAppResolution.ScreenName = scr.DeviceName;
                    screenAppResolution.HorizontalResolution = SystemParameters.VirtualScreenWidth - SystemParameters.PrimaryScreenWidth;
                    screenAppResolution.VerticalResolution = SystemParameters.VirtualScreenHeight;
                    screenAppResolution.XOrigin = SystemParameters.PrimaryScreenWidth;
                    screenAppResolution.YOrigin = scr.Bounds.Top;
                }

                sample += String.Format("DeviceName: {0} \r\nBounding Rectangle: W{1} h{2}\r\n", scr.DeviceName, scr.Bounds.Width, scr.Bounds.Height);
            }

            //Check whether the primary screen has 0,0 origin
            if (!(floorAppResolution.XOrigin == 0 && floorAppResolution.YOrigin == 0))
            {
                Console.WriteLine("Please set the primary screen with origin 0,0 by moving it to the left most in the Screen Resolution window for the machine\r\n");
                Console.WriteLine("Press enter to exit");
                
                Console.Read();
                return;
            }

            Console.WriteLine(sample);
            Console.WriteLine("Resolution verified successfully\r\n\r\n");

            Console.WriteLine("Starting, Please wait a moment");
            Thread.Sleep(1000);
            
            //Project floor on the primary display
            Console.WriteLine("Starting Floor Application on the primary screen...\r\n");
            StartFloorApplication(floorAppResolution.YOrigin, floorAppResolution.XOrigin, floorAppResolution.VerticalResolution, floorAppResolution.HorizontalResolution);

            //Project screen on the secondary display
            Console.WriteLine("Starting Screen Application on the primary screen...\r\n");
            StartScreenApplication(screenAppResolution.YOrigin, screenAppResolution.XOrigin, screenAppResolution.VerticalResolution, screenAppResolution.HorizontalResolution);

            //Confirm with the user if he is happy, else ask him to make floor projection as the primary display and make it the left most.
            Console.WriteLine("Are you happy with the projection setup? y:n\r\n");
            //char happyState = 'n';
            return;
            //if (happyState == 'y')
            //{
            //    Console.WriteLine("We are happy that you liked the setup, have fun playing around\r\n");
            //    Console.WriteLine("Press enter to exit\r\n");
            //    Console.Read();
            //    return;
            //}
            //else
            //{
            //    //TODO - record the feedback
            //    Console.WriteLine("We would improve the setup further, thanks a lot for your feedback\r\n");
            //    Console.WriteLine("Press enter to exit\r\n");
            //    Console.Read();
            //    return;
            //}
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            //Do nothing
            Console.WriteLine(e.ExceptionObject.ToString());
            Console.Read();
        }

        private static void StartScreenApplication(double top, double left, double vRes, double hRes)
        {
            try
            {
                const string executable = "..\\..\\..\\RippleScreenApp\\bin\\Debug\\RippleScreenApp.exe";
                StartApplication(top, left, vRes, hRes, executable);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Read();
            }
        }

        private static void StartFloorApplication(double top, double left, double vRes, double hRes)
        {
            try
            {
                const string executable = "..\\..\\..\\RippleFloorApp\\bin\\Debug\\RippleFloorApp.exe";
                StartApplication(top, left, vRes, hRes, executable);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Read();
            }
        }

        private static void StartApplication(double top, double left, double vRes, double hRes, string executable)
        {
            var executingFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var screenApplicationPath = Path.Combine(executingFolder, executable);

            if (File.Exists(screenApplicationPath))
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = screenApplicationPath,
                    Arguments = "/Top " + top + " /Left " + left + " /VRes " + vRes + " /HRes " + hRes
                };

                Process.Start(startInfo);
            }
            else
            {
                var message = String.Format("Unable to find Screen Application at {0}", screenApplicationPath);
                throw new FileNotFoundException(message);
            }
        }
    }
}
