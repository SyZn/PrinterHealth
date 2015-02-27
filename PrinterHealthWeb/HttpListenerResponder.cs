using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using DotLiquid;
using log4net;
using PrinterHealth;
using PrinterHealth.Config;
using PrinterHealth.Model;

namespace PrinterHealthWeb
{
    public class HttpListenerResponder
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected static readonly Regex AllowedStaticFilenameFormat = new Regex("^[a-zA-Z0-9-_]+[.][a-zA-Z0-9]+$");
        protected static readonly Dictionary<string, string> ExtensionsToMimeTypes = new Dictionary<string, string>
        {
            {"css", "text/css"},
            {"js", "text/javascript"},
            {"png", "image/png"},
            {"jpg", "image/jpeg"},
            {"jpeg", "image/jpeg"},
        };

        protected HttpListener Listener;
        private Thread _handlerThread;
        private readonly PrinterHealthConfig _config;
        private readonly HealthMonitor _monitor;

        public HttpListenerResponder(PrinterHealthConfig config, HealthMonitor monitor)
        {
            _config = config;
            _monitor = monitor;
            Listener = new HttpListener();
            Listener.Prefixes.Add(string.Format("http://+:{0}/", config.ListenPort));
        }

        protected static void SendPlainTextResponse(HttpListenerResponse resp, int statusCode, string statusDescription, string body)
        {
            resp.StatusCode = statusCode;
            resp.StatusDescription = statusDescription;
            resp.Headers[HttpResponseHeader.ContentType] = "text/plain; charset=utf-8";

            var bodyBytes = PrinterHealthUtils.Utf8NoBom.GetBytes(body);
            resp.ContentLength64 = bodyBytes.Length;
            resp.Close(bodyBytes, true);
        }

        protected static void Send404Response(HttpListenerResponse resp)
        {
            SendPlainTextResponse(resp, 404, "Not Found", "The requested file was not found.");
        }

        protected static void Send405Response(HttpListenerResponse resp, params string[] allowedMethods)
        {
            var allowedMethodsString = string.Join(", ", allowedMethods);
            resp.Headers[HttpResponseHeader.Allow] = allowedMethodsString;
            SendPlainTextResponse(resp, 405, "Method Not Allowed", "I can only be accessed using: " + allowedMethodsString);
        }

        protected static void SendOkResponse(HttpListenerResponse resp, string mimeType, byte[] body)
        {
            resp.StatusCode = 200;
            resp.StatusDescription = "OK";
            resp.Headers[HttpResponseHeader.ContentType] = mimeType;

            resp.ContentLength64 = body.Length;
            resp.Close(body, true);
        }

        [Pure]
        protected static string MimeTypeForExtension(string extension)
        {
            return ExtensionsToMimeTypes.ContainsKey(extension) ? ExtensionsToMimeTypes[extension] : "application/octet-stream";
        }

