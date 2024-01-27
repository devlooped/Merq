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
    /// Adds the <see cref="IMessageBus"/> service to the collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddMessageBus(this IServiceCollection services)
    {
        services.AddSingleton<IMessageBus, MessageBus>(sp => new MessageBus(sp));

        // Enables introspection of service registrations by the message bus.
        services.AddSingleton(_ => services);

        return services;
    }

    /// <summary>
    /// Adds the specific <typeparamref name="TMessageBus"/> implementation of 
    /// <see cref="IMessageBus"/> service to the collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddMessageBus<TMessageBus>(this IServiceCollection services)
        where TMessageBus : class, IMessageBus
    {
        services.AddSingleton<IMessageBus, TMessageBus>();

        // Enables introspection of service registrations by the message bus.
        services.AddSingleton(_ => services);

        return services;
    }
}
