using Microsoft.Extensions.DependencyInjection;
using Wakaikami.Core.Security.Interfaces;

namespace Wakaikami.Core.Security;

public static class SecurityServiceCollectionExtensions
{
    public static IServiceCollection AddPasswordHashing(this IServiceCollection services)
    {
        services.AddOptionsWithValidateOnStart<PasswordHashingOptions, PasswordHashingOptionsValidator>().BindConfiguration(PasswordHashingOptions.SectionName);
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        return services;
    }
}
