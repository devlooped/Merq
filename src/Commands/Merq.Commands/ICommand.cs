namespace Merq
{
	/// <summary>
	/// Marker interface for commands.
	/// </summary>
	public interface ICommand { }

	/// <summary>
	/// Marker interface for commands whose execution results in a value
	/// being returned by its command handler.
	/// </summary>
	public interface ICommand<out TResult> : ICommand { }
}
