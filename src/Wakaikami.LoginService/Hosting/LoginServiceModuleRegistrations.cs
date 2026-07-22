using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wakaikami.Core.Extensions;
using Wakaikami.Core.Hosting;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.LoginService.GameNetwork.Listening;
using Wakaikami.LoginService.GameNetwork.Listening.Interfaces;

namespace Wakaikami.LoginService.Hosting;

public class LoginServiceModuleRegistrations : IServiceRegistrar
{
    public void Register(IServiceCollection services, InitialType target, IConfiguration configuration)
    {
        services.AddSingletonAs<ServerMainBase, IServerLifecycle>();

        services.AddSingletonAs<LoginSessionManager, ILoginSessionManager>();
    }
}
