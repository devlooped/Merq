using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
		static readonly ITracer tracer = Tracer.Get<CommandBus>();

		IAsyncManager asyncManager;
		Dictionary<Type, ICommandHandler> handlerMap;

		/// <summary>
		/// Initializes the command bus with the given list of handlers.
		/// </summary>
		public CommandBus (IAsyncManager asyncManager, IEnumerable<ICommandHandler> handlers)
		{
			this.asyncManager = asyncManager;
			AddHandlers (handlers);
		}

		/// <summary>
		/// Initializes the command bus with the given list of handlers.
		/// </summary>
		public CommandBus (IAsyncManager asyncManager, params ICommandHandler[] handlers)
			: this (asyncManager, (IEnumerable<ICommandHandler>)handlers)
		{
		}

		/// <summary>
		/// Determines whether the given command type has a registered handler.
		/// </summary>
		/// <typeparam name="TCommand">The type of command to query.</typeparam>
		/// <returns><see langword="true"/> if the command has a registered handler. <see langword="false"/> otherwise.</returns>
		public bool CanHandle<TCommand> () where TCommand : ICommand => handlerMap.ContainsKey (typeof (TCommand));

		/// <summary>
		/// Determines whether the given command has a registered handler.
		/// </summary>
		/// <param name="command">The command to query.</param>
		/// <returns><see langword="true"/> if the command has a registered handler. <see langword="false"/> otherwise.</returns>
		public bool CanHandle (ICommand command)
		{
			Guard.NotNull ("command", command);

			return handlerMap.ContainsKey (command.GetType ());
		}

		/// <summary>
		/// Determines whether the given command can be executed by a registered
		/// handler with the provided command instance values.
		/// </summary>
		/// <param name="command">The command parameters for the query.</param>
		/// <returns><see langword="true"/> if the command can be executed. <see langword="false"/> otherwise.</returns>
		public bool CanExecute (ICommand command)
		{
			Guard.NotNull ("command", command);

			var handler = GetCommandHandler(command);

			return handler.CanExecute ((dynamic)command);
		}

		/// <summary>
		/// Executes the given command synchronously.
		/// </summary>
		/// <param name="command">The command parameters for the execution.</param>
		public void Execute (ICommand command)
		{
			Guard.NotNull ("command", command);

			var handler = GetCommandHandler(command);

			if (handler is IAsyncCommandHandler)
				asyncManager.Run (async () => await 
					(Task)(handler.ExecuteAsync ((dynamic)command, CancellationToken.None)));
			else
				handler.Execute ((dynamic)command);
		}

		/// <summary>
		/// Executes the given command synchronously.
		/// </summary>
		/// <typeparam name="TResult">The return type of the command execution.</typeparam>
		/// <param name="command">The command parameters for the execution.</param>
		/// <returns>The result of executing the command.</returns>
		public TResult Execute<TResult>(ICommand<TResult> command)
		{
			Guard.NotNull ("command", command);

			var handler = GetCommandHandler(command);

			if (handler is IAsyncCommandHandler)
				return asyncManager.Run(async () => await
					(Task<TResult>)(handler.ExecuteAsync ((dynamic)command, CancellationToken.None)));
			else
				return handler.Execute ((dynamic)command);
		}

		/// <summary>
		/// Executes the given command asynchronously.
		/// </summary>
		/// <param name="command">The command parameters for the execution.</param>
		/// <param name="cancellation">Cancellation token to cancel command execution.</param>
		public Task ExecuteAsync(ICommand command, CancellationToken cancellation)
		{
			Guard.NotNull ("command", command);

			var handler = GetCommandHandler(command);

			if (handler is IAsyncCommandHandler)
				return handler.ExecuteAsync ((dynamic)command, cancellation);
			else
				return Task.Run ((Action)(() => handler.Execute ((dynamic)command)), cancellation);
		}

		/// <summary>
		/// Executes the given command asynchronously.
		/// </summary>
		/// <typeparam name="TResult">The return type of the command execution.</typeparam>
		/// <param name="command">The command parameters for the execution.</param>
		/// <param name="cancellation">Cancellation token to cancel command execution.</param>
		/// <returns>The result of executing the command.</returns>
		public Task<TResult> ExecuteAsync<TResult>(ICommand<TResult> command, CancellationToken cancellation)
		{
			Guard.NotNull ("command", command);

			var handler = GetCommandHandler(command);

			if (handler is IAsyncCommandHandler)
				return handler.ExecuteAsync ((dynamic)command, cancellation);
			else
				return Task.Run (() => (TResult)handler.Execute ((dynamic)command), cancellation);
		}

		void AddHandlers (IEnumerable<ICommandHandler> handlers)
		{
			handlerMap = new Dictionary<Type, ICommandHandler> ();
			var nonGenericHandlers = new List<ICommandHandler>();
			var noReturnsHandlers = new List<Tuple<Type, Type, ICommandHandler>>();
			var duplicateHandlers = new List<Tuple<Type, ICommandHandler>>();

			ProcessHandlers (handlers, nonGenericHandlers, noReturnsHandlers, duplicateHandlers);
			var errors = ProcessErrors (nonGenericHandlers, noReturnsHandlers, duplicateHandlers);

			if (!string.IsNullOrEmpty (errors))
				throw new ArgumentException (errors);
		}

		void ProcessHandlers (IEnumerable<ICommandHandler> handlers, List<ICommandHandler> nonGenericHandlers, List<Tuple<Type, Type, ICommandHandler>> noReturnsHandlers, List<Tuple<Type, ICommandHandler>> duplicateHandlers)
		{
			foreach (var handler in handlers.Where (x => x != null)) {
				var commandType = GetCommandType (handler.GetType ());
				if (commandType != null) {
					Type returnType;
					if (IsMissingReturnType (commandType, handler, out returnType))
						noReturnsHandlers.Add (Tuple.Create (commandType, returnType, handler));
					else if (!handlerMap.ContainsKey (commandType))
						handlerMap.Add (commandType, handler);
					else
						duplicateHandlers.Add (Tuple.Create (commandType, handler));
				} else {
					nonGenericHandlers.Add (handler);
				}
			}
		}

		static string ProcessErrors (List<ICommandHandler> nonGenericHandlers, List<Tuple<Type, Type, ICommandHandler>> noReturnsHandlers, List<Tuple<Type, ICommandHandler>> duplicateHandlers)
		{
			return string.Join (Environment.NewLine, nonGenericHandlers
				.Select (handler => Strings.CommandBus.InvalidHandler (handler.GetType ().Name))
				.Concat (duplicateHandlers
				.Select (handler => Strings.CommandBus.DuplicateHandler (handler.Item2.GetType ().Name, handler.Item1.Name)))
				.Concat (noReturnsHandlers
				.Select (handler => Strings.CommandBus.MissingReturnHandler (handler.Item3.GetType ().Name, handler.Item1.Name, handler.Item2.Name))));
		}

		dynamic GetCommandHandler (ICommand command)
		{
			ICommandHandler handler;
			if (!handlerMap.TryGetValue (command.GetType(), out handler)) {
				tracer.Error (Strings.CommandBus.NoHandler (command.GetType ()));
				throw new NotSupportedException (command.GetType ().FullName);
			}

			return handler;
		}

		static bool IsMissingReturnType (Type commandType, ICommandHandler handler, out Type returnType)
		{
			var commandReturns = typeof(ICommand<>);
			var commandInterface = commandType.GetInterfaces().FirstOrDefault(iface =>
				iface.IsGenericType && iface.GetGenericTypeDefinition() == commandReturns);

			if (commandInterface == null) {
				returnType = null;
				return false;
			}

			returnType = commandInterface.GetGenericArguments()[0];
			var syncReturns = typeof(ICommandHandler<,>);
			var asyncReturns = typeof(IAsyncCommandHandler<,>);

			var handlerReturns = handler.GetType().GetInterfaces()
				.Where(iface => iface.IsGenericType)
			 	.Select(iface => new { Concrete = iface, Generic = iface.GetGenericTypeDefinition() })
				.Where(iface => iface.Generic == syncReturns || iface.Generic == asyncReturns)
				.FirstOrDefault();

			if (handlerReturns == null)
				return true;

			return false;
        }

		static Type GetCommandType (Type type)
		{
			return type.GetInterfaces ()
				.Where (x => IsHandlerInterface (x))
				.Select (x => x.GetGenericArguments ()[0])
				.FirstOrDefault ();
		}

		static bool IsHandlerInterface (Type type)
		{
			if (!type.IsGenericType)
				return false;

			var genericType = type.GetGenericTypeDefinition();

			return genericType == typeof (ICommandHandler<>) ||
				genericType == typeof (ICommandHandler<,>) ||
				genericType == typeof (IAsyncCommandHandler<>) ||
				genericType == typeof (IAsyncCommandHandler<,>);
		}
	}
}
