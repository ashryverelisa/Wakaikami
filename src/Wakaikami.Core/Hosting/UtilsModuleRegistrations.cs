using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.Core.Time;
using Wakaikami.Core.Updates;

namespace Wakaikami.Core.Hosting;

public sealed class UtilsModuleRegistrations : IServiceRegistrar
{
    public void Register(IServiceCollection services, InitialType target, IConfiguration configuration)
    {
        services.AddSingleton(_ => new ServerTimeProvider(TimeProvider.System));
        services.AddSingleton(sp => new UpdateManager(sp.GetRequiredService<ServerTimeProvider>(), sp.GetRequiredService<ILogger<UpdateManager>>()));
        services.AddSingleton<IShutdownHandler>(sp => sp.GetRequiredService<UpdateManager>());
    }
}
