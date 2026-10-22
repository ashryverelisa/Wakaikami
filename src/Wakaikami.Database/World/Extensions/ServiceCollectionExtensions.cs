using Microsoft.Extensions.DependencyInjection;
using Wakaikami.Database.World.Repositories;
using Wakaikami.Database.World.Repositories.Interfaces;

namespace Wakaikami.Database.World.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddWorldDatabase(this IServiceCollection services)
    {
        services.AddSingleton<ICharacterRepository, CharacterRepository>();
    }
}
