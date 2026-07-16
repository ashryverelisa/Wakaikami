using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Wakaikami.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSingletonAs<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImpl,
        TService1
    >(this IServiceCollection services)
        where TImpl : class, TService1
        where TService1 : class
    {
        services.AddSingleton<TImpl>();
        services.AddSingleton<TService1>(sp => sp.GetRequiredService<TImpl>());
        return services;
    }
}
