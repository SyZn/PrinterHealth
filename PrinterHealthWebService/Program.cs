using System.ServiceProcess;

namespace PrinterHealthWebService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            var servicesToRun = new ServiceBase[] 
            { 
                new PrinterHealthWebService() 
            };
            ServiceBase.Run(servicesToRun);
        }
    }
}
