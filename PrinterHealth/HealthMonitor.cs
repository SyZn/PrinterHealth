using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PrinterHealth.Config;
using PrinterHealth.Model;
using RavuAlHemio.CentralizedLog;

namespace PrinterHealth
{
    public class HealthMonitor : IDisposable
    {
        private static readonly ILogger Logger = CentralizedLogger.Factory.CreateLogger<HealthMonitor>();

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
                Logger.LogDebug("updating {PrinterName}...", printer.Key);

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
                    Logger.LogWarning("exception thrown while updating/cleaning {PrinterName}: {Exception}", printer.Key, exc);
                }

                Logger.LogDebug("{PrinterName} updated", printer.Key);
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

                Logger.LogDebug("keeping {PrinterName} warm...", printer.Key);
                
                try
                {
                    keepWarmPrinter.KeepWarm();
                }
                catch (Exception exc)
                {
                    Logger.LogWarning("exception thrown while keeping {PrinterName} warm: {Exception}", printer.Key, exc);
                }

                Logger.LogDebug("{PrinterName} kept warm", printer.Key);
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
                Logger.LogError("keeping warm failed: {Exception}", exc);
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
                Logger.LogError("health monitoring failed: {Exception}", exc);
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
                    Logger.LogError(
                        "duplicate printer name {PrinterName}",
                        printer.Name
                    );
                    continue;
                }

                var assembly = Assembly.Load(new AssemblyName(printer.Assembly));
                if (assembly == null)
                {
                    Logger.LogError(
                        "assembly {Assembly} (for class {DeviceClass}) could not be loaded",
                        printer.Assembly,
                        printer.DeviceClass
                    );
                    continue;
                }

                var type = assembly.GetType(printer.DeviceClass);
                if (type == null)
                {
                    Logger.LogError(
                        "class {DeviceClass} not found in assembly {Assembly}",
                        printer.DeviceClass,
                        printer.Assembly
                    );
                    continue;
                }

                var constructor = type.GetConstructor(new [] {typeof(JObject)});
                if (constructor == null)
                {
                    Logger.LogError(
                        "constructor with JObject parameter not found in class {DeviceClass} in assembly {Assembly}",
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
