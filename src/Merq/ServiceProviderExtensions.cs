using System;
using System.Collections.Generic;

namespace Merq;

/// <summary>
/// Inspired by https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.DependencyInjection.Abstractions/src/ServiceProviderServiceExtensions.cs.
/// </summary>
static class ServiceProviderExtensions
{
    public static T? GetService<T>(this IServiceProvider provider)
        => (T?)(provider ?? throw new ArgumentNullException(nameof(provider))).GetService(typeof(T));

    public static T GetRequiredService<T>(this IServiceProvider provider) where T : notnull
        => (T)(provider ?? throw new ArgumentNullException(nameof(provider))).GetRequiredService(typeof(T));

    public static object GetRequiredService(this IServiceProvider provider, Type serviceType)
    {
        object? service = (provider ?? throw new ArgumentNullException(nameof(provider))).GetService(serviceType);
        if (service == null)
            throw new InvalidOperationException($"No service for type '{serviceType}' has been registered.");

        return service;
    }

    public static IEnumerable<T> GetServices<T>(this IServiceProvider provider)
        => provider.GetRequiredService<IEnumerable<T>>();

    public static IEnumerable<object?> GetServices(this IServiceProvider provider, Type serviceType)
    {
        var genericEnumerable = typeof(IEnumerable<>).MakeGenericType(serviceType);
        return (IEnumerable<object>)provider.GetRequiredService(genericEnumerable);
    }
}
