using System.Windows;
using System.Windows.Media;

namespace RippleCalibration
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Create main application window
            var floorWin = new MainWindow
            {
                Top = 0.0,
                Left = 0.0,
                Topmost = true,
                BorderThickness = new Thickness(0.2),
                BorderBrush = new SolidColorBrush((Color) ColorConverter.ConvertFromString("#0072C6")),
                WindowStartupLocation = WindowStartupLocation.Manual,
                Width = SystemParameters.PrimaryScreenWidth,
                Height = SystemParameters.PrimaryScreenHeight,
                WindowState = WindowState.Maximized,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize
            };


#if DEBUG // we need a little bit more control when debugging
            floorWin.Width = 800;
            floorWin.Height = 600;

            floorWin.WindowState = WindowState.Normal;
            floorWin.WindowStyle = WindowStyle.SingleBorderWindow;
            floorWin.ResizeMode = ResizeMode.CanResize;
#endif

            
            
            floorWin.Show();
        }
    }
}
