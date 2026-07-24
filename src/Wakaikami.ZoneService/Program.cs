using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Wakaikami.Content.Hosting;
using Wakaikami.Core.Database.Enums;
using Wakaikami.Core.Hosting;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Extensions;
using Wakaikami.Networking.Grpc.Extensions;
using Wakaikami.Networking.Hosting;
using Wakaikami.ZoneService.Configuration;
using Wakaikami.ZoneService.Hosting;

var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
if (!File.Exists(appSettingsPath))
    throw new FileNotFoundException("Required configuration file not found.", appSettingsPath);

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { ContentRootPath = AppContext.BaseDirectory });

builder.AddOpenTelemetryLogging();
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

builder.Services.AddServerLifecycleHostedServices(
    databaseStage: services =>
        services.AddDatabaseStartupProbe(DatabaseType.World, sp => sp.GetRequiredService<IOptions<ZoneOptions>>().Value.ZoneServerInfo.ZoneId),
    bootStage: services => services.AddHostedService<Wakaikami.ZoneService.Grpc.ZonePushSubscriber>()
);

var host = builder.Build();

await host.RunAsync(CancellationToken.None);
