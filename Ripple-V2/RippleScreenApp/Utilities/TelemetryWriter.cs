using System;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Xml.Serialization;
using RippleCommonUtilities;

namespace RippleScreenApp.Utilities
{
    public static class TelemetryWriter
    {
        internal static DataSet telemetryData;

        private static DateTime _currentTime;
        private static string _setupId;
        private static string _personName;
        private static string _tileName;
        private static string _tileId;
        private static string _option;
        private static DateTime _oldTime;

        private static string TelemetryFilePath
        {
            get { return Path.Combine(Path.GetTempPath(), "Ripple", "RippleTelemetryData.xml"); }
        }

        private delegate void CommitTelemetryDelegate();
        public static void CommitTelemetryAsync()
        {
            var asyncDelegate = new CommitTelemetryDelegate(CommitTelemetry);
            var operation = AsyncOperationManager.CreateOperation(null);
            asyncDelegate.BeginInvoke(null, operation);
        }
       
        public static void CommitTelemetry()
        {
            XmlSerializer writer;
            StreamWriter telemetryFile = null;
            try
            {
                if (telemetryData == null)
                    return;

                writer = new XmlSerializer(typeof(DataSet));
                telemetryFile = new StreamWriter(TelemetryFilePath);
                writer.Serialize(telemetryFile, telemetryData);
                telemetryFile.Close();
                telemetryFile.Dispose();
                telemetryData = null;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in CommitTelemetry at Screen side {0}", ex.Message);
                writer = null;
                if (telemetryFile != null)
                {
                    telemetryFile.Close();
                    telemetryFile.Dispose();
                }
                telemetryData = null;
            }
        }

        public static void RetrieveTelemetryData()
        {
            XmlSerializer reader;
            StreamReader telemetryFile = null;
            try
            {
                if (File.Exists(TelemetryFilePath))
                {
                    reader = new XmlSerializer(typeof(DataSet));
                    telemetryFile = new StreamReader(TelemetryFilePath);
                    telemetryData = (DataSet)reader.Deserialize(telemetryFile);
                    telemetryFile.Close();
                    telemetryFile.Dispose();
                }
                else
                {
                    telemetryData = new DataSet();
                    //Initialize the Dataset and return it
                    var dt = new DataTable();
                    dt.Columns.Add(new DataColumn("SetupID", typeof(String)));
                    dt.Columns.Add(new DataColumn("PersonID", typeof(String)));
                    dt.Columns.Add(new DataColumn("TileName", typeof(String)));
                    dt.Columns.Add(new DataColumn("TileID", typeof(String)));
                    dt.Columns.Add(new DataColumn("OptionType", typeof(String)));
                    dt.Columns.Add(new DataColumn("StartTime", typeof(DateTime)));
                    dt.Columns.Add(new DataColumn("EndTime", typeof(DateTime)));
                    telemetryData.Tables.Add(dt);
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in RetrieveTelemetryData at Screen side {0}", ex.Message);
                reader = null;
                if (telemetryFile != null)
                {
                    telemetryFile.Close();
                    telemetryFile.Dispose();
                }
            }
        }

        public static void AddTelemetryRow(String setupID, String personName, String TileName, String TileID, String Option)
        {
            if(telemetryData != null)
            {
                _currentTime = DateTime.Now;
                telemetryData.Tables[0].Rows.Add(setupID, personName, TileName, TileID, Option, _currentTime, _currentTime);
            }
        }

        public static void UpdatePreviousEntry()
        {
            if (telemetryData != null && telemetryData.Tables[0].Rows.Count >= 1)
            {
                //Get the previous entry
                _setupId = Convert.ToString(telemetryData.Tables[0].Rows[telemetryData.Tables[0].Rows.Count - 1].ItemArray[0]);
                _personName = Convert.ToString(telemetryData.Tables[0].Rows[telemetryData.Tables[0].Rows.Count - 1].ItemArray[1]);
                _tileName = Convert.ToString(telemetryData.Tables[0].Rows[telemetryData.Tables[0].Rows.Count - 1].ItemArray[2]);
                _tileId = Convert.ToString(telemetryData.Tables[0].Rows[telemetryData.Tables[0].Rows.Count - 1].ItemArray[3]);
                _option = Convert.ToString(telemetryData.Tables[0].Rows[telemetryData.Tables[0].Rows.Count - 1].ItemArray[4]);
                _oldTime = Convert.ToDateTime(telemetryData.Tables[0].Rows[telemetryData.Tables[0].Rows.Count - 1].ItemArray[5]);
                _currentTime = DateTime.Now;
                telemetryData.Tables[0].Rows[telemetryData.Tables[0].Rows.Count - 1].ItemArray = new object[] { _setupId, _personName, _tileName, _tileId, _option, _oldTime, _currentTime};
            }
        }
    }
}
