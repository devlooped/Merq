using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Merq;

/// <summary>
/// Default implementation of the <see cref="ICommandBus"/>, optimized 
/// for high performance and no dynamic or reflection invocation.
/// </summary>
/// <remarks>
/// There are basically four types of command/handler pairs: 
/// * void synchronous
/// * non-void synchronous
/// * void async
/// * non-void async
/// 
/// Each of these have different handler interfaces that are constrained 
/// by the command type, so that callers can know what invocation style 
/// to use depending on the command alone, not the implementation. The 
/// implementers are constrained by what's declared in the command type 
/// so that there is no mismatch between the invocation style and the 
/// implementation style. This avoids implementing anti-patterns like 
/// faking async on a non-async implementation and vice-versa. 
/// </remarks>
/// <devdoc>
/// In order for the caller to avoid having to specify both the command 
/// type and the result type when executing, we resort to inferring the 
/// latter from the former. But in order for this to work, we need to 
/// introduce an intermediary "executor" which is typed to the actual 
/// command instance received in the various Execute* overloads. 
/// 
/// This might seem like unnecessary complexity, but it actually produces 
/// a very performant invocation pattern (no reflection, lazy instantiation 
/// of the executors per command-type) while keeping the calling pattern on 
/// the command bus simple like:
/// 
///     FooResult result = commandBus.Execute(new FooCommand());
/// 
/// Without the executors, inference wouldn't "just work" and both types 
/// (command and result) would need to be specified, as in:
/// 
///     FooResult result = commandBus.Execute&lt;FooCommand, FooResult&gt;(new FooCommand());
/// 
/// which is quite awful.
/// 
/// Each of the four invocation styles gets its own non-generic executor, so that the 
/// Execute overloads can invoke them with the generic command argument, as well as the 
/// generic implementations that contain the actual invocation and downcast.
/// </devdoc>
public class CommandBus : ICommandBus
{
    readonly ConcurrentDictionary<Type, Lazy<ICommandHandler>> handlerMap = new();
    readonly ConcurrentDictionary<Type, VoidExecutor> voidExecutors = new();
    readonly ConcurrentDictionary<Type, VoidAsyncExecutor> voidAsyncExecutors = new();
    readonly ConcurrentDictionary<Type, ResultExecutor> resultExecutors = new();
    readonly ConcurrentDictionary<Type, ResultAsyncExecutor> resultAsyncExecutors = new();

    /// <summary>
    /// Creates a new <see cref="CommandBus"/> with the given set of command handlers.
    /// </summary>
    /// <param name="handlerMap">A map from command types to lazy command handlers.</param>
    public CommandBus(ConcurrentDictionary<Type, Lazy<ICommandHandler>> handlerMap)
        => this.handlerMap = handlerMap;

    /// <summary>
    /// Initializes the command bus with the given list of handlers.
    /// </summary>
    public CommandBus(IEnumerable<ICommandHandler> handlers)
        : this(new ConcurrentDictionary<Type, Lazy<ICommandHandler>>(
            (handlers ?? throw new ArgumentNullException(nameof(handlers)))
            .Select(x => new KeyValuePair<Type, Lazy<ICommandHandler>>(GetCommandType(x?.GetType() ?? throw new ArgumentNullException()), new Lazy<ICommandHandler>(() => x)))))
    {
    }

    /// <summary>
    /// Initializes the command bus with the given list of handlers.
    /// </summary>
    public CommandBus(params ICommandHandler[] handlers)
        : this((IEnumerable<ICommandHandler>)handlers)
    {
    }

    /// <summary>
    /// Registers a command handler type for use with the command bus.
    /// </summary>
    public virtual void Register<TCommandHandler>()
        where TCommandHandler : ICommandHandler, new()
        => handlerMap.AddOrUpdate(
            GetCommandType(typeof(TCommandHandler)),
            _ => new Lazy<ICommandHandler>(() => new TCommandHandler()),
            (_, __) => new Lazy<ICommandHandler>(() => new TCommandHandler()));

