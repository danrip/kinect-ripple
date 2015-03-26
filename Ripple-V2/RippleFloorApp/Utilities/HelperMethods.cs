using System.IO;

namespace RippleFloorApp.Utilities
{
    public static class HelperMethods
    {
        public static string GetAssetUri(string content)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var result = Path.GetFullPath(currentDirectory + content);
            
            return result; 
        }
    }
}
