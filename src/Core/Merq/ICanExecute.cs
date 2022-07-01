namespace Merq
{
    /// <summary>
    /// Interface implemented by all generic command handlers 
    /// to determine if they can execute a given command.
    /// </summary>
    public interface ICanExecute<in TCommand> where TCommand : IExecutable
    {
        /// <summary>
        /// Determines whether the given command can be executed given the 
        /// current state of the environment or the command itself.
        /// </summary>
        /// <param name="command">The command being queried for execution.</param>		
        bool CanExecute(TCommand command);
    }
}