using System.ComponentModel;

namespace Merq;

/// <summary>
/// Marker interface for all generic command handlers, whether synchronous or asynchronous, 
/// allowing easy introspection of the generic <typeparamref name="TCommand" /> if necessary.
/// </summary>
/// <typeparam name="TCommand">The type of command to be executed.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IExecutableCommandHandler<in TCommand> : ICommandHandler where TCommand : IExecutable
{
}

/// <summary>
/// Marker interface for all generic command handlers that return values, whether synchronous or asynchronous, 
/// allowing easy introspection of the generic <typeparamref name="TCommand" /> and <typeparamref name="TResult"/> 
/// if necessary.
/// </summary>
/// <typeparam name="TCommand">The type of command to be executed.</typeparam>
/// <typeparam name="TResult">The type of the command execution result.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IExecutableCommandHandler<in TCommand, out TResult> : IExecutableCommandHandler<TCommand> where TCommand : IExecutable<TResult>
{
}
