﻿using System;
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
            
            var screenWin = new ScreenWindow
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                Top = top,
                Left = left,
                BorderThickness = new Thickness(0.2),
                BorderBrush = new SolidColorBrush((Color) ColorConverter.ConvertFromString("#0072C6")),
                WindowStyle = WindowStyle.None,
                Height = vRes,
                Width = hRes,
                ResizeMode = ResizeMode.NoResize
            };

#if DEBUG // we need a little bit more control when debugging
            screenWin.WindowState = WindowState.Normal;
            screenWin.WindowStyle = WindowStyle.SingleBorderWindow;
            screenWin.ResizeMode = ResizeMode.CanResize;
#endif

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
