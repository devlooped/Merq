using System.ComponentModel;

namespace Merq;

/// <summary>
/// Marker interface for all command handlers, whether synchronous or asynchronous.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface ICommandHandler { }

/// <summary>
/// Interface implemented by synchronous void commands.
/// </summary>
/// <typeparam name="TCommand">Type of command supported by the handler.</typeparam>
public interface ICommandHandler<in TCommand> : IExecutableCommandHandler<TCommand>, ICanExecute<TCommand> where TCommand : ICommand
{
    /// <summary>
    /// Executes the command synchronously.
    /// </summary>
    /// <param name="command">The command parameters for the execution.</param>
    void Execute(TCommand command);
}

/// <summary>
/// Interface implemented by synchronous command handlers that return a result.
/// </summary>
/// <typeparam name="TCommand">Type of command supported by the handler.</typeparam>
/// <typeparam name="TResult">The type of the returned value from the execution.</typeparam>
public interface ICommandHandler<in TCommand, out TResult> : IExecutableCommandHandler<TCommand, TResult>, IExecuteResult, ICanExecute<TCommand> where TCommand : ICommand<TResult>
{
    /// <summary>
    /// Executes the command synchronously.
    /// </summary>
    /// <param name="command">The command parameters for the execution.</param>
    /// <returns>The result of the execution.</returns>
    TResult Execute(TCommand command);
}

/// <summary>
/// Marker interface for all command handlers that return a result. 
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IExecuteResult { }
