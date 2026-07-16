using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using Wakaikami.Networking.Grpc.Helpers;

namespace Wakaikami.Networking.Grpc.Extensions;

public static class GrpcHostingExtensions
{
    public static IWebHostBuilder ConfigureGrpcKestrel(
        this IWebHostBuilder webHostBuilder,
        int port,
        X509Certificate2 cert
    )
    {
        webHostBuilder.UseUrls();
        webHostBuilder.ConfigureKestrel(options =>
            options.ListenAnyIP(
                port,
                listen =>
                {
                    listen.Protocols = HttpProtocols.Http2;
                    listen.UseHttps(
                        cert,
                        https =>
                        {
                            https.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                            https.ClientCertificateValidation =
                                GrpcCertHelper.ValidateClientCertificate;
                        }
                    );
                }
            )
        );
        return webHostBuilder;
    }

    /// <summary>
    /// Exports <see cref="ILogger"/> output to the OTLP endpoint (Aspire dashboard).
    /// No-op when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is unset (standalone run).
    /// </summary>
    public static IHostApplicationBuilder AddOpenTelemetryLogging(
        this IHostApplicationBuilder builder
    )
    {
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.Logging.AddOpenTelemetry(otel =>
            {
                otel.IncludeFormattedMessage = true;
                otel.IncludeScopes = true;
                otel.AddOtlpExporter();
            });
        }
        return builder;
    }
}
