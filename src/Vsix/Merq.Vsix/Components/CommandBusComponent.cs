using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Linq;
using System.Reflection;
using Merq.Properties;

namespace Merq
{
	[Export(typeof(ICommandBus))]
	[PartCreationPolicy(CreationPolicy.Shared)]
	internal class CommandBusComponent : ICommandBus
	{
		MethodInfo canHandleMethod = typeof(CommandBusComponent)
			.GetTypeInfo()
			.GetDeclaredMethods("CanHandle")
			.First(m => m.IsGenericMethodDefinition);

		IComponentModel components;
		Runner forCommands;

		[ImportingConstructor]
		public CommandBusComponent([Import(typeof(Microsoft.VisualStudio.Shell.SVsServiceProvider))] IServiceProvider services)
			: this((IComponentModel)services.GetService(typeof(SComponentModel)))
		{
		}

		public CommandBusComponent(IComponentModel components)
		{
			this.components = components;
			forCommands = new Runner(components);
		}

		public event EventHandler<IExecutable> CommandStarted;
		public event EventHandler<CommandFinishedEventArgs> CommandFinished;

		public bool CanExecute<TCommand>(TCommand command) where TCommand : IExecutable
		{
			if (command == null) throw new ArgumentNullException(nameof(command));

			var handler = components.GetExtensions<ICanExecute<TCommand>>().FirstOrDefault();

			return handler == null ? false : handler.CanExecute(command);
		}

		public bool CanHandle(IExecutable command)
		{
			if (command == null) throw new ArgumentNullException(nameof(command));

			try
			{
				return (bool)canHandleMethod.MakeGenericMethod(command.GetType())
					.Invoke(this, new object[0]);
			}
			catch (TargetInvocationException ex)
			{
				// Rethrow the inner exception preserving stack trace.
				System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				// Will never get here.
				throw ex.InnerException;
			}
		}

		public bool CanHandle<TCommand>() where TCommand : IExecutable
		{
			return components.GetExtensions<ICanExecute<TCommand>>().Any();
		}

		public void Execute(ICommand command)
		{
			if (command == null) throw new ArgumentNullException(nameof(command));

			Exception error = null;
			var time = DateTime.UtcNow;
			TriggerEventWithShieldAsync(CommandStarted, command);
			try
			{
				ForCommand().Execute((dynamic)command);
			}
			catch (Exception ex)
			{
				error = ex;
				throw;
			}
			finally
			{
				TriggerEventWithShieldAsync(CommandFinished,
					new CommandFinishedEventArgs(
						command,
						error,
						DateTime.UtcNow.Subtract(time).TotalMilliseconds));
			}
		}

		public TResult Execute<TResult>(ICommand<TResult> command)
		{
			if (command == null) throw new ArgumentNullException(nameof(command));

			Exception error = null;
			var time = DateTime.UtcNow;
			TriggerEventWithShieldAsync(CommandStarted, command);
			try
			{
				return ForResult<TResult>().Execute((dynamic)command);
			}
			catch (Exception ex)
			{
				error = ex;
				throw;
			}
			finally
			{
				TriggerEventWithShieldAsync(CommandFinished,
					new CommandFinishedEventArgs(
						command,
						error,
						DateTime.UtcNow.Subtract(time).TotalMilliseconds));
			}
		}

		public Task ExecuteAsync(IAsyncCommand command, CancellationToken cancellation)
		{
			if (command == null) throw new ArgumentNullException(nameof(command));

			var time = DateTime.UtcNow;

			TriggerEventWithShieldAsync(CommandStarted, command);
			var task = (Task)ForCommand().ExecuteAsync((dynamic)command, cancellation);
			task.ContinueWith(t =>
				TriggerEventWithShieldAsync(CommandFinished,
					new CommandFinishedEventArgs(
						command,
						t.Exception,
						DateTime.UtcNow.Subtract(time).TotalMilliseconds)));

			return task;
		}

		public Task<TResult> ExecuteAsync<TResult>(IAsyncCommand<TResult> command, CancellationToken cancellation)
		{
			if (command == null) throw new ArgumentNullException(nameof(command));

			var time = DateTime.UtcNow;

			TriggerEventWithShieldAsync(CommandStarted, command);
			var task = (Task<TResult>)ForResult<TResult>().ExecuteAsync((dynamic)command, cancellation);
			task.ContinueWith(t =>
				TriggerEventWithShieldAsync(CommandFinished,
					new CommandFinishedEventArgs(
						command,
						t.Exception,
						DateTime.UtcNow.Subtract(time).TotalMilliseconds)));

			return task;
		}

		Task TriggerEventWithShieldAsync<T>(EventHandler<T> eventHandler, T args)
		{
			if (eventHandler != null)
			{
				return Task.Run(() =>
				{
					foreach (var handler in eventHandler.GetInvocationList())
					{
						try
						{
							handler.DynamicInvoke(this, args);
						}
						catch { }
					}
				});
			}

			return Task.FromResult(true);
		}

		protected virtual void OnCommandStarted(IExecutable command)
		{
			if (CommandStarted != null)
				foreach (EventHandler handler in CommandStarted.GetInvocationList())
					try
					{
						handler.DynamicInvoke(this, command);
					}
					catch { }
		}

		Runner ForCommand() => forCommands;

		Runner<TResult> ForResult<TResult>() => new Runner<TResult>(components);

		class Runner
		{
			IComponentModel components;

			public Runner(IComponentModel components)
			{
				this.components = components;
			}

			public void Execute<TCommand>(TCommand command) where TCommand : ICommand
			{
				var handler = components.GetExtensions<ICommandHandler<TCommand>>().FirstOrDefault();
				if (handler == null)
					throw new NotSupportedException(Strings.CommandBus.NoHandler(command.GetType()));

				handler.Execute(command);
			}

			public Task ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellation) where TCommand : IAsyncCommand
			{
				var handler = components.GetExtensions<IAsyncCommandHandler<TCommand>>().FirstOrDefault();
				if (handler == null)
					throw new NotSupportedException(Strings.CommandBus.NoHandler(command.GetType()));

				return handler.ExecuteAsync(command, cancellation);
			}
		}

		class Runner<TResult>
		{
			IComponentModel components;

			public Runner(IComponentModel components)
			{
				this.components = components;
			}

			public TResult Execute<TCommand>(TCommand command) where TCommand : ICommand<TResult>
			{
				var handler = components.GetExtensions<ICommandHandler<TCommand, TResult>>().FirstOrDefault();
				if (handler == null)
					throw new NotSupportedException(Strings.CommandBus.NoHandler(command.GetType()));

				return handler.Execute(command);
			}

			public Task<TResult> ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellation) where TCommand : IAsyncCommand<TResult>
			{
				var handler = components.GetExtensions<IAsyncCommandHandler<TCommand, TResult>>().FirstOrDefault();
				if (handler == null)
					throw new NotSupportedException(Strings.CommandBus.NoHandler(command.GetType()));

				return handler.ExecuteAsync(command, cancellation);
			}
		}
	}
}