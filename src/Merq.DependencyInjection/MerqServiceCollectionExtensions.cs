using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Merq;

/// <summary>
/// Contains extension methods to <see cref="IServiceCollection"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class MerqServiceCollectionExtensions
{
    /// <summary>
    /// Adds the <see cref="IMessageBus"/> service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddMessageBus(this IServiceCollection services)
        => services.AddSingleton<IMessageBus, MessageBusService>();
}
