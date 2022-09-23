using System;
using Microsoft.Extensions.DependencyInjection;

namespace Merq;

/// <summary>
/// Determines the lifetime of a service in an <see cref="IServiceCollection"/>.
/// </summary>
public enum ServiceLifetime
{
    /// <summary>
    /// The service should be registered as scoped.
    /// </summary>
    Scoped,
    /// <summary>
    /// The service should be registered as a singleton.
    /// </summary>
    Singleton,
    /// <summary>
    /// The service should be registered as transient.
    /// </summary>
    Transient,
}

/// <summary>
/// Configures the registration of a service in an <see cref="IServiceCollection"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ServiceAttribute : Attribute
{
    /// <summary>
    /// Annotates the service with the lifetime.
    /// </summary>
    public ServiceAttribute(ServiceLifetime lifetime = ServiceLifetime.Singleton)
        => Lifetime = lifetime;

    /// <summary>
    /// <see cref="ServiceLifetime"/> associated with a registered service 
    /// in an <see cref="IServiceCollection"/>.
    /// </summary>
    public ServiceLifetime Lifetime { get; }
}
