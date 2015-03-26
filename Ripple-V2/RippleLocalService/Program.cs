using System.ServiceProcess;

namespace RippleLocalService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
            { 
                new RippleService() 
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
