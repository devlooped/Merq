using System.ComponentModel;

namespace Merq
{
	/// <summary>
	/// Marker interface for all generic command handlers, whether synchronous or asynchronous, 
	/// allowing easy introspection of the generic <typeparam name="TCommand" /> if necessary.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public interface IExecutableCommandHandler<in TCommand> : ICommandHandler where TCommand : IExecutable { }
}
