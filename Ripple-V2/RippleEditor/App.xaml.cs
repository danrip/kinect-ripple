using System.Windows;
using System.Windows.Threading;
using RippleCommonUtilities;

namespace RippleEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Exit_1(object sender, ExitEventArgs e)
        {
            //Stop the logging session
            LoggingHelper.StopLogging();
        }

        private void Application_DispatcherUnhandledException_1(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            //Stop the logging session
            LoggingHelper.StopLogging();
            e.Handled = true;
        }
    }
}
