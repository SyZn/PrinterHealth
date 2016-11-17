using DasMulli.Win32.ServiceUtils;
using PrinterHealth;
using PrinterHealth.Config;
using PrinterHealthWeb;
using RavuAlHemio.CentralizedLog;

namespace PrinterHealthWebService
{
    public partial class PrinterHealthWebService : IWin32Service
    {
        private PrinterHealthConfig _config;
        private HealthMonitor _monitor;
        private HttpListenerResponder _responder;

        public string ServiceName => "PrinterHealthWebService";
        private const string ServiceDisplayName = "Printer Health Web";
        private const string ServiceDescription = "Monitors printers and consolidates their status information in a web frontend.";
        private const bool AutoStart = true;

        public PrinterHealthWebService()
        {
            var service = new PrinterHealthWebService();
        }

        public void Start(string[] args, ServiceStoppedCallback callbacks)
        {
            CentralizedLogger.SetupConsoleLogging();
            CentralizedLogger.SetupFileLogging("PrinterHealthWeb");

            _config = PrinterHealthUtils.LoadConfig();

            // start the health monitor
            _monitor = new HealthMonitor(_config);
            // (starts automatically)

            // start the HTTP responder
            _responder = new HttpListenerResponder(_config, _monitor);
            _responder.Start();
        }

        public void Stop()
        {
            _responder.Stop();
            _monitor.Dispose();
        }
    }
}
