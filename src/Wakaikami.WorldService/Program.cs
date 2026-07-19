using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wakaikami.Networking.Grpc;
using Wakaikami.Networking.Grpc.Extensions;
using Wakaikami.Networking.Grpc.Helpers;
using Wakaikami.WorldService.Configuration;
using Wakaikami.WorldService.Grpc;
using Wakaikami.WorldService.Grpc.Interfaces;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory });

builder.AddOpenTelemetryLogging();
builder.Services.AddOptionsWithValidateOnStart<WorldOptions, WorldOptionsValidator>().Bind(builder.Configuration.GetSection(WorldOptions.SectionName));

builder.Services.AddLogging();
builder.Services.AddGrpc();
builder.Services.AddSingleton<IZonePushHub, ZonePushHub>();

var grpcPort = builder.Configuration.GetValue("Grpc:Port", 8510);
builder.WebHost.ConfigureGrpcKestrel(grpcPort, GrpcCertHelper.LoadWorldServerCert());

var app = builder.Build();

app.MapGrpcService<InternalControlService>();

await app.RunAsync();
