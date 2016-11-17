using DasMulli.Win32.ServiceUtils;

namespace PrinterHealthWebService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            var service = new PrinterHealthWebService();
            var serviceHost = new Win32ServiceHost(service);
            serviceHost.Run();
        }
    }
}
