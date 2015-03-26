using System;
using System.IO;
using System.Reflection;
using System.Text;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using Newtonsoft.Json.Linq;
using PrinterHealth.Config;

namespace PrinterHealth
{
    public static class PrinterHealthUtils
    {
        public static Encoding Utf8NoBom = new UTF8Encoding(false, true);

        public static string ProgramDirectory
        {
            get
            {
                var localPath = (new Uri(Assembly.GetExecutingAssembly().CodeBase)).LocalPath;
                return Path.GetDirectoryName(localPath);
            }
        }

        public static PrinterHealthConfig LoadConfig()
        {
            var configBody = File.ReadAllText(Path.Combine(ProgramDirectory, "Config.json"), Utf8NoBom);
            var jo = JObject.Parse(configBody);
            var config = new PrinterHealthConfig();
            config.LoadFromJson(jo);
            return config;
        }

        /// <summary>
        /// Sets up logging from a configuration file or chooses some sane defaults.
        /// </summary>
        public static void SetupLogging()
        {
            var confFile = new FileInfo(Path.Combine(ProgramDirectory, "Logging.conf"));
            if (confFile.Exists)
            {
                XmlConfigurator.Configure(confFile);
            }
            else
            {
                var rootLogger = ((Hierarchy)LogManager.GetRepository()).Root;
                rootLogger.Level = Level.Debug;
                LogManager.GetRepository().Configured = true;

                // log to a file
                var layout = new PatternLayout
                {
                    ConversionPattern = "%date{yyyy-MM-dd HH:mm:ss} [%15.15thread] %-5level %30.30logger - %message%newline"
                };
                layout.ActivateOptions();

                var fileLogAppender = new FileAppender
                {
                    Layout = layout,
                    File = Path.Combine(ProgramDirectory, "Log.txt"),
                    AppendToFile = true
                };
                fileLogAppender.ActivateOptions();

                rootLogger.AddAppender(fileLogAppender);
            }
        }
    }
}
