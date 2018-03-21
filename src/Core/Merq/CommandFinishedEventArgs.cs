using System;

namespace Merq
{
	/// <summary>
	/// Event args for <see cref="ICommandBus.CommandFinished"/>
	/// </summary>
    public class CommandFinishedEventArgs
    {
		/// <summary>
		/// Initialized an instance of <see cref="CommandFinishedEventArgs"/>
		/// </summary>
		/// <param name="command">The command</param>
		/// <param name="error">The exception when the command fails</param>
		/// <param name="elapsedMilliseconds">The amount of time the command execution took</param>
		public CommandFinishedEventArgs(
			IExecutable command, 
			Exception error,
			double elapsedMilliseconds)
		{
			Command = command;
			Error = error;
			ElapsedMilliseconds = elapsedMilliseconds;
		}

		/// <summary>
		/// Gets the command
		/// </summary>
		public IExecutable Command { get; }

		/// <summary>
		/// Gets the exception when the command execution fails
		/// </summary>
		public Exception Error { get; }

		/// <summary>
		/// Gets the amount of time the command execution took
		/// </summary>
		public double ElapsedMilliseconds { get; set; }
	}
}