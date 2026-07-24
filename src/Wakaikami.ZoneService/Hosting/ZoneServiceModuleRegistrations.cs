using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wakaikami.Core.Database.Enums;
using Wakaikami.Core.Database.Extensions;
using Wakaikami.Core.Extensions;
using Wakaikami.Core.Hosting;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.Networking.Grpc.Helpers;
using Wakaikami.Networking.Grpc.Messages.ZonePush;
using Wakaikami.Networking.Grpc.Messages.ZoneRegistration;
using Wakaikami.Networking.Grpc.Messages.ZoneWorldAccount;
using Wakaikami.ZoneService.Configuration;
using Wakaikami.ZoneService.GameNetwork.Listening;
using Wakaikami.ZoneService.GameNetwork.Listening.Interfaces;
using Wakaikami.ZoneService.Grpc;
using Wakaikami.ZoneService.Grpc.Handlers.Servers;

namespace Wakaikami.ZoneService.Hosting;

public sealed class ZoneServiceModuleRegistrations : IServiceRegistrar
{
    public void Register(IServiceCollection services, InitialType target, IConfiguration configuration)
    {
        services.AddSingletonAs<ServerMainBase, IServerLifecycle>();

        services.AddDatabase(configuration, DatabaseType.World, "ConnectionStrings:World");

        services.AddSingletonAs<ZoneSessionManager, IZoneSessionManager>(sp => new ZoneSessionManager(
            sp.GetRequiredService<IOptions<ZoneOptions>>().Value.ZoneServerInfo.MaxConnection,
            sp.GetRequiredService<ISh03ClusterHandler>(),
            sp.GetRequiredService<ILogger<ZoneSessionManager>>()
        ));
        services.AddHostedService(sp => sp.GetRequiredService<ZoneSessionManager>());

        // Internal Zone↔World hop runs over gRPC.
        {
            var worldEndpoint = configuration["Grpc:WorldEndpoint"] ?? "https://127.0.0.1:8510";
            var zoneClientCert = GrpcCertHelper.LoadZoneClientCert();
            services.AddSingleton(_ =>
                GrpcChannel.ForAddress(worldEndpoint, new GrpcChannelOptions { HttpHandler = GrpcCertHelper.CreateClientHandler(zoneClientCert) })
            );

            services.AddSingleton(sp => new ZonePushStream.ZonePushStreamClient(sp.GetRequiredService<GrpcChannel>()));
            services.AddSingleton(sp => new ZoneRegistration.ZoneRegistrationClient(sp.GetRequiredService<GrpcChannel>()));
            services.AddSingleton(sp => new ZoneWorldAccount.ZoneWorldAccountClient(sp.GetRequiredService<GrpcChannel>()));
            services.AddSingleton<ISh03ClusterHandler, GrpcZoneSh03ClusterHandler>();
        }

        services.AddSingletonAs<ZoneServer, IShutdownHandler>();
    }
}
