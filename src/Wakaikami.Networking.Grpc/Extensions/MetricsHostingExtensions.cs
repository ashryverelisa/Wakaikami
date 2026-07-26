using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;

namespace Wakaikami.Networking.Grpc.Extensions;

public static class MetricsHostingExtensions
{
    public const int DefaultMetricsPort = 9464;

    /// <summary>
    /// Registers the OpenTelemetry metrics pipeline with a Prometheus scrape endpoint:
    /// all <c>Wakaikami.*</c> meters plus the built-in runtime, Kestrel/ASP.NET and gRPC meters.
    /// Metrics are additionally exported via OTLP when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set
    /// (Aspire dashboard), mirroring <see cref="GrpcHostingExtensions.AddOpenTelemetryLogging"/>.
    /// </summary>
    public static IHostApplicationBuilder AddPrometheusMetrics(this IHostApplicationBuilder builder)
    {
        builder
            .Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(
                    "Wakaikami.*",
                    "System.Runtime",
                    "System.Net.Sockets",
                    "System.Net.NameResolution",
                    "Microsoft.AspNetCore.Hosting",
                    "Microsoft.AspNetCore.Server.Kestrel",
                    "Grpc.AspNetCore.Server",
                    "Grpc.Net.Client"
                );

                metrics.AddPrometheusExporter();

                if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
                    metrics.AddOtlpExporter();
            });

        return builder;
    }

    /// <summary>
    /// Adds a plaintext HTTP/1.1 Kestrel listener for Prometheus scraping (<c>Metrics:Port</c>,
    /// default 9464), separate from the mTLS gRPC port so Prometheus needs no client certificate.
    /// </summary>
    public static IWebHostBuilder ConfigureMetricsKestrel(this IWebHostBuilder webHostBuilder, IConfiguration configuration)
    {
        var metricsPort = configuration.GetValue("Metrics:Port", DefaultMetricsPort);
        webHostBuilder.ConfigureKestrel(options => options.ListenAnyIP(metricsPort, listen => listen.Protocols = HttpProtocols.Http1));
        return webHostBuilder;
    }

    /// <summary>Maps the Prometheus scrape endpoint at <c>/metrics</c>.</summary>
    public static WebApplication MapPrometheusMetrics(this WebApplication app)
    {
        app.MapPrometheusScrapingEndpoint();
        return app;
    }
}
