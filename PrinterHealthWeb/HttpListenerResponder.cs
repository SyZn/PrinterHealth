using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DotLiquid;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using PrinterHealth;
using PrinterHealth.Config;
using PrinterHealth.Model;
using RavuAlHemio.CentralizedLog;

namespace PrinterHealthWeb
{
    public class HttpListenerResponder
    {
        private static readonly ILogger Logger = CentralizedLogger.Factory.CreateLogger<HttpListenerResponder>();

        protected static readonly Regex AllowedStaticFilenameFormat = new Regex("^[a-zA-Z0-9-_]+[.][a-zA-Z0-9]+$");
        protected static readonly Dictionary<string, string> ExtensionsToMimeTypes = new Dictionary<string, string>
        {
            ["css"] = "text/css",
            ["js"] = "text/javascript",
            ["png"] = "image/png",
            ["jpg"] = "image/jpeg",
            ["jpeg"] = "image/jpeg",
        };

        protected IWebHost WebHost;
        private readonly PrinterHealthConfig _config;
        private readonly HealthMonitor _monitor;

        public HttpListenerResponder(PrinterHealthConfig config, HealthMonitor monitor)
        {
            _config = config;
            _monitor = monitor;

            X509Certificate2 cert = null;
            if (_config.Https)
            {
                if (_config.CertificateStoreFileName == null)
                {
                    if (_config.CertificateStorePassword == null)
                    {
                        Logger.LogCritical(
                            "Config error: Https is true but CertificateStoreFileName and CertificateStorePassword are null"
                        );
                    }
                    else
                    {
                        Logger.LogCritical("Config error: Https is true but CertificateStoreFileName is null");
                    }
                    return;
                }

                if (_config.CertificateStorePassword == null)
                {
                    Logger.LogCritical("Config error: Https is true but CertificateStoreFileName is null");
                    return;
                }

                cert = new X509Certificate2(
                    Path.Combine(PrinterHealthUtils.ProgramDirectory, config.CertificateStoreFileName),
                    config.CertificateStorePassword
                );
            }

            WebHost = new WebHostBuilder()
                .UseKestrel(kestrel =>
                {
                    kestrel.Listen(IPAddress.Any, _config.Port, listenConfig =>
                    {
                        listenConfig.UseHttps(cert);
                    });
                })
                .Configure(app =>
                {
                    app.Use(async (ctx, next) =>
                    {
                        await Task.Run(() => HandleRequest(ctx));
                        await next();
                    });
                })
                .Build();
        }

        protected static void SendPlainTextResponse(HttpResponse resp, int statusCode, string body)
        {
            resp.StatusCode = statusCode;
            resp.ContentType = "text/plain; charset=utf-8";

            var bodyBytes = PrinterHealthUtils.Utf8NoBom.GetBytes(body);
            resp.ContentLength = bodyBytes.Length;
            resp.Body.Write(bodyBytes, 0, bodyBytes.Length);
        }

        protected static void Send404Response(HttpResponse resp)
        {
            SendPlainTextResponse(resp, 404, "The requested file was not found.");
        }

        protected static void Send405Response(HttpResponse resp, params string[] allowedMethods)
        {
            var allowedMethodsString = string.Join(", ", allowedMethods);
            resp.Headers["Allow"] = allowedMethodsString;
            SendPlainTextResponse(resp, 405, "I can only be accessed using: " + allowedMethodsString);
        }

        protected static void SendOkResponse(HttpResponse resp, string mimeType, byte[] body)
        {
            resp.StatusCode = 200;
            resp.ContentType = mimeType;

            resp.ContentLength = body.Length;
            resp.Body.Write(body, 0, body.Length);
        }

        [Pure]
        protected static string MimeTypeForExtension(string extension)
        {
            return ExtensionsToMimeTypes.ContainsKey(extension) ? ExtensionsToMimeTypes[extension] : "application/octet-stream";
        }

        protected virtual void HandleRequest(HttpContext ctx)
        {
            var method = ctx.Request.Method;
            string path = ctx.Request.Path;

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
                            ["description"] = medium.Description,
                            ["style_classes"] = string.Join(" ", medium.StyleClasses),
                            ["status_class"] = statusClass
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
                            ["description"] = marker.Description,
                            ["style_classes"] = string.Join(" ", marker.StyleClasses),
                            ["status_class"] = statusClass
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
                        ["level_class"] = statusMessage.Level.ToString().ToLowerInvariant(),
                        ["description"] = statusMessage.Description
                    }).ToArray();

                    var lastUpdatedString = printer.Value.LastUpdated.HasValue
                        ? printer.Value.LastUpdated.Value.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture)
                        : "never";
                    var lastUpdateTimely = printer.Value.LastUpdated.HasValue &&
                        (DateTimeOffset.Now - printer.Value.LastUpdated.Value).TotalMinutes <= _config.OutdatedIntervalMinutes;
                    
                    return new Hash
                    {
                        ["name"] = printer.Key,
                        ["has_web_interface"] = (printer.Value.WebInterfaceUri != null),
                        ["web_interface_uri"] = printer.Value.WebInterfaceUri,
                        ["active_jobs"] = printer.Value.JobCount.ToString(CultureInfo.InvariantCulture),
                        ["media"] = media,
                        ["markers"] = markers,
                        ["status_messages"] = statusMessages,
                        ["last_updated"] = lastUpdatedString,
                        ["last_updated_timeliness_class"] = lastUpdateTimely ? "timely" : "outdated"
                    };
                }).ToArray();

                var hash = new Hash
                {
                    ["printers"] = printerHashes
                };

                var rendered = template.Render(new RenderParameters { LocalVariables = hash, Filters = new[] {typeof(PrinterHealthFilters)} });
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

        public void Start()
        {
            WebHost.Start();
        }

        public void Stop()
        {
            WebHost.Dispose();
        }
    }
}
