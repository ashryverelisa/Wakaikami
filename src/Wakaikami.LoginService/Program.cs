using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wakaikami.Content.Hosting;
using Wakaikami.Core.Hosting;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Extensions;
using Wakaikami.LoginService.Configuration;
using Wakaikami.LoginService.Grpc;
using Wakaikami.LoginService.Grpc.Interfaces;
using Wakaikami.LoginService.Hosting;
using Wakaikami.Networking.Grpc;
using Wakaikami.Networking.Grpc.Extensions;
using Wakaikami.Networking.Grpc.Helpers;
using Wakaikami.Networking.Hosting;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory });

builder.AddOpenTelemetryLogging();
builder.Services.AddOptionsWithValidateOnStart<LoginOptions, LoginOptionsValidator>().Bind(builder.Configuration.GetSection(LoginOptions.SectionName));

builder.AddServiceModules(
    InitialType.Login,
    new UtilsModuleRegistrations(),
    new InfrastructureModuleRegistrations(),
    new NetworkingModuleRegistrations(),
    new ContentDataModuleRegistrations(),
    new LoginServiceModuleRegistrations()
);

builder.Services.AddGrpc();
builder.Services.AddSingleton<IWorldPushHub, WorldPushHub>();
builder.Services.AddSingleton<WorldPushService>();
builder.Services.AddSingleton<WorldLoginRegistrationService>();

builder.Services.AddServerLifecycleHostedServices(bootStage: services =>
{
    services.AddHostedService<GameServerModulesHostedService>();
});

var grpcPort = builder.Configuration.GetValue("Grpc:Port", 8500);
builder.WebHost.ConfigureGrpcKestrel(grpcPort, GrpcCertHelper.LoadLoginServerCert());

var app = builder.Build();

app.MapGrpcService<InternalControlService>();
app.MapGrpcService<WorldPushService>();
app.MapGrpcService<WorldLoginRegistrationService>();

await app.RunAsync();
