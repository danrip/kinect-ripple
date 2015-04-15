using System;
using System.Configuration;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using System.Windows.Threading;
using RippleCommonUtilities;

namespace RippleFloorApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var fileLocation = ConfigurationManager.AppSettings["LogFileLocation"];
            const string componentName = "Ripple Floor App";
            
            if (!String.IsNullOrEmpty(fileLocation))
            {
                LoggingHelper.StartLogging(componentName, fileLocation);
            }
            else
            {
                LoggingHelper.StartLogging(componentName);
            }

            var top = 0.0;
            var left = 0.0;
            double hRes = 1280;
            double vRes = 800;
            
            for (var i = 0; i != e.Args.Length; ++i)
            {
                switch (e.Args[i].ToLower())
                {
                    case "/top":
                        top = Convert.ToDouble(e.Args[++i]);
                        break;
                    case "/left":
                        left = Convert.ToDouble(e.Args[++i]);
                        break;
                    case "/vres":
                        vRes = Convert.ToDouble(e.Args[++i]);
                        break;
                    case "/hres":
                        hRes = Convert.ToDouble(e.Args[++i]);
                        break;
                }
            }

            //Set the globals
            Globals.CurrentResolution.VerticalResolution = vRes;
            Globals.CurrentResolution.HorizontalResolution = hRes;
            Globals.CurrentResolution.XOrigin = left;
            Globals.CurrentResolution.YOrigin = top;

            // Create main application window
            var floorWin = new FloorWindow(new WindowsFormsHost())
            {
                Top = top,
                Left = left,
                Topmost = true,
                BorderThickness = new Thickness(0.2),
                BorderBrush = new SolidColorBrush((Color) ColorConverter.ConvertFromString("#0072C6")),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Width = hRes,
                Height = vRes,
                WindowState = WindowState.Maximized,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize
            };
            

#if DEBUG // we need a little bit more control when debugging
            floorWin.Topmost = false;
            floorWin.WindowState = WindowState.Normal;
            floorWin.WindowStyle = WindowStyle.SingleBorderWindow;
            floorWin.ResizeMode = ResizeMode.CanResize;

            BuildDebugMenu();
#endif

            floorWin.Show();
        }

        private void BuildDebugMenu()
        {
            var menu = new MenuStrip();
           
        }

        private void Application_Exit_1(object sender, ExitEventArgs e)
        {
            //Stop the logging session
            LoggingHelper.StopLogging();
        }

        private void Application_DispatcherUnhandledException_1(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            //Stop the logging session
            LoggingHelper.LogTrace(1, "Something went wrong with the Floor : {0}", e.Exception.Message);
           
            e.Handled = true;
        }
    }
}
