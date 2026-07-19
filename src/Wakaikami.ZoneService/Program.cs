using Microsoft.Extensions.Hosting;
using Wakaikami.Networking.Grpc.Extensions;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { ContentRootPath = AppContext.BaseDirectory });

builder.AddOpenTelemetryLogging();

var host = builder.Build();

await host.RunAsync(CancellationToken.None);
