using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Merq;

/// <summary>
/// Usability overloads for <see cref="IMessageBus"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IMessageBusExtensions
{
    /// <summary>
    /// Notifies the bus of an event.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use await NotifyAsync instead.")]
    public static void Notify<TEvent>(this IMessageBus bus, TEvent e, [CallerMemberName] string? callerName = default, [CallerFilePath] string? callerFile = default, [CallerLineNumber] int? callerLine = default)
        => bus.NotifyAsync(e, callerName, callerFile, callerLine).Forget();

    /// <summary>
    /// Notifies the bus of a new event instance of <typeparamref name="TEvent"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use await NotifyAsync instead.")]
    public static void Notify<TEvent>(this IMessageBus bus, [CallerMemberName] string? callerName = default, [CallerFilePath] string? callerFile = default, [CallerLineNumber] int? callerLine = default) where TEvent : new()
        => bus.Notify(new TEvent(), callerName, callerFile, callerLine);

    /// <summary>
    /// Notifies the bus of a new event instance of <typeparamref name="TEvent"/>.
    /// </summary>
    public static Task NotifyAsync<TEvent>(this IMessageBus bus, [CallerMemberName] string? callerName = default, [CallerFilePath] string? callerFile = default, [CallerLineNumber] int? callerLine = default) where TEvent : new()
        => bus.NotifyAsync(new TEvent(), callerName, callerFile, callerLine);

    /// <summary>
    /// Executes the given synchronous command.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to create and execute.</typeparam>
    public static void Execute<TCommand>(this IMessageBus bus, [CallerMemberName] string? callerName = default, [CallerFilePath] string? callerFile = default, [CallerLineNumber] int? callerLine = default) where TCommand : ICommand, new()
        => bus.Execute(new TCommand(), callerName, callerFile, callerLine);
}
