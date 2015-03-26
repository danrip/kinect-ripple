using System.IO;
using System.Reflection;
using RippleCommonUtilities;
using System;
using System.Windows;

namespace RippleScreenApp.Utilities
{
    public static class Helper
    {
        public static void ClickOnScreenToGetFocus()
        {
            var middleWidth = (int)SystemParameters.PrimaryScreenWidth + Convert.ToInt32(Math.Floor((double)((int)Globals.CurrentResolution.HorizontalResolution / 2)));
            var middleHeight = (int)SystemParameters.PrimaryScreenHeight + Convert.ToInt32(Math.Floor((double)((int)Globals.CurrentResolution.VerticalResolution / 2)));
            //RippleCommonUtilities.LoggingHelper.LogTrace(1, "Click on the screen middleW: {0} middleH: {1} width: {2} height: {3}", middleWidth, middleHeight, Globals.CurrentResolution.HorizontalResolution, Globals.CurrentResolution.VerticalResolution);
            OSNativeMethods.SendMouseInput(middleWidth, middleHeight, (int)Globals.CurrentResolution.HorizontalResolution, (int)Globals.CurrentResolution.VerticalResolution, true);
            OSNativeMethods.SendMouseInput(middleWidth, middleHeight, (int)Globals.CurrentResolution.HorizontalResolution, (int)Globals.CurrentResolution.VerticalResolution, false);
        }

        public static string GetAssetURI(string Content)
        {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\.." + Content;
        }
    }
}
