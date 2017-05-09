using System;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PrinterHealth.Config;
using RavuAlHemio.CentralizedLog;

namespace PrinterHealth
{
    public static class PrinterHealthUtils
    {
        private static readonly ILogger Logger = CentralizedLogger.Factory.CreateLogger(typeof(PrinterHealthUtils));

        public static Encoding Utf8NoBom = new UTF8Encoding(false, true);

        public static string ProgramDirectory => AppContext.BaseDirectory;

        public const int LPDPortNumber = 515;

        public static PrinterHealthConfig LoadConfig()
        {
            var configBody = File.ReadAllText(Path.Combine(ProgramDirectory, "Config.json"), Utf8NoBom);
            var jo = JObject.Parse(configBody);
            var config = new PrinterHealthConfig();
            config.LoadFromJson(jo);
            return config;
        }

        /// <summary>
        /// A certificate validation callback that validates nothing.
        /// </summary>
        public static bool NoCertificateValidationCallback(HttpRequestMessage request, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors policyErrors)
        {
            return true;
        }

        public static HttpClientHandler WithoutCertificateVerification(this HttpClientHandler handler)
        {
            handler.ServerCertificateCustomValidationCallback = NoCertificateValidationCallback;
            return handler;
        }

        public static T SyncWait<T>(this Task<T> task, int millisecondsTimeout = Timeout.Infinite, CancellationToken cancelToken = default(CancellationToken))
        {
            task.Wait(millisecondsTimeout, cancelToken);
            if (task.IsFaulted)
            {
                throw task.Exception;
            }
            if (task.IsCanceled)
            {
                throw new TaskCanceledException();
            }
            return task.Result;
        }
    }
}
