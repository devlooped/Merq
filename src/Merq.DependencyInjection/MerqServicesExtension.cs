using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Merq;

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
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddMessageBus(this IServiceCollection services, bool addDiscoveredServices = true)
    {
        services.AddSingleton<IMessageBus, MessageBus>();
        // Enables introspection of service registrations by the message bus.
        services.AddSingleton(_ => services);

        if (addDiscoveredServices)
            services.AddServices();

        return services;
    }
}
