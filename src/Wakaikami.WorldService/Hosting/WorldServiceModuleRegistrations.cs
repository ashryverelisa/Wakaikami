using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wakaikami.Core.Extensions;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.Networking.Grpc.Helpers;
using Wakaikami.Networking.Grpc.Messages.WorldLoginRegistration;
using Wakaikami.Networking.Grpc.Messages.WorldPush;
using Wakaikami.WorldService.Core;

namespace Wakaikami.WorldService.Hosting;

public sealed class WorldServiceModuleRegistrations : IServiceRegistrar
{
    public void Register(IServiceCollection services, InitialType target, IConfiguration configuration)
    {
        services.AddSingletonAs<ServerMain, IServerLifecycle>();

        {
            var loginEndpoint = configuration["Grpc:LoginEndpoint"] ?? "https://127.0.0.1:8500";
            var worldClientCert = GrpcCertHelper.LoadWorldClientCert();
            services.AddSingleton(_ =>
                GrpcChannel.ForAddress(loginEndpoint, new GrpcChannelOptions { HttpHandler = GrpcCertHelper.CreateClientHandler(worldClientCert) })
            );
            services.AddSingleton(sp => new WorldPushStream.WorldPushStreamClient(sp.GetRequiredService<GrpcChannel>()));
            services.AddSingleton(sp => new WorldLoginRegistration.WorldLoginRegistrationClient(sp.GetRequiredService<GrpcChannel>()));
        }
    }
}
