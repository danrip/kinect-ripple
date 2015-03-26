﻿using System;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.ComponentModel;
using RippleCommonUtilities;

namespace RippleScreenApp.Utilities
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [ComVisible(true)]
    public class ScriptingHelper : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }

        private String sendMessage = String.Empty;
        public String SendMessage
        {
            get { return sendMessage; }
            set
            {
                sendMessage = value;
                OnPropertyChanged(new PropertyChangedEventArgs("SendMessage"));
            }
        }

        ScreenWindow mExternalWPF;
        
        public ScriptingHelper(ScreenWindow w)
        {
            mExternalWPF = w;
        }

        public void MessageReceived(String messageParam)
        {
            try
            {
                mExternalWPF.browserElement.Document.InvokeScript("executeCommandFromFloor", new Object[]{messageParam});
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in MessageReceived of scripting Helper for Screen {0} for message {1}", ex.Message, messageParam);
            }
        }

        public void executeCommand(String commandText, String commandParameters)
        {
            try
            {
                var parameters = commandParameters.Split(new Char[] { ',' });
                var commandExecuted = false;
                switch (commandText)
                {
                    case "sendCommandToBottomFloor":
                        SendMessage = commandParameters;
                        commandExecuted = true;
                        break;
                    case "logMessage":
                        LoggingHelper.LogTrace(1, "Log Message from HTML {0}", commandParameters);
                        commandExecuted = true;
                        break;
                    case "saveFeedback":
                        QuizAnswersWriter.CommitQuizAnswersAsync(commandParameters, ScreenWindow.personName, ScreenWindow.rippleData.Floor.SetupID);
                        commandExecuted = true;
                        break;
                    case "GetSessionID":
                        mExternalWPF.browserElement.Document.InvokeScript("executeCommandFromFloor", new Object[] { ScreenWindow.sessionGuid });
                        commandExecuted = true;
                        break;
                    case "PrintDiscount":
                        //Get the values
                        if(!String.IsNullOrEmpty(commandParameters))
                        {
                            var valArray = commandParameters.Split(',');
                            PrinterHelper.PrintDiscountCoupon(valArray[0], valArray[1], valArray[2]);
                        }
                        break;
                    default:
                        break;
                }
                if (!commandExecuted)
                {
                    LoggingHelper.LogTrace(1, "Command {0} with Parameters {1} not Supported in Screen", commandText, commandParameters);
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in executeCommand for Scripting helper {0}", ex.Message);
            }

        }

    }

}
