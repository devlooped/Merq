using System;
using Microsoft.Extensions.DependencyInjection;

namespace Merq;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAll<TImplementation>(this IServiceCollection services)
        where TImplementation : class, new()
    {
        AddAll(services, _ => new TImplementation());
        return services;
    }

    public static IServiceCollection AddAll<TImplementation>(
        this IServiceCollection services,
        Func<IServiceProvider, TImplementation> implementationFactory)
        where TImplementation : class
    {
        // Simulates what the DependencyInjection.Attribute does, by registering 
        // the implementation with all the interfaces it implements.

        services.AddSingleton(implementationFactory);

        foreach (var interfaceType in typeof(TImplementation).GetInterfaces())
            services.AddSingleton(interfaceType, s => s.GetRequiredService(typeof(TImplementation)));

        return services;
    }
}