        protected virtual void HandleRequest(HttpListenerContext ctx)
        {
            var method = ctx.Request.HttpMethod;
            var path = ctx.Request.Url.AbsolutePath;

            if (path == "/")
            {
                if (method != "GET")
                {
                    Send405Response(ctx.Response, "GET");
                    return;
                }

                // load the health template
                var templateBody = File.ReadAllText(Path.Combine(PrinterHealthUtils.ProgramDirectory, "Templates", "health.html"), PrinterHealthUtils.Utf8NoBom);
                var template = Template.Parse(templateBody);

                // fill it
                var printerHashes = _monitor.Printers.Select(printer =>
                {
                    var media = printer.Value.Media.Select(medium =>
                    {
                        string statusClass = medium.IsEmpty ? "empty" : "unknown";
                        var monitoredMedium = medium as IMonitoredMedium;
                        if (monitoredMedium != null)
                        {
                            if (monitoredMedium.IsLow)
                            {
                                statusClass = "low";
                            }
                            else if (!monitoredMedium.IsEmpty)
                            {
                                // it's not unknown, it's full
                                statusClass = "full";
                            }
                        }

                        var mediumHash = new Hash
                        {
                            {"description", medium.Description},
                            {"style_classes", string.Join(" ", medium.StyleClasses)},
                            {"status_class", statusClass}
                        };

                        var measuredMedium = medium as IMeasuredMedium;
                        if (measuredMedium != null)
                        {
                            mediumHash["is_measurable"] = true;
                            mediumHash["percentage"] = ((int)Math.Round(measuredMedium.LevelPercent)).ToString(CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            mediumHash["is_measurable"] = false;
                        }

                        return mediumHash;
                    }).ToArray();

                    var markers = printer.Value.Markers.Select(marker =>
                    {
                        string statusClass = marker.IsEmpty ? "empty" : "unknown";
                        var monitoredMarker = marker as IMonitoredMarker;
                        if (monitoredMarker != null)
                        {
                            if (monitoredMarker.IsLow)
                            {
                                statusClass = "low";
                            }
                            else if (!monitoredMarker.IsEmpty)
                            {
                                // it's not unknown, it's full
                                statusClass = "full";
                            }
                        }

                        var markerHash = new Hash
                        {
                            {"description", marker.Description},
                            {"style_classes", string.Join(" ", marker.StyleClasses)},
                            {"status_class", statusClass}
                        };

                        var measuredMarker = marker as IMeasuredMarker;
                        if (measuredMarker != null)
                        {
                            markerHash["is_measurable"] = true;
                            markerHash["percentage"] = ((int)Math.Round(measuredMarker.LevelPercent)).ToString(CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            markerHash["is_measurable"] = false;
                        }

                        return markerHash;
                    }).ToArray();

                    var statusMessages = printer.Value.CurrentStatusMessages.Select(statusMessage => new Hash
                    {
                        {"level_class", statusMessage.Level.ToString().ToLowerInvariant()},
                        {"description", statusMessage.Description}
                    }).ToArray();

                    var lastUpdatedString = printer.Value.LastUpdated.HasValue
                        ? printer.Value.LastUpdated.Value.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture)
                        : "never";
                    var lastUpdateTimely = printer.Value.LastUpdated.HasValue &&
                        (DateTimeOffset.Now - printer.Value.LastUpdated.Value).TotalMinutes <= _config.OutdatedIntervalMinutes;
                    
                    return new Hash
                    {
                        {"name", printer.Key},
                        {"active_jobs", printer.Value.JobCount.ToString(CultureInfo.InvariantCulture)},
                        {"media", media},
                        {"markers", markers},
                        {"status_messages", statusMessages},
                        {"last_updated", lastUpdatedString},
                        {"last_updated_timeliness_class", lastUpdateTimely ? "timely" : "outdated"}
                    };
                }).ToArray();

                var hash = new Hash
                {
                    {"printers", printerHashes}
                };

                var rendered = template.Render(hash);
                var renderedBytes = PrinterHealthUtils.Utf8NoBom.GetBytes(rendered);

                SendOkResponse(ctx.Response, "text/html; charset=utf-8", renderedBytes);
                return;
            }
            else if (path.StartsWith("/static/"))
            {
                var staticPath = path.Substring(("/static/").Length);
                if (!AllowedStaticFilenameFormat.IsMatch(staticPath))
                {
                    // 404 for security reasons
                    Send404Response(ctx.Response);
                    return;
                }

                var fileInfo = new FileInfo(Path.Combine(PrinterHealthUtils.ProgramDirectory, "Static", staticPath));
                if (!fileInfo.Exists)
                {
                    Send404Response(ctx.Response);
                    return;
                }
                var bytes = File.ReadAllBytes(fileInfo.FullName);
                var extension = staticPath.Split('.')[1].ToLowerInvariant();
                var mimeType = MimeTypeForExtension(extension);

                SendOkResponse(ctx.Response, mimeType, bytes);
                return;
            }
        }

        protected virtual void Proc()
        {
            for (;;)
            {
                try
                {
                    var context = Listener.GetContext();
                    ThreadPool.QueueUserWorkItem(ctxObj =>
                    {
                        var ctx = (HttpListenerContext) ctxObj;
                        using (ctx.Response)
                        {
                            try
                            {
                                HandleRequest(ctx);
                            }
                            catch (Exception exc)
                            {
                                Logger.Error("handling HTTP request failed", exc);
                            }
                        }
                    }, context);
                }
                catch (Exception exc)
                {
                    if (!Listener.IsListening)
                    {
                        // listener stopped; I should stop too
                        return;
                    }
                    Logger.Error("HTTP listening handling broke", exc);
                }
            }
        }

        public void Start()
        {
            Listener.Start();
            _handlerThread = new Thread(Proc) {Name = "HTTP handler", IsBackground = true};
            _handlerThread.Start();
        }

        public void Stop()
        {
            Listener.Stop();
            _handlerThread.Join();
        }
    }
}
