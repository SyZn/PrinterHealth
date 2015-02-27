﻿using System;
using log4net;
using log4net.Appender;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using PrinterHealth;
using PrinterHealthWeb;

namespace PrinterHealthWebCLI
{
    class Program
    {
        private static void SetupConsoleLogging()
        {
            var layout = new PatternLayout
            {
                ConversionPattern = "%date{yyyy-MM-dd HH:mm:ss} [%15.15thread] %-5level %30.30logger - %message%newline"
            };
            layout.ActivateOptions();

            var conApp = new ConsoleAppender
            {
                Layout = layout
            };
            conApp.ActivateOptions();

            var rootLogger = ((Hierarchy)LogManager.GetRepository()).Root;
            rootLogger.AddAppender(conApp);
        }

        static void Main(string[] args)
        {
            // load config
            var config = PrinterHealthUtils.LoadConfig();

            // setup logging
            PrinterHealthUtils.SetupLogging();
            SetupConsoleLogging();

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