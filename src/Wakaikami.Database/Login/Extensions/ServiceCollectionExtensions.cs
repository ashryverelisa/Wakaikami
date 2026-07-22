using Microsoft.Extensions.DependencyInjection;
using Wakaikami.Database.Login.Repositories;
using Wakaikami.Database.Login.Repositories.Interfaces;

namespace Wakaikami.Database.Login.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddLoginDatabase(this IServiceCollection services)
    {
        services.AddSingleton<IAccountRepository, AccountRepository>();
    }
}
