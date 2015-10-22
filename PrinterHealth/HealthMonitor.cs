using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using log4net;
using Newtonsoft.Json.Linq;
using PrinterHealth.Config;
using PrinterHealth.Model;

namespace PrinterHealth
{
    public class HealthMonitor : IDisposable
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private bool _disposed = false;
        private readonly object _interlock = new object();
        private readonly Timer _timer;
        private Timer _keepWarmTimer;
        private readonly TimeSpan _keepWarmSpan;
        private readonly SortedDictionary<string, IPrinter> _printers;

        public IDictionary<string, IPrinter> Printers
        {
            get { return new Dictionary<string, IPrinter>(_printers); }
        }

        protected virtual void ActuallyPerform()
        {
            foreach (var printer in _printers)
            {
                Logger.DebugFormat("updating {0}...", printer.Key);

                try
                {
                    printer.Value.Update();

                    var cleanupPrinter = printer.Value as IPrinterWithJobCleanup;
                    if (cleanupPrinter != null)
                    {
                        cleanupPrinter.CleanupBrokenJobs();
                    }
                }
                catch (Exception exc)
                {
                    Logger.WarnFormat("exception thrown while updating/cleaning {0}: {1}", printer.Key, exc);
                }

                Logger.DebugFormat("{0} updated", printer.Key);
            }
        }

        public virtual void KeepWarmActuallyPerform()
        {
            foreach (var printer in _printers)
            {
                var keepWarmPrinter = printer.Value as IPrinterToKeepWarm;
                if (keepWarmPrinter == null)
                {
                    continue;
                }

                Logger.DebugFormat("keeping {0} warm...", printer.Key);
                
                try
                {
                    keepWarmPrinter.KeepWarm();
                }
                catch (Exception exc)
                {
                    Logger.WarnFormat("exception thrown while keeping {0} warm: {1}", printer.Key, exc);
                }

                Logger.DebugFormat("{0} kept warm", printer.Key);
            }
        }

        protected virtual void KeepWarmPerform(object state)
        {
            try
            {
                KeepWarmActuallyPerform();
            }
            catch (Exception exc)
            {
                Logger.ErrorFormat("keeping warm failed: {0}", exc);
            }
        }

        protected virtual void Perform(object state)
        {
            if (!Monitor.TryEnter(_interlock))
            {
                // the previous iteration is still running
                return;
            }
            try
            {
                ActuallyPerform();
            }
            catch (Exception exc)
            {
                Logger.ErrorFormat("health monitoring failed: {0}", exc);
            }
            finally
            {
                Monitor.Exit(_interlock);
            }
        }

        public HealthMonitor(PrinterHealthConfig config)
        {
            _printers = new SortedDictionary<string, IPrinter>();

            // load the printer modules
            foreach (var printer in config.Printers)
            {
                if (_printers.ContainsKey(printer.Name))
                {
                    Logger.ErrorFormat(
                        "duplicate printer name {0}",
                        printer.Name
                    );
                    continue;
                }

                var assembly = Assembly.Load(printer.Assembly);
                if (assembly == null)
                {
                    Logger.ErrorFormat(
                        "assembly {1} (for class {0}) could not be loaded",
                        printer.DeviceClass,
                        printer.Assembly
                    );
                    continue;
                }

                var type = assembly.GetType(printer.DeviceClass);
                if (type == null)
                {
                    Logger.ErrorFormat(
                        "class {0} not found in assembly {1}",
                        printer.DeviceClass,
                        printer.Assembly
                    );
                    continue;
                }

                var constructor = type.GetConstructor(new [] {typeof(JObject)});
                if (constructor == null)
                {
                    Logger.ErrorFormat(
                        "constructor with JObject parameter not found in class {0} in assembly {1}",
                        printer.DeviceClass,
                        printer.Assembly
                    );
                    continue;
                }

                var printerObject = (IPrinter) constructor.Invoke(new object[] {printer.Options});

                _printers[printer.Name] = printerObject;
            }

            _timer = new Timer(Perform, null, TimeSpan.Zero, TimeSpan.FromMinutes(config.UpdateIntervalMinutes));
            _keepWarmTimer = null;
            _keepWarmSpan = TimeSpan.FromMinutes(config.KeepWarmIntervalMinutes);
        }

        public void StartKeepingWarm()
        {
            if (_keepWarmTimer != null)
            {
                _keepWarmTimer = new Timer(KeepWarmPerform, null, TimeSpan.Zero, _keepWarmSpan);
            }
        }

        public void StopKeepingWarm()
        {
            _keepWarmTimer.Dispose();
            _keepWarmTimer = null;
        }

        #region disposal logic
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // dispose managed objects
                _timer.Dispose();
                _keepWarmTimer?.Dispose();
            }

            // dispose unmanaged objects

            _disposed = true;
        }

        ~HealthMonitor()
        {
            Dispose(false);
        }
        #endregion
    }
}
