using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Ripple.Common;

namespace RippleCommonUtilities.Messaging
{
    public static class NamedPipeServer
    {
        private const int BufferSize = 256;
        public static string PipeName;
        private static NamedPipeServerStream _pipeServer;
        private static Window _owner;

        public static void StartReceivingMessages(Window currentInstance, string pipeName)
        {
            PipeName = pipeName;
            _owner = currentInstance;

            var pipeThread = new ThreadStart(CreatePipeServer);
            var listenerThread = new Thread(pipeThread);

            listenerThread.SetApartmentState(ApartmentState.STA);
            listenerThread.IsBackground = true;

            listenerThread.Start();
        }

        public static void CreatePipeServer()
        {
            var decoder = Encoding.Default.GetDecoder();
            var bytes = new Byte[BufferSize];
            var chars = new char[BufferSize];
            var msg = new StringBuilder();

            try
            {
                _pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);

                while (true)
                {
                    _pipeServer.WaitForConnection();

                    var numBytes = 0;
                    do
                    {
                        msg.Length = 0;
                        do
                        {
                            numBytes = _pipeServer.Read(bytes, 0, BufferSize);
                            if (numBytes > 0)
                            {
                                var numChars = decoder.GetCharCount(bytes, 0, numBytes);
                                decoder.GetChars(bytes, 0, numBytes, chars, 0, false);
                                msg.Append(chars, 0, numChars);
                            }
                        } while (numBytes > 0 && !_pipeServer.IsMessageComplete);

                        decoder.Reset();
                        if (numBytes > 0 && _owner != null)
                        {
                            //_owner.Dispatcher.Invoke(DispatcherPriority.Send, new Action<string>(_owner.OnMessageReceived), msg.ToString());
                        }
                    } while (numBytes != 0);
                    _pipeServer.Disconnect();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static void SendMessage(String optionVal)
        {
            using (var pipeClient = new NamedPipeClientStream(".", "RippleReversePipe", PipeDirection.Out, PipeOptions.Asynchronous))
            {
                try
                {
                    pipeClient.Connect(2000);
                }
                catch (Exception)
                {
                    //Try once more
                    try
                    {
                        pipeClient.Connect(5000);
                    }
                    catch (Exception ex)
                    {
                        LoggingHelper.LogTrace(1, "Went wrong in Send Message {1} at Screen side {0}", ex.Message, optionVal);
                        return;
                    }
                }
                //Connected to the server or floor application
                using (var sw = new StreamWriter(pipeClient))
                {
                    sw.Write(optionVal);
                }
            }
        }

    }
}
