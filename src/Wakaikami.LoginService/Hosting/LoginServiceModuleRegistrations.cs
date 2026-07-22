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
using Wakaikami.LoginService.Configuration;
using Wakaikami.LoginService.Content.Account;
using Wakaikami.LoginService.Content.Account.Interfaces;
using Wakaikami.LoginService.Content.Transfer;
using Wakaikami.LoginService.Content.World;
using Wakaikami.LoginService.Content.World.Interfaces;
using Wakaikami.LoginService.GameNetwork.Listening;
using Wakaikami.LoginService.GameNetwork.Listening.Interfaces;

namespace Wakaikami.LoginService.Hosting;

public class LoginServiceModuleRegistrations : IServiceRegistrar
{
    public void Register(IServiceCollection services, InitialType target, IConfiguration configuration)
    {
        services.AddSingletonAs<ServerMainBase, IServerLifecycle>();

        services.AddDatabase(configuration, DatabaseType.Login, "Login:ConnectionString:AuthDb");

        services.AddSingleton<AccountTransferManager>();

        services.AddSingletonAs<LoginSessionManager, ILoginSessionManager, IGameServerModule>(sp => new LoginSessionManager(
            sp.GetRequiredService<IOptions<LoginOptions>>().Value.Info.MaxConnection,
            sp.GetRequiredService<IAccountManager>(),
            sp.GetRequiredService<AccountTransferManager>(),
            sp.GetRequiredService<ILogger<LoginSessionManager>>()
        ));
        services.AddHostedService(sp => sp.GetRequiredService<LoginSessionManager>());

        services.AddSingletonAs<AccountManager, IAccountManager>();
        services.AddSingletonAs<AccountPresence, IAccountPresence>();
        services.AddSingletonAs<WorldServerManager, IWorldServerManager>();
        services.AddSingletonAs<LoginServer, IGameServerModule, IShutdownHandler>();

        // Emitted by Wakaikami.Networking.Generators (PacketHandlerGenerator).
        services.AddLoginServicePacketHandlers();
    }
}
