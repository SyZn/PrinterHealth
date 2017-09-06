using System;
using System.Threading;
using PrinterHealth;
using RavuAlHemio.CentralizedLog;

namespace KeepWarmCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            // setup logging
            CentralizedLogger.SetupConsoleLogging();

            // load config
            var config = PrinterHealthUtils.LoadConfig();

            // start the health monitor
            var healthMonitor = new HealthMonitor(config);
            // (starts automatically)
            healthMonitor.StartKeepingWarm();

            if (!Console.IsInputRedirected)
            {
                Console.WriteLine("Press Enter to trigger a warmup and Escape to exit.");
                for (;;)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape)
                    {
                        break;
                    }
                    if (key.Key == ConsoleKey.Enter)
                    {
                        try
                        {
                            healthMonitor.KeepWarmActuallyPerform();
                        }
                        catch (Exception exc)
                        {
                            Console.WriteLine("Exception!");
                            Console.WriteLine(exc);
                        }
                    }
                }
            }
            else
            {
                for (;;)
                {
                    Thread.Sleep(Timeout.InfiniteTimeSpan);
                }
            }

            healthMonitor.Dispose();
        }
    }
}
