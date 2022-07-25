using System.ComponentModel;

namespace Merq;

/// <summary>
/// Usability overloads for <see cref="IEventStream"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IMessageBusExtensions
{
    /// <summary>
    /// Notifies the bus of a new event instance of <typeparamref name="TEvent"/>.
    /// </summary>
    public static void Notify<TEvent>(this IMessageBus bus) where TEvent : new()
        => bus.Notify(new TEvent());
}
