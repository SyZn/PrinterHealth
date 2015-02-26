using System.ServiceProcess;
using PrinterHealth;
using PrinterHealth.Config;
using PrinterHealthWeb;

namespace PrinterHealthWebService
{
    public partial class PrinterHealthWebService : ServiceBase
    {
        private PrinterHealthConfig _config;
        private HealthMonitor _monitor;
        private HttpListenerResponder _responder;

        public PrinterHealthWebService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _config = PrinterHealthUtils.LoadConfig();
            PrinterHealthUtils.SetupLogging();

            // start the health monitor
            _monitor = new HealthMonitor(_config);
            // (starts automatically)

            // start the HTTP responder
            _responder = new HttpListenerResponder(_monitor, _config.ListenPort);
            _responder.Start();
        }

        protected override void OnStop()
        {
            _responder.Stop();
            _monitor.Dispose();
        }
    }
}
