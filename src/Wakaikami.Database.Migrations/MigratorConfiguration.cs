using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Wakaikami.Database.Migrations;

public static class MigratorConfiguration
{
    public static IConfiguration Build()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("migrator.appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile(
                $"migrator.appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json",
                optional: true,
                reloadOnChange: false
            )
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    public static string ResolveConnectionString(IConfiguration configuration, string name, bool requirePassword = true)
    {
        var raw = configuration.GetConnectionString(name) ?? throw new InvalidOperationException($"Connection string '{name}' is not configured.");

        var builder = new SqlConnectionStringBuilder(raw);
        if (!string.IsNullOrEmpty(builder.Password))
        {
            return builder.ConnectionString;
        }

        var password = configuration["Sql:Password"];
        if (password is null && requirePassword)
        {
            throw new InvalidOperationException(
                $"No password for connection '{name}'. Set the 'Sql:Password' user-secret (dotnet user-secrets set \"Sql:Password\" \"<pw>\") or supply a full connection string."
            );
        }

        builder.Password = password ?? string.Empty;
        return builder.ConnectionString;
    }
}
