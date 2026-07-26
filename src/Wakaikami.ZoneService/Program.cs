using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Wakaikami.Content.Hosting;
using Wakaikami.Core.Database.Enums;
using Wakaikami.Core.Hosting;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Extensions;
using Wakaikami.Networking.Grpc.Extensions;
using Wakaikami.Networking.Grpc.Health;
using Wakaikami.Networking.Hosting;
using Wakaikami.ZoneService.Configuration;
using Wakaikami.ZoneService.Grpc;
using Wakaikami.ZoneService.Hosting;

var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
if (!File.Exists(appSettingsPath))
    throw new FileNotFoundException("Required configuration file not found.", appSettingsPath);

// WebApplication instead of a generic host solely so Kestrel can serve the Prometheus
// scrape endpoint; the game listener and gRPC client links are unaffected.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory });

builder.AddOpenTelemetryLogging();
builder.AddPrometheusMetrics();
builder.Services.AddOptionsWithValidateOnStart<ZoneOptions, ZoneOptionsValidator>().Bind(builder.Configuration.GetSection(ZoneOptions.SectionName));

builder.AddServiceModules(
    InitialType.Zone,
    new UtilsModuleRegistrations(),
    new InfrastructureModuleRegistrations(),
    new NetworkingModuleRegistrations(),
    new ContentDataModuleRegistrations(),
    new ZoneServiceModuleRegistrations()
);

builder.Services.AddLogging();

builder
    .Services.AddHealthChecks()
    .AddGameServerReadiness(DatabaseType.World)
    .AddCheck<PushStreamHealthCheck>("push-stream", tags: [HealthCheckRegistrationExtensions.ReadyTag]);

builder.Services.AddServerLifecycleHostedServices(
    databaseStage: services =>
        services.AddDatabaseStartupProbe(DatabaseType.World, sp => sp.GetRequiredService<IOptions<ZoneOptions>>().Value.ZoneServerInfo.ZoneId),
    // Singleton + forward so the push-stream health check can observe the subscriber.
    bootStage: services =>
    {
        services.AddSingleton<ZonePushSubscriber>();
        services.AddSingleton<IPushStreamStatus>(sp => sp.GetRequiredService<ZonePushSubscriber>());
        services.AddHostedService(sp => sp.GetRequiredService<ZonePushSubscriber>());
    }
);

builder.WebHost.UseUrls();
builder.WebHost.ConfigureMetricsKestrel(builder.Configuration);

var app = builder.Build();

app.MapPrometheusMetrics();
app.MapHealthEndpoints();

await app.RunAsync();
