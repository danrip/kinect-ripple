using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Speech.Synthesis;
using RippleCommonUtilities;

namespace RippleFloorApp.Utilities
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [ComVisible(true)]
    public class ScriptingHelper : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        private readonly FloorWindow _externalFloorWindow;
        private bool _systemUnlocked;
        private bool _exitGame;
        private bool _exitOnStart;
        private string _sendMessage = String.Empty;

        public ScriptingHelper(FloorWindow externalFloorWindow)
        {
            _externalFloorWindow = externalFloorWindow;
        }

        public bool SystemUnlocked
        {
            get { return _systemUnlocked; }
            set
            {
                _systemUnlocked = value;
                OnPropertyChanged(new PropertyChangedEventArgs("SystemUnlocked"));
            }
        }

        public bool ExitGame
        {
            get { return _exitGame; }
            set
            {
                _exitGame = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ExitGame"));
            }
        }

        public bool ExitOnStart
        {
            get { return _exitOnStart; }
            set
            {
                _exitOnStart = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ExitOnStart"));
            }
        }

        public string SendMessage
        {
            get { return _sendMessage; }
            set
            {
                _sendMessage = value;
                OnPropertyChanged(new PropertyChangedEventArgs("SendMessage"));
            }
        }

        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }

        public void MessageReceived(String messageParam)
        {
            try
            {
                _externalFloorWindow.BrowserElement.Document.InvokeScript("executeCommandFromScreen", new Object[] {messageParam});
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in MessageReceived of scripting Helper for Floor {0}", ex.Message);
            }
        }

        public void GestureReceived(GestureTypes ges)
        {
            try
            {
               _externalFloorWindow.BrowserElement.Document.InvokeScript("gestureReceived", new object[] {ges.ToString()});
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in GestureReceived {0}", ex.Message);
            }
        }

        public void ExecuteCommand(String commandText, String commandParameters)
        {
            try
            {
                var parameters = commandParameters.Split(new[] {','});
                var commandExecuted = false;
                
                _exitGame = false;
                _exitOnStart = false;
                
                switch (commandText)
                {
                    case "unlockSystem":
                        if (parameters[0] == String.Empty && parameters.Length == 1)
                        {
                            SystemUnlocked = true;
                            commandExecuted = true;
                        }
                        break;
                    case "exitGame":
                        //Raise it to the floor
                        ExitGame = true;
                        commandExecuted = true;
                        break;
                    case "exitOnStart":
                        //Raise it to the floor
                        ExitOnStart = true;
                        commandExecuted = true;
                        break;
                    case "playAudio":
                        PlayAudio(commandParameters);
                        commandExecuted = true;
                        break;
                    case "sendCommandToFrontScreen":
                        SendMessage = "HTML:" + commandParameters;
                        commandExecuted = true;
                        break;
                    case "sendCommandForScreenProcessing":
                        SendMessage = commandParameters;
                        commandExecuted = true;
                        break;
                    case "logMessage":
                        LoggingHelper.LogTrace(1, "Log Message from HTML {0}", commandParameters);
                        commandExecuted = true;
                        break;
                }
                if (!commandExecuted)
                {
                    LoggingHelper.LogTrace(1, "Command {0} with Parameters {1} not Supported", commandText, commandParameters);
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in executeCommand for Scripting helper {0}", ex.Message);
            }
        }

        private void PlayAudio(string commandParameters)
        {
            using (var bg = new BackgroundWorker())
            {
                bg.DoWork += bg_DoWork;
                bg.RunWorkerAsync(commandParameters);
            }
        }

        private void bg_DoWork(object sender, DoWorkEventArgs e)
        {
            using (var synClass = new SpeechSynthesizer())
            {
                synClass.SetOutputToDefaultAudioDevice();
                synClass.Volume = 100;
                synClass.Speak(Convert.ToString(e.Argument));
            }
        }
    }
}