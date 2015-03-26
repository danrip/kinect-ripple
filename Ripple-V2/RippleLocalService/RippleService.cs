﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.ServiceProcess;
using System.Timers;
using System.Xml.Serialization;
using MicrosoftIT.ManagedLogging;
using RippleLocalService.Utilities;

namespace RippleLocalService
{
    public partial class RippleService : ServiceBase
    {
        private Timer iRippleWindowsServiceTimer = null;
        private Timer iRippleWindowsServiceTimer2 = null;
        private static List<String> fileListToBeDeleted = new List<string>();

        private static String TelemetryFilePath
        {
            get { return Path.Combine(Path.GetTempPath(), "Ripple", "RippleTelemetryData.xml"); }
        }

        private static String QuizAnswersFilePath
        {
            get { return Path.Combine(Path.GetTempPath(), "Ripple", "RippleQuizAnswersData.xml"); }
        }

        //Update period for Telemetry and emailing log files
        private static int UpdateIntervalInMinutesForTelemetry = Convert.ToInt16(ConfigurationManager.AppSettings["UpdateIntervalInMinutesForTelemetry"]);
        //Update period for Quiz Update
        private static int UpdateIntervalInMinutesForQuiz = Convert.ToInt16(ConfigurationManager.AppSettings["UpdateIntervalInMinutesForQuiz"]);
 

        //Telemetry and Feedback variables
        private static String TargetTableName = ConfigurationManager.AppSettings["TelemetryTargetTableName"];
        private static String FeedbackTargetTableName = ConfigurationManager.AppSettings["FeedbackTargetTableName"];
        private static String TargetDatabaseName = ConfigurationManager.AppSettings["TargetDatabaseName"];
        private static String TargetServerName = ConfigurationManager.AppSettings["TargetServerName"];

        //Email Variables
        private static String SMTPServerName = ConfigurationManager.AppSettings["SMTPServer"];
        private static String EmailTo = ConfigurationManager.AppSettings["EmailTo"];
        private static String EmailFrom = ConfigurationManager.AppSettings["EmailFrom"];
        private static String EmailSubject = ConfigurationManager.AppSettings["EmailSubject"];
        private static String EmailBody = ConfigurationManager.AppSettings["EmailBody"];


        public RippleService()
        {
            InitializeComponent();

           
        }

        protected override void OnStart(string[] args)
        {
            //Initialize Timer to work for Telemetry and logs
            iRippleWindowsServiceTimer = new Timer(UpdateIntervalInMinutesForTelemetry * 60 * 1000);
            iRippleWindowsServiceTimer.Elapsed += new ElapsedEventHandler(RippleWindowsServiceTimer_Tick);
            iRippleWindowsServiceTimer.Enabled = true;

            //Initialize Timer to work for Quiz Data Update
            iRippleWindowsServiceTimer2 = new Timer(UpdateIntervalInMinutesForQuiz * 60 * 1000);
            iRippleWindowsServiceTimer2.Elapsed += new ElapsedEventHandler(RippleWindowsServiceTimer2_Tick);
            iRippleWindowsServiceTimer2.Enabled = true;

            //Initialize event logging
            LogManager.StartLogging("RippleLocalService");

            LogManager.LogTrace(1, "Ripple Local Service Started");

        }

        protected override void OnStop()
        {
            LogManager.StopLogging();
        }

        private void RippleWindowsServiceTimer_Tick(object sender, ElapsedEventArgs e)
        {
            try
            {
                iRippleWindowsServiceTimer.Enabled = false;
                //iRippleWindowsServiceTimer2.Enabled = false;

                //Log tick event
                LogManager.LogTrace(1, "Tick Event for Telemetry in Service {0}", DateTime.Now);

                //Work
                //Telemetry
                UpdateTelemetry();
                //Logging files
                EmailETLFiles();
                DeleteETLFiles();

                iRippleWindowsServiceTimer.Enabled = true;
                //iRippleWindowsServiceTimer2.Enabled = true;
            }
            catch (Exception ex)
            {
                LogManager.LogTrace(1, "Went wrong in Ripple Service Tick Event for Telemetry {0}", ex.Message);
                iRippleWindowsServiceTimer.Enabled = true;
                //iRippleWindowsServiceTimer2.Enabled = true;
            }
        }

