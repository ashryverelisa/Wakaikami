using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wakaikami.Core.Database.Enums;
using Wakaikami.Core.Database.Extensions;
using Wakaikami.Core.Extensions;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.Database.World.Extensions;
using Wakaikami.Networking.Grpc.Helpers;
using Wakaikami.Networking.Grpc.Messages.WorldLoginAccount;
using Wakaikami.Networking.Grpc.Messages.WorldLoginRegistration;
using Wakaikami.Networking.Grpc.Messages.WorldPush;
using Wakaikami.WorldService.Configuration;
using Wakaikami.WorldService.Content.Entity.Character;
using Wakaikami.WorldService.Content.Entity.Character.Data;
using Wakaikami.WorldService.Content.Entity.Character.Interfaces;
using Wakaikami.WorldService.Content.Zone;
using Wakaikami.WorldService.Core;
using Wakaikami.WorldService.GameNetwork.Listening;
using Wakaikami.WorldService.GameNetwork.Listening.Interfaces;
using Wakaikami.WorldService.Grpc;
using Wakaikami.WorldService.Grpc.Handlers.Client;
using Wakaikami.WorldService.Grpc.Handlers.Server;

namespace Wakaikami.WorldService.Hosting;

public sealed class WorldServiceModuleRegistrations : IServiceRegistrar
{
    public void Register(IServiceCollection services, InitialType target, IConfiguration configuration)
    {
        services.AddSingletonAs<ServerMain, IServerLifecycle>();

        services.AddDatabase(configuration, DatabaseType.World, "ConnectionStrings:World");
        services.AddWorldDatabase();

        services.AddSingletonAs<WorldSessionManager, IWorldSessionManager>(sp => new WorldSessionManager(
            sp.GetRequiredService<IOptions<WorldOptions>>().Value.WorldInfo.MaxConnection,
            sp.GetRequiredService<ISh03ClusterHandler>(),
            sp.GetRequiredService<ILogger<WorldSessionManager>>()
        ));
        services.AddHostedService(sp => sp.GetRequiredService<WorldSessionManager>());

        services.AddSingletonAs<CharacterManager, ICharacterManager>();

        services.AddSingletonAs<CharacterShapeProvider, IDataLoader>();
        services.AddSingletonAs<CharacterClassProvider, IDataLoader>();
        services.AddSingletonAs<DefaultCharacterDataProvider, IDataLoader>();
        services.AddSingleton<CharacterNameValidator>();
        services.AddSingletonAs<DetachedCharacterLoader, IDetachedCharacterLoader>();
        services.AddSingleton<CharacterRosterService>();

        services.AddWorldServicePacketHandlers();

        services.AddSingleton<Ch03ClusterHandler>();

        {
            var loginEndpoint = configuration["Grpc:LoginEndpoint"] ?? "https://127.0.0.1:8500";
            var worldClientCert = GrpcCertHelper.LoadWorldClientCert();
            services.AddSingleton(_ =>
                GrpcChannel.ForAddress(loginEndpoint, new GrpcChannelOptions { HttpHandler = GrpcCertHelper.CreateClientHandler(worldClientCert) })
            );
            services.AddSingleton(sp => new WorldLoginAccount.WorldLoginAccountClient(sp.GetRequiredService<GrpcChannel>()));
            services.AddSingleton(sp => new WorldPushStream.WorldPushStreamClient(sp.GetRequiredService<GrpcChannel>()));
            services.AddSingleton(sp => new WorldLoginRegistration.WorldLoginRegistrationClient(sp.GetRequiredService<GrpcChannel>()));
            services.AddSingleton<ISh03ClusterHandler, GrpcWorldSh03ClusterHandler>();
        }

        services.AddSingleton<ISh04ClusterHandler, WorldSh04ClusterHandler>();

        services.AddSingletonAs<WorldServer, IShutdownHandler>();

        services.AddSingletonAs<ZoneManager, IGameServerModule>();
    }
}
