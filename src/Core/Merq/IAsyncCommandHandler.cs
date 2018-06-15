using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Merq
{
	/// <summary>
	/// Marker interface for asynchronous command handlers.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public interface IAsyncCommandHandler : ICommandHandler { }

	/// <summary>
	/// Interface implemented by command handlers that 
	/// handle commands asynchronously and don't return 
	/// a result.
	/// </summary>
	/// <typeparam name="TCommand">Type of command supported by the handler.</typeparam>
	public interface IAsyncCommandHandler<in TCommand> : IAsyncCommandHandler, IExecutableCommandHandler<TCommand>, ICanExecute<TCommand> where TCommand : IAsyncCommand
	{
		/// <summary>
		/// Executes the command asynchronously.
		/// </summary>
		/// <param name="command">The command parameters for the execution.</param>
		/// <param name="cancellation">Cancellation token to cancel command execution.</param>
		Task ExecuteAsync(TCommand command, CancellationToken cancellation);
	}

	/// <summary>
	/// Interface implemented by command handlers that 
	/// handle commands asynchronously and return a result.
	/// </summary>
	/// <typeparam name="TCommand">Type of command supported by the handler.</typeparam>
	/// <typeparam name="TResult">The type of the returned value from the execution.</typeparam>
	/// <devdoc>
	/// NOTE: we can't make TResult covariant since the result is Task{T} which is a class and 
	/// isn't covariant on the {T} either.
	/// </devdoc>
	public interface IAsyncCommandHandler<in TCommand, TResult> : IAsyncCommandHandler, IExecutableCommandHandler<TCommand, TResult>, IExecuteResult, ICanExecute<TCommand> where TCommand : IAsyncCommand<TResult>
	{
		/// <summary>
		/// Executes the command asynchronously.
		/// </summary>
		/// <param name="command">The command parameters for the execution.</param>
		/// <param name="cancellation">Cancellation token to cancel command execution.</param>
		/// <returns>The result of the execution.</returns>
		Task<TResult> ExecuteAsync(TCommand command, CancellationToken cancellation);
	}
}