    /// <summary>
    /// Registers a command handler instance for use with the command bus.
    /// </summary>
    public virtual void Register<TCommandHandler>(TCommandHandler handler)
        where TCommandHandler : ICommandHandler
        => handlerMap.AddOrUpdate(
            GetCommandType(typeof(TCommandHandler)),
            _ => new Lazy<ICommandHandler>(() => handler),
            (_, __) => new Lazy<ICommandHandler>(() => handler));

    /// <summary>
    /// Registers a lazy command handler instance for use with the command bus.
    /// </summary>
    public virtual void Register<TCommandHandler>(Lazy<TCommandHandler> handler)
        where TCommandHandler : ICommandHandler
        => handlerMap.AddOrUpdate(
            GetCommandType(typeof(TCommandHandler)),
            _ => new Lazy<ICommandHandler>(() => handler.Value),
            (_, __) => new Lazy<ICommandHandler>(() => handler.Value));

    static Type GetCommandType(Type type)
        => type.GetTypeInfo().ImplementedInterfaces
            .Where(t => t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition() == typeof(IExecutableCommandHandler<>))
            .Select(t => t.GetTypeInfo().GenericTypeArguments[0])
            .FirstOrDefault() ?? throw new ArgumentException();

    /// <summary>
    /// Determines whether the given command type has a registered handler.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to query.</typeparam>
    /// <returns><see langword="true"/> if the command has a registered handler. <see langword="false"/> otherwise.</returns>
    public virtual bool CanHandle<TCommand>() where TCommand : IExecutable
        => handlerMap.ContainsKey(typeof(TCommand));

    /// <summary>
    /// Determines whether the given command has a registered handler.
    /// </summary>
    /// <param name="command">The command to query.</param>
    /// <returns><see langword="true"/> if the command has a registered handler. <see langword="false"/> otherwise.</returns>
    public virtual bool CanHandle(IExecutable command)
        => command != null && handlerMap.ContainsKey(command.GetType());

    /// <summary>
    /// Determines whether the given command can be executed by a registered
    /// handler with the provided command instance values.
    /// </summary>
    /// <param name="command">The command parameters for the query.</param>
    /// <returns><see langword="true"/> if the command can be executed. <see langword="false"/> otherwise.</returns>
    public virtual bool CanExecute<TCommand>(TCommand command) where TCommand : IExecutable
        => command != null &&
            handlerMap.TryGetValue(command.GetType(), out var handler) &&
            ((ICanExecute<TCommand>)handler.Value).CanExecute(command);

    /// <summary>
    /// Executes the given command synchronously.
    /// </summary>
    /// <param name="command">The command parameters for the execution.</param>
    public virtual void Execute(ICommand command)
        => voidExecutors.GetOrAdd(
            GetCommandType(command),
            type => (VoidExecutor)Activator.CreateInstance(
                typeof(VoidExecutor<>).MakeGenericType(type),
                GetCommandHandler(type)))
            .Execute(command);

    /// <summary>
    /// Executes the given command synchronously.
    /// </summary>
    /// <typeparam name="TResult">The return type of the command execution.</typeparam>
    /// <param name="command">The command parameters for the execution.</param>
    /// <returns>The result of executing the command.</returns>
    public virtual TResult Execute<TResult>(ICommand<TResult> command)
        => (TResult)resultExecutors.GetOrAdd(
            GetCommandType(command),
            type => (ResultExecutor)Activator.CreateInstance(
                typeof(ResultExecutor<,>).MakeGenericType(type, typeof(TResult)),
                GetCommandHandler(type)))
            .Execute(command)!;

