using Microsoft.Extensions.DependencyInjection;

namespace Wakaikami.Core.Hosting.Extensions;

public static class ServerLifecycleRegistrationExtensions
{
    public static IServiceCollection AddServerLifecycleHostedServices(
        this IServiceCollection services,
        Action<IServiceCollection>? databaseStage = null,
        Action<IServiceCollection>? bootStage = null,
        Action<IServiceCollection>? postShutdownStage = null
    )
    {
        services.AddHostedService<ServerMainBootstrapHostedService>();
        databaseStage?.Invoke(services);
        services.AddHostedService<ServerModulesHostedService>();
        bootStage?.Invoke(services);
        services.AddHostedService<ServerShutdownHostedService>();
        postShutdownStage?.Invoke(services);
        return services;
    }
}
