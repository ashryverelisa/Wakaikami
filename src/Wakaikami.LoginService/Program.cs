using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Wakaikami.Networking.Grpc.Extensions;

var builder = WebApplication.CreateBuilder(
    new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory }
);

builder.AddOpenTelemetryLogging();

builder.Services.AddGrpc();

var app = builder.Build();

await app.RunAsync();
