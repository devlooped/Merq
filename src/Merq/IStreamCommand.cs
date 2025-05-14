#if NET6_0_OR_GREATER
using System.Collections.Generic;
using System.Threading;

namespace Merq;

/// <summary>
/// Marker interface for asynchronous commands whose execution results in a 
/// stream of values of the given <typeparamref name="TResult"/> type 
/// being returned by its command handler.
/// </summary>
public interface IStreamCommand<out TResult> : IExecutable<TResult> { }

/// <summary>
/// Interface implemented by command handlers that 
/// handle commands asynchronously and return a streaming result.
/// </summary>
/// <typeparam name="TCommand">Type of command supported by the handler.</typeparam>
/// <typeparam name="TResult">The type of the returned values from the execution.</typeparam>
/// <devdoc>
/// NOTE: we can't make TResult covariant since the result is Task{T} which is a class and 
/// isn't covariant on the {T} either.
/// </devdoc>
public interface IStreamCommandHandler<in TCommand, out TResult> : ICanExecute<TCommand> where TCommand : IStreamCommand<TResult>
{
    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    /// <param name="command">The command parameters for the execution.</param>
    /// <param name="cancellation">Cancellation token to cancel command execution.</param>
    /// <returns>The result of the execution.</returns>
    IAsyncEnumerable<TResult> ExecuteSteam(TCommand command, CancellationToken cancellation);
}
#endif