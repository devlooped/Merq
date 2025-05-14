#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters. BOGUS: https://github.com/dotnet/roslyn/issues/16564#issuecomment-1629556744
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Merq;

/// <summary>
/// A general purpose bus carrying events and commands 
/// and connecting them with event subscribers and command 
/// handlers.
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Determines whether the given command type has a registered handler.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to query.</typeparam>
    /// <returns><see langword="true"/> if the command has a registered handler. 
    /// <see langword="false"/> otherwise.</returns>
    bool CanHandle<TCommand>() where TCommand : IExecutable;

    /// <summary>
    /// Determines whether the given command has a registered handler.
    /// </summary>
    /// <param name="command">The command to query.</param>
    /// <returns><see langword="true"/> if the command has a registered handler. 
    /// <see langword="false"/> otherwise.</returns>
    bool CanHandle(IExecutable command);

    /// <summary>
    /// Determines whether the given command can be executed by a registered 
    /// handler with the provided command instance values. If no registered 
    /// handler exists, returns <see langword="false"/>.
    /// </summary>
    /// <param name="command">The command parameters for the query.</param>
    /// <returns><see langword="true"/> if a command handler is registered and 
    /// the command can be executed. <see langword="false"/> otherwise.</returns>
    bool CanExecute<TCommand>(TCommand command) where TCommand : IExecutable;

    /// <summary>
    /// Executes the given synchronous command.
    /// </summary>
    /// <param name="command">The command parameters for the execution.</param>
    /// <param name="callerName">Optional calling member name, provided by default by the compiler.</param>
    /// <param name="callerFile">Optional calling file name, provided by default by the compiler.</param>
    /// <param name="callerLine">Optional calling line number, provided by default by the compiler.</param>
    void Execute(ICommand command, [CallerMemberName] string? callerName = default, [CallerFilePath] string? callerFile = default, [CallerLineNumber] int? callerLine = default);

    /// <summary>
    /// Executes the given synchronous command and returns a result from it.
    /// </summary>
    /// <typeparam name="TResult">The return type of the command execution.</typeparam>
    /// <param name="command">The command parameters for the execution.</param>
    /// <param name="callerName">Optional calling member name, provided by default by the compiler.</param>
    /// <param name="callerFile">Optional calling file name, provided by default by the compiler.</param>
    /// <param name="callerLine">Optional calling line number, provided by default by the compiler.</param>
    /// <returns>The result of executing the command.</returns>
    TResult Execute<TResult>(ICommand<TResult> command, [CallerMemberName] string? callerName = default, [CallerFilePath] string? callerFile = default, [CallerLineNumber] int? callerLine = default);

    /// <summary>
    /// Executes the given asynchronous command.
    /// </summary>
    /// <param name="command">The command parameters for the execution.</param>
    /// <param name="cancellation">Cancellation token to cancel command execution.</param>
    /// <param name="callerName">Optional calling member name, provided by default by the compiler.</param>
    /// <param name="callerFile">Optional calling file name, provided by default by the compiler.</param>
    /// <param name="callerLine">Optional calling line number, provided by default by the compiler.</param>
    ValueTask ExecuteAsync(IAsyncCommand command, CancellationToken cancellation = default, [CallerMemberName] string? callerName = default, [CallerFilePath] string? callerFile = default, [CallerLineNumber] int? callerLine = default);

    /// <summary>
    /// Executes the given asynchronous command and returns a result from it.
    /// </summary>
    /// <typeparam name="TResult">The return type of the command execution.</typeparam>
    /// <param name="command">The command parameters for the execution.</param>
    /// <param name="cancellation">Cancellation token to cancel command execution.</param>
    /// <param name="callerName">Optional calling member name, provided by default by the compiler.</param>
    /// <param name="callerFile">Optional calling file name, provided by default by the compiler.</param>
    /// <param name="callerLine">Optional calling line number, provided by default by the compiler.</param>
    /// <returns>The result of executing the command.</returns>
    ValueTask<TResult> ExecuteAsync<TResult>(IAsyncCommand<TResult> command, CancellationToken cancellation = default, [CallerMemberName] string? callerName = default, [CallerFilePath] string? callerFile = default, [CallerLineNumber] int? callerLine = default);

#if NET6_0_OR_GREATER
    /// <summary>
    /// Executes the given command that returns an async stream of values.
    /// </summary>
    /// <typeparam name="TResult">The type of values returned by the async stream.</typeparam>
    /// <param name="command">The command parameters for the execution.</param>
    /// <param name="cancellation">Cancellation token to cancel async enumeration.</param>
    /// <param name="callerName">Optional calling member name, provided by default by the compiler.</param>
    /// <param name="callerFile">Optional calling file name, provided by default by the compiler.</param>
    /// <param name="callerLine">Optional calling line number, provided by default by the compiler.</param>
    /// <returns>The async streaming results from the command execution.</returns>
    IAsyncEnumerable<TResult> ExecuteStream<TResult>(IStreamCommand<TResult> command, CancellationToken cancellation = default, [CallerMemberName] string? callerName = default, [CallerFilePath] string? callerFile = default, [CallerLineNumber] int? callerLine = default);
#endif

    /// <summary>
    /// Notifies the bus of an event.
    /// </summary>
    /// <param name="e">The event to notify to potential subscribers.</param>
    /// <param name="callerName">Optional calling member name, provided by default by the compiler.</param>
    /// <param name="callerFile">Optional calling file name, provided by default by the compiler.</param>
    /// <param name="callerLine">Optional calling line number, provided by default by the compiler.</param>
    ValueTask NotifyAsync<TEvent>(TEvent e, [CallerMemberName] string? callerName = default, [CallerFilePath] string? callerFile = default, [CallerLineNumber] int? callerLine = default);

    /// <summary>
    /// Observes the events of a given type <typeparamref name="TEvent"/>.
    /// </summary>
    IObservable<TEvent> Observe<TEvent>();
}
