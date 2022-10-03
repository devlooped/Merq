using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Merq;

/// <summary>
/// Usability overloads for <see cref="IMessageBus"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IMessageBusExtensions
{
    /// <summary>
    /// Notifies the bus of a new event instance of <typeparamref name="TEvent"/>.
    /// </summary>
    public static void Notify<TEvent>(this IMessageBus bus) where TEvent : new()
        => bus.Notify(new TEvent());

    /// <summary>
    /// Executes the given synchronous command.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to create and execute.</typeparam>
    public static void Execute<TCommand>(this IMessageBus bus) where TCommand : ICommand, new()
        => bus.Execute(new TCommand());

    /// <summary>
    /// Executes the given synchronous command.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to create and execute.</typeparam>
    public static Task ExecuteAsync<TCommand>(this IMessageBus bus, CancellationToken cancellation = default)
        where TCommand : IAsyncCommand, new()
        => bus.ExecuteAsync(new TCommand(), cancellation);

}
