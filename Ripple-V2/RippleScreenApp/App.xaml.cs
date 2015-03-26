using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using RippleCommonUtilities;
using RippleScreenApp.Utilities;

namespace RippleScreenApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var top = 0.0;
            var left = 0.0;
            double HRes = 1280;
            double VRes = 800;
            for (var i = 0; i != e.Args.Length; ++i)
            {
                if (e.Args[i] == "/Top")
                {
                    top = Convert.ToDouble(e.Args[++i]);
                }
                else if (e.Args[i] == "/Left")
                {
                    left = Convert.ToDouble(e.Args[++i]);
                }
                else if (e.Args[i] == "/VRes")
                {
                    VRes = Convert.ToDouble(e.Args[++i]);
                }
                else if (e.Args[i] == "/HRes")
                {
                    HRes = Convert.ToDouble(e.Args[++i]);
                }
            }

            //Set the globals
            Globals.CurrentResolution.VerticalResolution = VRes;
            Globals.CurrentResolution.HorizontalResolution = HRes;
            Globals.CurrentResolution.XOrigin = left;
            Globals.CurrentResolution.YOrigin = top;

            // Create main application window
            var screenWin = new ScreenWindow();
            screenWin.WindowStartupLocation = WindowStartupLocation.Manual;
            screenWin.Top = top;
            screenWin.Left = left;
            screenWin.BorderThickness = new Thickness(0.2);
            screenWin.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0072C6"));
            screenWin.WindowStyle = WindowStyle.None;
            screenWin.Height = VRes;
            screenWin.Width = HRes;
            screenWin.ResizeMode = ResizeMode.NoResize;
            screenWin.Show();
        }

        private void Application_Exit_1(object sender, ExitEventArgs e)
        {
            //Commit the telemetry data
            TelemetryWriter.CommitTelemetry();
        }

        private void Application_DispatcherUnhandledException_1(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LoggingHelper.LogTrace(1, "Went wrong in screen {0}", e.Exception.Message);
            e.Handled = true;
        }
    }
}
