namespace Merq
{
	/// <summary>
	/// Marker interface for asynchronous commands.
	/// </summary>
	public interface IAsyncCommand : IExecutable { }

	/// <summary>
	/// Marker interface for asynchronous commands whose execution results in a value
	/// being returned by its command handler.
	/// </summary>
	public interface IAsyncCommand<out TResult> : IExecutable<TResult> { }
}