namespace Merq
{
	/// <summary>
	/// Marker interface for synchronous commands.
	/// </summary>
	public interface ICommand : IExecutable { }

	/// <summary>
	/// Marker interface for synchronous commands whose execution results in a value
	/// being returned by its command handler.
	/// </summary>
	public interface ICommand<out TResult> : IExecutable<TResult> { }
}