        private void RippleWindowsServiceTimer2_Tick(object sender, ElapsedEventArgs e)
        {
            try
            {
                //iRippleWindowsServiceTimer.Enabled = false;
                iRippleWindowsServiceTimer2.Enabled = false;

                //Log tick event
                LogManager.LogTrace(1, "Tick Event for Quiz in Service {0}", DateTime.Now);

                //Work
                //Quiz answers
                UpdateQuizAnswers();

                //iRippleWindowsServiceTimer.Enabled = true;
                iRippleWindowsServiceTimer2.Enabled = true;
            }
            catch (Exception ex)
            {
                LogManager.LogTrace(1, "Went wrong in Ripple Service Tick Event for Quiz {0}", ex.Message);
                //iRippleWindowsServiceTimer.Enabled = true;
                iRippleWindowsServiceTimer2.Enabled = true;
            }
        }

        private void DeleteETLFiles()
        {
            try
            {
                if (fileListToBeDeleted == null || fileListToBeDeleted.Count == 0)
                    return;
                //Delete all the files
                foreach (var t in fileListToBeDeleted)
                {
                    try
                    {
                        File.Delete(t);
                        fileListToBeDeleted.Remove(t);
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogTrace(1, "Went wrong in Delete file {0}", ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogTrace(1, "Went wrong in deleting the ETL files {0}", ex.Message);
            }
        }

        private void EmailETLFiles()
        {
            try
            {
                var message = new EmailSender(SMTPServerName, EmailTo, EmailFrom, EmailSubject);
                var fileList = Directory.EnumerateFiles(Path.Combine(Path.GetTempPath(), "Ripple"), "*.etl", SearchOption.TopDirectoryOnly);
                foreach (var t in fileList)
                {
                    try
                    {
                        message.addAttachments(t);
                        if(!fileListToBeDeleted.Contains(t))
                            fileListToBeDeleted.Add(t);
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogTrace(1, "Went wrong in Emailing ETL files {0}", ex.Message);
                    }
                }
                message.sendmail(EmailBody);
                message.Dispose();
            }
            catch (Exception ex)
            {
                LogManager.LogTrace(1,"Went wrong in sending the ETL files {0}", ex.Message);
            }
        }

        private void UpdateTelemetry()
        {
            var telemetryData = new DataSet();
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

                    //Insert in the Database
                    using (var sqlConn = new SqlConnection(GetConnectionString()))
                    {
                        sqlConn.Open();
                        //Insert the new data
                        using (var bulkCopy = new SqlBulkCopy(sqlConn))
                        {
                            bulkCopy.DestinationTableName = TargetTableName;

                            bulkCopy.WriteToServer(telemetryData.Tables[0]);
                        }
                        sqlConn.Close();

                        //Successfull, hence delete the file
                        File.Delete(TelemetryFilePath);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                reader = null;
                if (telemetryFile != null)
                {
                    telemetryFile.Close();
                    telemetryFile.Dispose();
                }
                LogManager.LogTrace(1,"Went wrong in uploading the telemetry data to teh database {0}", ex.Message);
            }
        }

        private void UpdateQuizAnswers()
        {
            var quiAnswersData = new DataSet();
            XmlSerializer reader;
            StreamReader quizAnswersFile = null;
            try
            {
                if (File.Exists(QuizAnswersFilePath))
                {
                    reader = new XmlSerializer(typeof(DataSet));
                    quizAnswersFile = new StreamReader(QuizAnswersFilePath);
                    quiAnswersData = (DataSet)reader.Deserialize(quizAnswersFile);
                    quizAnswersFile.Close();
                    quizAnswersFile.Dispose();

                    //Insert in the Database
                    using (var sqlConn = new SqlConnection(GetConnectionString()))
                    {
                        sqlConn.Open();
                        //Insert the new data
                        using (var bulkCopy = new SqlBulkCopy(sqlConn))
                        {
                            bulkCopy.DestinationTableName = FeedbackTargetTableName;

                            bulkCopy.WriteToServer(quiAnswersData.Tables[0]);
                        }
                        sqlConn.Close();

                        //Successfull, hence delete the file
                        File.Delete(QuizAnswersFilePath);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                reader = null;
                if (quizAnswersFile != null)
                {
                    quizAnswersFile.Close();
                    quizAnswersFile.Dispose();
                }
                LogManager.LogTrace(1, "Went wrong in uploading the telemetry data to the database {0}", ex.Message);
            }
        }

        private String GetConnectionString()
        {
            var builder = new SqlConnectionStringBuilder();
            builder.InitialCatalog = TargetDatabaseName;
            builder.DataSource = TargetServerName;
            builder.IntegratedSecurity = true;
            builder.ConnectTimeout = 12;

            return builder.ConnectionString;
        }
    }
}
