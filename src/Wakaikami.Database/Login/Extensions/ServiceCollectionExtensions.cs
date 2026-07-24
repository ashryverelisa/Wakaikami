using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wakaikami.Database.Login.Repositories;
using Wakaikami.Database.Login.Repositories.Interfaces;

namespace Wakaikami.Database.Login.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>Dapper-based read path used by the login/game services.</summary>
    public static void AddLoginDatabase(this IServiceCollection services)
    {
        services.AddSingleton<IAccountRepository, AccountRepository>();
    }

    /// <summary>
    /// EF Core write path (account registration) used by the web front end. The caller supplies the
    /// provider configuration (e.g. <c>o =&gt; o.UseSqlServer(connectionString)</c>).
    /// </summary>
    public static IServiceCollection AddLoginRegistration(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDbContext)
    {
        services.AddDbContext<LoginDbContext>(configureDbContext);
        services.AddScoped<IAccountRegistrationRepository, AccountRegistrationRepository>();
        return services;
    }
}
