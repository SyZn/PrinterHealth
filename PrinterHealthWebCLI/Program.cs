using System;
using PrinterHealth;
using PrinterHealthWeb;
using RavuAlHemio.CentralizedLog;

namespace PrinterHealthWebCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            CentralizedLogger.SetupConsoleLogging();
            CentralizedLogger.SetupFileLogging("PrinterHealthWeb");

            // load config
            var config = PrinterHealthUtils.LoadConfig();

            // start the health monitor
            var healthMonitor = new HealthMonitor(config);
            // (starts automatically)
            
            // start the HTTP responder
            var httpResponder = new HttpListenerResponder(config, healthMonitor);

            httpResponder.Start();

            Console.WriteLine("Press Enter or Escape to stop.");
            for (;;)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.Enter)
                {
                    break;
                }
            }

            httpResponder.Stop();
            healthMonitor.Dispose();
        }
    }
}
