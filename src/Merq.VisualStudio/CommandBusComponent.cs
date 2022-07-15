using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace Merq;

[Export("Merq.ICommandBus.Default", typeof(ICommandBus))]
[PartCreationPolicy(CreationPolicy.Shared)]
class CommandBusComponent : ICommandBus
{
    static readonly MethodInfo canHandleMethod = typeof(CommandBusComponent)
        .GetTypeInfo()
        .GetDeclaredMethods("CanHandle")
        .First(m => m.IsGenericMethodDefinition);

    readonly IComponentModel components;
    readonly Runner forCommands;

    [ImportingConstructor]
    public CommandBusComponent([Import(typeof(SVsServiceProvider))] IServiceProvider services)
        : this((IComponentModel)services.GetService(typeof(SComponentModel)))
    {
    }

    public CommandBusComponent(IComponentModel components)
    {
        this.components = components;
        forCommands = new Runner(components);
    }

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
        => components.GetExtensions<ICanExecute<TCommand>>().Any();

    public void Execute(ICommand command)
        => ForCommand().Execute((dynamic)command ??
            throw new ArgumentNullException(nameof(command)));

    public TResult Execute<TResult>(ICommand<TResult> command)
        => ForResult<TResult>().Execute((dynamic)command ??
            throw new ArgumentNullException(nameof(command)));

    public Task ExecuteAsync(IAsyncCommand command, CancellationToken cancellation)
        => ForCommand().ExecuteAsync((dynamic)command ??
            throw new ArgumentNullException(nameof(command)), cancellation);

    public Task<TResult> ExecuteAsync<TResult>(IAsyncCommand<TResult> command, CancellationToken cancellation)
        => ForResult<TResult>().ExecuteAsync((dynamic)command ??
            throw new ArgumentNullException(nameof(command)), cancellation);

    Runner ForCommand() => forCommands;

    Runner<TResult> ForResult<TResult>() => new Runner<TResult>(components);

    class Runner
    {
        readonly IComponentModel components;

        public Runner(IComponentModel components) => this.components = components;

        public void Execute<TCommand>(TCommand command) where TCommand : ICommand
        {
            var handler = components.GetExtensions<ICommandHandler<TCommand>>().FirstOrDefault();
            if (handler == null)
                throw new NotSupportedException($"No command handler is registered for command type {command.GetType().FullName}.");

            handler.Execute(command);
        }

        public Task ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellation) where TCommand : IAsyncCommand
        {
            var handler = components.GetExtensions<IAsyncCommandHandler<TCommand>>().FirstOrDefault();
            if (handler == null)
                throw new NotSupportedException($"No command handler is registered for command type {command.GetType().FullName}.");

            return handler.ExecuteAsync(command, cancellation);
        }
    }

    class Runner<TResult>
    {
        readonly IComponentModel components;

        public Runner(IComponentModel components) => this.components = components;

        public TResult Execute<TCommand>(TCommand command) where TCommand : ICommand<TResult>
        {
            var handler = components.GetExtensions<ICommandHandler<TCommand, TResult>>().FirstOrDefault();
            if (handler == null)
                throw new NotSupportedException($"No command handler is registered for command type {command.GetType().FullName}.");

            return handler.Execute(command);
        }

        public Task<TResult> ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellation) where TCommand : IAsyncCommand<TResult>
        {
            var handler = components.GetExtensions<IAsyncCommandHandler<TCommand, TResult>>().FirstOrDefault();
            if (handler == null)
                throw new NotSupportedException($"No command handler is registered for command type {command.GetType().FullName}.");

            return handler.ExecuteAsync(command, cancellation);
        }
    }
}
