using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Wakaikami.Content.Hosting;
using Wakaikami.Core.Database.Enums;
using Wakaikami.Core.Hosting;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Extensions;
using Wakaikami.Networking.Grpc;
using Wakaikami.Networking.Grpc.Extensions;
using Wakaikami.Networking.Grpc.Health;
using Wakaikami.Networking.Grpc.Helpers;
using Wakaikami.Networking.Hosting;
using Wakaikami.WorldService.Configuration;
using Wakaikami.WorldService.Grpc;
using Wakaikami.WorldService.Grpc.Interfaces;
using Wakaikami.WorldService.Hosting;

var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
if (!File.Exists(appSettingsPath))
    throw new FileNotFoundException("Required configuration file not found.", appSettingsPath);

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory });

builder.AddOpenTelemetryLogging();
builder.AddPrometheusMetrics();
builder.Services.AddOptionsWithValidateOnStart<WorldOptions, WorldOptionsValidator>().Bind(builder.Configuration.GetSection(WorldOptions.SectionName));

builder.AddServiceModules(
    InitialType.World,
    new UtilsModuleRegistrations(),
    new InfrastructureModuleRegistrations(),
    new NetworkingModuleRegistrations(),
    new ContentDataModuleRegistrations(),
    new WorldServiceModuleRegistrations()
);

builder.Services.AddLogging();
builder
    .Services.AddHealthChecks()
    .AddGameServerReadiness(DatabaseType.World)
    .AddCheck<PushStreamHealthCheck>("push-stream", tags: [HealthCheckRegistrationExtensions.ReadyTag]);

builder.Services.AddGrpc();
builder.Services.AddSingleton<ZoneWorldAccountService>();
builder.Services.AddSingleton<IZonePushHub, ZonePushHub>();
builder.Services.AddSingleton<ZonePushService>();
builder.Services.AddSingleton<ZoneRegistrationService>();

builder.Services.AddServerLifecycleHostedServices(
    databaseStage: services =>
        services.AddDatabaseStartupProbe(DatabaseType.World, sp => sp.GetRequiredService<IOptions<WorldOptions>>().Value.WorldInfo.WorldId),
    // gRPC connection lifecycle to Login: its boot path runs on a fully-initialized server.
    // Singleton + forward so the push-stream health check can observe the subscriber.
    bootStage: services =>
    {
        services.AddSingleton<WorldPushSubscriber>();
        services.AddSingleton<IPushStreamStatus>(sp => sp.GetRequiredService<WorldPushSubscriber>());
        services.AddHostedService(sp => sp.GetRequiredService<WorldPushSubscriber>());
    }
);

var grpcPort = builder.Configuration.GetValue("Grpc:Port", 8510);
builder.WebHost.ConfigureGrpcKestrel(grpcPort, GrpcCertHelper.LoadWorldServerCert());
builder.WebHost.ConfigureMetricsKestrel(builder.Configuration);

var app = builder.Build();

app.MapPrometheusMetrics();
app.MapHealthEndpoints();
app.MapGrpcService<InternalControlService>();
app.MapGrpcService<ZoneWorldAccountService>();
app.MapGrpcService<ZonePushService>();
app.MapGrpcService<ZoneRegistrationService>();

await app.RunAsync();
