using System.ComponentModel;

namespace Merq
{
	/// <summary>
	/// Marker interface for both synchronous and asynchronous commands, which 
	/// allows <see cref="ICanExecute{TCommand}"/> and <see cref="ICommand"/> 
	/// reference either.
	/// </summary>
	[EditorBrowsable (EditorBrowsableState.Never)]
	public interface IExecutable
	{
	}
}
