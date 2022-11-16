using System.ComponentModel;
using Merq;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Contains extension methods to register the <see cref="IMessageBus"/> 
/// with a <see cref="IServiceCollection"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
static partial class MerqServicesExtension
{
    /// <summary>
    /// Adds the <see cref="IMessageBus"/> service and optionally all automatically discovered 
    /// components that were annotated with the <see cref="ServiceAttribute"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="addDiscoveredServices">Whether to add compile-time discovered services.</param>
    /// <param name="enableAutoMapping">Enables duck-typing behavior for events and commands, where 
    /// instances of disparate assemblies can observe and execute each other's events and commands 
    /// as long as their full type name matches.</param>
    /// </param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddMessageBus(this IServiceCollection services, bool addDiscoveredServices = true, bool enableAutoMapping = false)
    {
        if (enableAutoMapping)
            services.AddSingleton<IMessageBus, AutoMapperMessageBus>(sp => new AutoMapperMessageBus(sp));
        else
            services.AddSingleton<IMessageBus, MessageBus>(sp => new MessageBus(sp));

        // Enables introspection of service registrations by the message bus.
        services.AddSingleton(_ => services);

        if (addDiscoveredServices)
            services.AddServices();

        return services;
    }

    /// <summary>
    /// Adds the <see cref="IMessageBus"/> service and optionally all automatically discovered 
    /// components that were annotated with the <see cref="ServiceAttribute"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="addDiscoveredServices">Whether to add compile-time discovered services.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddMessageBus<TMessageBus>(this IServiceCollection services, bool addDiscoveredServices = true)
        where TMessageBus : class, IMessageBus
    {
        services.AddSingleton<IMessageBus, TMessageBus>();
        // Enables introspection of service registrations by the message bus.
        services.AddSingleton(_ => services);

        if (addDiscoveredServices)
            services.AddServices();

        return services;
    }
}
