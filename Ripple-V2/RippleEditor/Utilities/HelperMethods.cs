using System;
using System.IO;
using System.Reflection;
using Microsoft.VisualBasic.FileIO;
using RippleCommonUtilities;
using RippleDictionary;

namespace RippleEditor.Utilities
{
    public static class HelperMethods
    {
        public static String CurrentDirectory
        {
            get { return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location); }
        }

        public static String DefaultAssetsDirectory
        {
            get { return CurrentDirectory + "\\Default Assets"; }
        }

        public static String TargetAssetsDirectory
        {
            get { return CurrentDirectory + "\\..\\Assets"; }
        }

        public static String TargetAssetsRoot
        {
            get { return CurrentDirectory + "\\.."; }
        }

        /// <summary>
        /// Returns the XML path for the current selected template
        /// </summary>
        /// <param name="currentTemplate"></param>
        /// <returns></returns>
        public static String GetXMLFileForTemplate(TemplateOptions currentTemplate)
        {
            switch (currentTemplate)
            {
                case TemplateOptions.Template_2X2:
                    return Environment.CurrentDirectory + "\\SampleTemplates\\Template2X2.xml";
                case TemplateOptions.Template_2X3:
                    return Environment.CurrentDirectory + "\\SampleTemplates\\Template2X3.xml";
                case TemplateOptions.Template_3X2:
                    return Environment.CurrentDirectory + "\\SampleTemplates\\Template3X2.xml";
                case TemplateOptions.Template_3X3:
                    return Environment.CurrentDirectory + "\\SampleTemplates\\Template3X3.xml";
                case TemplateOptions.Template_Random_1:
                    return Environment.CurrentDirectory + "\\SampleTemplates\\TemplateMerge1.xml";
                default:
                    return String.Empty;
                  
            }
        }

        /// <summary>
        /// Creates the required file "sourceFile" in targetFolder with same name
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="targetFolder"></param>
        /// <returns></returns>
        public static String CopyFile(String sourceFile, String targetFolder)
        {
            var targetFileName = String.Empty;
            try
            {
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }
                targetFileName = targetFolder + "\\" + Path.GetFileName(sourceFile);
                File.Copy(sourceFile, targetFileName, false);
                return targetFileName;
            }
            catch (IOException ex)
            {
                if (ex.Message.Contains("already exists"))
                    return targetFileName;
                else
                    return String.Empty;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in CopyFile({0},  {1}) {2}", sourceFile, targetFolder, ex.Message);
                return String.Empty;
            }
        }

        /// <summary>
        /// Copies all the files and folders inside sourceFolder to the targetFolder
        /// </summary>
        /// <param name="sourceFolder"></param>
        /// <param name="targetFolder"></param>
        /// <returns></returns>
        public static String CopyFolder(String sourceFolder, String targetFolder)
        {
            try
            {
                targetFolder = targetFolder + sourceFolder.Substring(sourceFolder.LastIndexOf("\\"));
                FileSystem.CopyDirectory(sourceFolder, targetFolder, true);
                //System.IO.Directory.
                //Process proc = new Process();
                //proc.StartInfo.UseShellExecute = false;
                //proc.StartInfo.CreateNoWindow = true;
                //proc.StartInfo.FileName = @"xcopy.exe";
                //proc.StartInfo.Arguments = "\"" + sourceFolder + "\" \"" + targetFolder + "\" /E /I /Y";
                //proc.Start();
                return targetFolder;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in CopyFolder({0}, {1}) : {2}", sourceFolder, targetFolder, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Gets the required tile object for the Tile ID
        /// </summary>
        /// <param name="TileID"></param>
        /// <returns></returns>
        public static Tile GetFloorTileForID(string TileID)
        {
            Tile reqdTile = null;
            try
            {
                reqdTile = MainPage.rippleData.Floor.Tiles[TileID];
            }
            catch (Exception)
            {
                try
                {
                    reqdTile = MainPage.rippleData.Floor.Tiles[TileID.Substring(0, TileID.LastIndexOf("SubTile"))].SubTiles[TileID];
                }
                catch (Exception)
                {
                    return null;
                }
            }
            return reqdTile;
        }
    }
}
