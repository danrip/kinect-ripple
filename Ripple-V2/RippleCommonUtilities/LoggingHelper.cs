using System;
using MicrosoftIT.ManagedLogging;

namespace RippleCommonUtilities
{
    public static class LoggingHelper
    {
        public static void StartLogging(String componentName, String fileLocation=null)
        {
            LogManager.StartLogging(componentName, fileLocation);
        }

        public static void StopLogging()
        {
            LogManager.StopLogging();
        }

        public static void LogTrace(int level, string formatString, params object[] varargs)
        {
            LogManager.LogTrace(level, formatString, varargs);
        }
    }
}
