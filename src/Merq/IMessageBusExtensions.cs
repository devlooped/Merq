using System.ComponentModel;
using System.Runtime.CompilerServices;

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
    public static void Notify<TEvent>(this IMessageBus bus, [CallerMemberName] string? callerName = default, [CallerFilePath] string? callerFile = default, [CallerLineNumber] int? callerLine = default) where TEvent : new()
        => bus.Notify(new TEvent(), callerName, callerFile, callerLine);

    /// <summary>
    /// Executes the given synchronous command.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to create and execute.</typeparam>
    public static void Execute<TCommand>(this IMessageBus bus, [CallerMemberName] string? callerName = default, [CallerFilePath] string? callerFile = default, [CallerLineNumber] int? callerLine = default) where TCommand : ICommand, new()
        => bus.Execute(new TCommand(), callerName, callerFile, callerLine);
}
