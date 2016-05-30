using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Merq.Properties;

namespace Merq
{
	/// <summary>
	/// Default implementation of the <see cref="ICommandBus"/>.
	/// </summary>
	public class CommandBus : ICommandBus
	{
		Dictionary<Type, ICommandHandler> handlerMap;

		/// <summary>
		/// Initializes the command bus with the given list of handlers.
		/// </summary>
		public CommandBus (IEnumerable<ICommandHandler> handlers)
		{
			if (handlers == null) throw new ArgumentNullException (nameof (handlers));


			AddHandlers (handlers);
		}

		/// <summary>
		/// Initializes the command bus with the given list of handlers.
		/// </summary>
		public CommandBus (params ICommandHandler[] handlers)
			: this ((IEnumerable<ICommandHandler>)handlers)
		{
		}

		/// <summary>
		/// Determines whether the given command type has a registered handler.
		/// </summary>
		/// <typeparam name="TCommand">The type of command to query.</typeparam>
		/// <returns><see langword="true"/> if the command has a registered handler. <see langword="false"/> otherwise.</returns>
		public virtual bool CanHandle<TCommand> () where TCommand : IExecutable => handlerMap.ContainsKey (typeof (TCommand));

		/// <summary>
		/// Determines whether the given command has a registered handler.
		/// </summary>
		/// <param name="command">The command to query.</param>
		/// <returns><see langword="true"/> if the command has a registered handler. <see langword="false"/> otherwise.</returns>
		public virtual bool CanHandle (IExecutable command)
		{
			if (command == null) throw new ArgumentNullException (nameof (command));

			return handlerMap.ContainsKey (command.GetType ());
		}

		/// <summary>
		/// Determines whether the given command can be executed by a registered
		/// handler with the provided command instance values.
		/// </summary>
		/// <param name="command">The command parameters for the query.</param>
		/// <returns><see langword="true"/> if the command can be executed. <see langword="false"/> otherwise.</returns>
		public virtual bool CanExecute<TCommand> (TCommand command) where TCommand : IExecutable
		{
			if (command == null) throw new ArgumentNullException (nameof (command));

			var handler = GetCommandHandler ((IExecutable) command);

			return ((ICanExecute<TCommand>)handler).CanExecute (command);
		}

		/// <summary>
		/// Executes the given command synchronously.
		/// </summary>
		/// <param name="command">The command parameters for the execution.</param>
		public virtual void Execute (ICommand command)
		{
			if (command == null) throw new ArgumentNullException (nameof (command));

			ExecuteImpl (
				"ExecuteCommand",
				new[] { command.GetType () },
				new[] { command });
		}

		/// <summary>
		/// Executes the given command synchronously.
		/// </summary>
		/// <typeparam name="TResult">The return type of the command execution.</typeparam>
		/// <param name="command">The command parameters for the execution.</param>
		/// <returns>The result of executing the command.</returns>
		public virtual TResult Execute<TResult> (ICommand<TResult> command)
		{
			if (command == null) throw new ArgumentNullException (nameof (command));

			return (TResult)ExecuteImpl (
					"ExecuteCommandWithResult",
					new[] { command.GetType (), typeof (TResult) },
					new[] { command });
		}

		/// <summary>
		/// Executes the given asynchronous command.
		/// </summary>
		/// <param name="command">The command parameters for the execution.</param>
		/// <param name="cancellation">Cancellation token to cancel command execution.</param>
		public virtual Task ExecuteAsync (IAsyncCommand command, CancellationToken cancellation)
		{
			if (command == null) throw new ArgumentNullException (nameof (command));

			return (Task)ExecuteImpl (
					"ExecuteCommandAsync",
					new[] { command.GetType () },
					new object[] { command, cancellation });
		}

		/// <summary>
		/// Executes the given command asynchronously.
		/// </summary>
		/// <typeparam name="TResult">The return type of the command execution.</typeparam>
		/// <param name="command">The command parameters for the execution.</param>
		/// <param name="cancellation">Cancellation token to cancel command execution.</param>
		/// <returns>The result of executing the command.</returns>
		public virtual Task<TResult> ExecuteAsync<TResult> (IAsyncCommand<TResult> command, CancellationToken cancellation)
		{
			if (command == null) throw new ArgumentNullException (nameof (command));

			return (Task<TResult>)ExecuteImpl (
				"ExecuteCommandWithResultAsync",
				new[] { command.GetType (), typeof (TResult) },
				new object[] { command, cancellation });
		}

		object ExecuteImpl (string methodName, Type[] typeArguments, params object[] parameters)
		{
			// Important: We need to do this in order to be able to invoke the Execute or ExecuteAsync
			// methods without specifying the generic arguments by the caller explicitly
			// Otherwise it would not be possible to do the following:
			//
			// commandBus.Execute (new MyCommand ()) // void command
			// var result = commandBus.execute (new MyCommandWithResult ()) // command with result
			
			try {
				return this.GetType ()
					.GetTypeInfo ()
					.GetDeclaredMethod (methodName)
					.MakeGenericMethod (typeArguments)
					.Invoke (this, parameters);
			} catch (TargetInvocationException ex) {
				// TODO: replace the usage of throwing the inner exception with rethrow preserving stacktrace
				throw ex.InnerException;
			}
		}

