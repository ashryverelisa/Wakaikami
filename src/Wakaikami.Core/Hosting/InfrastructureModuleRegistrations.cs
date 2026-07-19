using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wakaikami.Core.Database;
using Wakaikami.Core.Database.Interfaces;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;

namespace Wakaikami.Core.Hosting;

public sealed class InfrastructureModuleRegistrations : IServiceRegistrar
{
    public void Register(IServiceCollection services, InitialType target, IConfiguration configuration)
    {
        services.AddSingleton<IDbConnectionFactory, SqlDbConnectionFactory>();
    }
}