    /// <summary>
    /// Executes the given asynchronous command.
    /// </summary>
    /// <param name="command">The command parameters for the execution.</param>
    /// <param name="cancellation">Cancellation token to cancel command execution.</param>
    public virtual Task ExecuteAsync(IAsyncCommand command, CancellationToken cancellation)
        => voidAsyncExecutors.GetOrAdd(
            GetCommandType(command),
            type => (VoidAsyncExecutor)Activator.CreateInstance(
                typeof(VoidAsyncExecutor<>).MakeGenericType(type),
                GetCommandHandler(type)))
            .ExecuteAsync(command, cancellation);

    /// <summary>
    /// Executes the given command asynchronously.
    /// </summary>
    /// <typeparam name="TResult">The return type of the command execution.</typeparam>
    /// <param name="command">The command parameters for the execution.</param>
    /// <param name="cancellation">Cancellation token to cancel command execution.</param>
    /// <returns>The result of executing the command.</returns>
    public virtual Task<TResult> ExecuteAsync<TResult>(IAsyncCommand<TResult> command, CancellationToken cancellation)
        => (Task<TResult>)resultAsyncExecutors.GetOrAdd(
            GetCommandType(command),
            type => (ResultAsyncExecutor)Activator.CreateInstance(
                typeof(ResultAsyncExecutor<,>).MakeGenericType(type, typeof(TResult)),
                GetCommandHandler(type)))
            .ExecuteAsync(command, cancellation);

    /// <summary>
    /// Command cannot be null for execution
    /// </summary>
    static Type GetCommandType(IExecutable command)
        => command?.GetType() ?? throw new ArgumentNullException(nameof(command));

    /// <summary>
    /// A handler must be registered for the given command.
    /// </summary>
    ICommandHandler GetCommandHandler(Type commandType)
        => handlerMap.TryGetValue(commandType, out var value) ? value.Value : throw new NotSupportedException(commandType.Name);

    abstract class VoidExecutor
    {
        public abstract void Execute(IExecutable command);
    }

    class VoidExecutor<TCommand> : VoidExecutor where TCommand : ICommand
    {
        readonly ICommandHandler<TCommand> handler;

        public VoidExecutor(ICommandHandler handler)
            => this.handler = (ICommandHandler<TCommand>)handler;

        public override void Execute(IExecutable command)
            => handler.Execute((TCommand)command);
    }

    abstract class ResultExecutor
    {
        public abstract object? Execute(IExecutable command);
    }

    class ResultExecutor<TCommand, TResult> : ResultExecutor where TCommand : ICommand<TResult>
    {
        readonly ICommandHandler<TCommand, TResult> handler;

        public ResultExecutor(ICommandHandler handler)
            => this.handler = (ICommandHandler<TCommand, TResult>)handler;

        public override object? Execute(IExecutable command)
            => handler.Execute((TCommand)command);
    }

    abstract class VoidAsyncExecutor
    {
        public abstract Task ExecuteAsync(IExecutable command, CancellationToken cancellation);
    }

    class VoidAsyncExecutor<TCommand> : VoidAsyncExecutor where TCommand : IAsyncCommand
    {
        readonly IAsyncCommandHandler<TCommand> handler;

        public VoidAsyncExecutor(ICommandHandler handler)
            => this.handler = (IAsyncCommandHandler<TCommand>)handler;

        public override Task ExecuteAsync(IExecutable command, CancellationToken cancellation)
            => handler.ExecuteAsync((TCommand)command, cancellation);
    }

    abstract class ResultAsyncExecutor
    {
        public abstract object ExecuteAsync(IExecutable command, CancellationToken cancellation);
    }

    class ResultAsyncExecutor<TCommand, TResult> : ResultAsyncExecutor where TCommand : IAsyncCommand<TResult>
    {
        readonly IAsyncCommandHandler<TCommand, TResult> handler;

        public ResultAsyncExecutor(ICommandHandler handler)
            => this.handler = (IAsyncCommandHandler<TCommand, TResult>)handler;

        public override object ExecuteAsync(IExecutable command, CancellationToken cancellation)
            => handler.ExecuteAsync((TCommand)command, cancellation);
    }
}
