using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wakaikami.Core.Database.Enums;

namespace Wakaikami.Core.Database.Extensions;

public static class DatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        DatabaseType type,
        string configKey
    )
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(configKey);

        var connectionString = configuration[configKey];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{configKey}' is not configured."
            );
        }

        services.AddSingleton(new DatabaseConnectionRegistration(type, connectionString));
        return services;
    }
}