		void ExecuteCommand<TCommand> (TCommand command) where TCommand : ICommand
		{
			if (command == null) throw new ArgumentNullException (nameof (command));

			var handler = GetCommandHandler(command);

			handler.Execute (command);
		}

		TResult ExecuteCommandWithResult<TCommand, TResult> (TCommand command) where TCommand : ICommand<TResult>
		{
			if (command == null) throw new ArgumentNullException (nameof (command));

			var handler = GetCommandHandler <TCommand, TResult>(command);

			return handler.Execute (command);
		}

		Task ExecuteCommandAsync<TCommand> (TCommand command, CancellationToken cancellation) where TCommand : IAsyncCommand
		{
			if (command == null) throw new ArgumentNullException (nameof (command));

			var handler = GetAsyncCommandHandler (command);

			return handler.ExecuteAsync (command, cancellation);
		}

		Task<TResult> ExecuteCommandWithResultAsync<TCommand, TResult> (TCommand command, CancellationToken cancellation) where TCommand : IAsyncCommand<TResult>
		{
			if (command == null) throw new ArgumentNullException (nameof (command));

			var handler = GetAsyncCommandHandler <TCommand, TResult> (command);

			return handler.ExecuteAsync ((dynamic)command, cancellation);
		}

		void AddHandlers (IEnumerable<ICommandHandler> handlers)
		{
			handlerMap = new Dictionary<Type, ICommandHandler> ();
			var nonGenericHandlers = new List<ICommandHandler>();
			var duplicateHandlers = new List<Tuple<Type, ICommandHandler>>();

			ProcessHandlers (handlers, nonGenericHandlers, duplicateHandlers);
			var errors = ProcessErrors (nonGenericHandlers, duplicateHandlers);

			if (!string.IsNullOrEmpty (errors))
				throw new ArgumentException (errors);
		}

		void ProcessHandlers (IEnumerable<ICommandHandler> handlers, List<ICommandHandler> nonGenericHandlers, List<Tuple<Type, ICommandHandler>> duplicateHandlers)
		{
			foreach (var handler in handlers.Where (x => x != null)) {
				// Extracts the first T in any of the command handler interfaces (sync, async, with/without result)
				var commandType = GetCommandType (handler.GetType ());
				if (commandType != null) {
					if (!handlerMap.ContainsKey (commandType))
						handlerMap.Add (commandType, handler);
					else
						// Can't have duplicate handlers either. This is one key 
						// difference between commands and events.
						duplicateHandlers.Add (Tuple.Create (commandType, handler));
				} else {
					// ICommandHandler without any T is not valid since it can't handle 
					// anything.
					nonGenericHandlers.Add (handler);
				}
			}
		}

		static string ProcessErrors (List<ICommandHandler> nonGenericHandlers, List<Tuple<Type, ICommandHandler>> duplicateHandlers)
		{
			return string.Join (Environment.NewLine, nonGenericHandlers
				.Select (handler => Strings.CommandBus.InvalidHandler (handler.GetType ().Name))
				.Concat (duplicateHandlers
				.Select (handler => Strings.CommandBus.DuplicateHandler (handler.Item2.GetType ().Name, handler.Item1.Name))));
		}

		ICommandHandler GetCommandHandler (IExecutable command)
		{
			ICommandHandler handler;
			if (!handlerMap.TryGetValue (command.GetType (), out handler)) {
				throw new NotSupportedException (Strings.CommandBus.NoHandler (command.GetType ()));
			}

			return handler;
		}

		ICommandHandler<TCommand> GetCommandHandler<TCommand> (TCommand command) where TCommand : ICommand
		{
			return (ICommandHandler<TCommand>)GetCommandHandler ((IExecutable)command);
		}

		ICommandHandler<TCommand, TResult> GetCommandHandler<TCommand, TResult> (IExecutable command) where TCommand : ICommand<TResult>
		{
			return (ICommandHandler<TCommand, TResult>)GetCommandHandler (command);
		}

		IAsyncCommandHandler<TCommand> GetAsyncCommandHandler<TCommand> (TCommand command) where TCommand : IAsyncCommand
		{
			return (IAsyncCommandHandler<TCommand>)GetCommandHandler ((IExecutable)command);
		}

		IAsyncCommandHandler<TCommand, TResult> GetAsyncCommandHandler<TCommand, TResult> (IExecutable command) where TCommand : IAsyncCommand<TResult>
		{
			return (IAsyncCommandHandler<TCommand, TResult>)GetCommandHandler (command);
		}

		static Type GetCommandType (Type type)
		{
			return type.GetTypeInfo ().ImplementedInterfaces
				.Where (x => IsHandlerInterface (x))
				.Select (x => x.GenericTypeArguments.First ())
				.FirstOrDefault ();
		}

		static bool IsHandlerInterface (Type type)
		{
			if (!type.IsConstructedGenericType)
				return false;

			var genericType = type.GetGenericTypeDefinition();

			return genericType == typeof (ICommandHandler<>) ||
				genericType == typeof (ICommandHandler<,>) ||
				genericType == typeof (IAsyncCommandHandler<>) ||
				genericType == typeof (IAsyncCommandHandler<,>);
		}
	}
}
