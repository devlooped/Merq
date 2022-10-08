using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Merq;

/// <summary>
/// Default implementation of <see cref="IMessageBus"/>, which relies on 
/// <see cref="IServiceProvider" /> to discover command handlers and 
/// external event producers.
/// </summary>
/// <remarks>
/// The default implementation assumes external event producers are registered 
/// with the service collection with the <see cref="IObservable{TEvent}"/> interfaces 
/// they implement.
/// <para>
/// Command handlers, in turn, need to be registered with:
/// * <see cref="ICanExecute{TCommand}"/>: to properly respond to invocations of 
/// <see cref="IMessageBus.CanExecute{TCommand}(TCommand)"/>
/// * <see cref="ICommandHandler{TCommand}"/>, <see cref="ICommandHandler{TCommand, TResult}"/>, 
/// <see cref="IAsyncCommandHandler{TCommand}"/> or <see cref="IAsyncCommandHandler{TCommand, TResult}"/> 
/// according to the corresponding marker interface implemented by the <c>TCommand</c> 
/// (<see cref="ICommand"/>, <see cref="ICommand{TResult}"/>, <see cref="IAsyncCommand"/> or 
/// <see cref="IAsyncCommand{TResult}"/> respectively).
/// </para>
/// <para>
/// There are basically four types of command/handler pairs: 
/// * void synchronous
/// * non-void synchronous
/// * void async
/// * non-void async
/// 
/// Each of these have different handler interfaces that are constrained 
/// by the command type, so that callers can know what invocation style 
/// to use depending on the command message itself, not the implementation. 
/// Implementers are constrained by what's declared in the command type 
/// so that there is no mismatch between the invocation style and the 
/// implementation style. This avoids implementing anti-patterns like 
/// async over sync and sync over async.
/// </para>
/// </remarks>
public class MessageBus : IMessageBus
{
    static readonly ConcurrentDictionary<Type, Type> handlerTypeMap = new();
    static readonly MethodInfo canHandleMethod = typeof(MessageBus).GetMethod(nameof(CanHandle), Type.EmptyTypes);

    // All subjects active in the event stream.
    readonly ConcurrentDictionary<Type, Subject> subjects = new();
    // An cache of subjects indexed by the compatible event types, used to quickly lookup the subjects to 
    // invoke in a Notify. Refreshed whenever a new Observe<T> subscription is added.
    readonly ConcurrentDictionary<Type, Subject[]> compatibleSubjects = new();

    readonly ConcurrentDictionary<Type, bool> canHandleMap = new();
    readonly IServiceProvider services;

    readonly ConcurrentDictionary<Type, VoidDispatcher> voidExecutors = new();
    readonly ConcurrentDictionary<Type, VoidAsyncDispatcher> voidAsyncExecutors = new();
    readonly ConcurrentDictionary<Type, ResultDispatcher> resultExecutors = new();
    readonly ConcurrentDictionary<Type, ResultAsyncDispatcher> resultAsyncExecutors = new();

    /// <summary>
    /// Instantiates the message bus with the given <see cref="IServiceProvider"/> 
    /// that resolves instances of command handlers and external event producers.
    /// </summary>
    public MessageBus(IServiceProvider services)
        => this.services = services;

    /// <summary>
    /// Determines whether the given command can be executed by a registered 
    /// handler with the provided command instance values. If no registered 
    /// handler exists, returns <see langword="false"/>.
    /// </summary>
    /// <param name="command">The command parameters for the query.</param>
    /// <returns><see langword="true"/> if a command handler is registered and 
    /// the command can be executed. <see langword="false"/> otherwise.</returns>
    /// <remarks>
    /// Command handlers need to be registered in the given <see cref="IServiceProvider"/> with the 
    /// <see cref="ICommandHandler{TCommand}"/>, <see cref="ICommandHandler{TCommand, TResult}"/>, 
    /// <see cref="IAsyncCommandHandler{TCommand}"/> or <see cref="IAsyncCommandHandler{TCommand, TResult}"/> 
    /// service interface, according to the corresponding marker interface implemented by their 
    /// <c>TCommand</c> parameter (<see cref="ICommand"/>, <see cref="ICommand{TResult}"/>, 
    /// <see cref="IAsyncCommand"/> or <see cref="IAsyncCommand{TResult}"/> respectively).
    /// </remarks>
    public bool CanExecute<TCommand>(TCommand command) where TCommand : IExecutable
        => services.GetService(GetHandlerType(GetCommandType(command))) is ICanExecute<TCommand> canExec &&
           canExec.CanExecute(command);

    /// <summary>
    /// Determines whether the given command type has handler registered in the 
    /// <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to query.</typeparam>
    /// <returns><see langword="true"/> if the command has a registered handler. 
    /// <see langword="false"/> otherwise.</returns>
    /// <remarks>
    /// Command handlers need to be registered in the given <see cref="IServiceProvider"/> with the 
    /// <see cref="ICommandHandler{TCommand}"/>, <see cref="ICommandHandler{TCommand, TResult}"/>, 
    /// <see cref="IAsyncCommandHandler{TCommand}"/> or <see cref="IAsyncCommandHandler{TCommand, TResult}"/> 
    /// service interface, according to the corresponding marker interface implemented by their 
    /// <c>TCommand</c> parameter (<see cref="ICommand"/>, <see cref="ICommand{TResult}"/>, 
    /// <see cref="IAsyncCommand"/> or <see cref="IAsyncCommand{TResult}"/> respectively).
    /// </remarks>
    public bool CanHandle<TCommand>() where TCommand : IExecutable
        => canHandleMap.GetOrAdd(typeof(TCommand), type
            => services.GetService(GetHandlerType(typeof(TCommand))) != null);

    /// <summary>
    /// Determines whether the given command has a handler registered in the
    /// <see cref="IServiceProvider"/>, according to the runtime-type of the <paramref name="command"/>.
    /// </summary>
    /// <param name="command">The command to query.</param>
    /// <returns><see langword="true"/> if the command has a registered handler. 
    /// <see langword="false"/> otherwise.</returns>
    /// <remarks>
    /// Command handlers need to be registered in the given <see cref="IServiceProvider"/> with the 
    /// <see cref="ICommandHandler{TCommand}"/>, <see cref="ICommandHandler{TCommand, TResult}"/>, 
    /// <see cref="IAsyncCommandHandler{TCommand}"/> or <see cref="IAsyncCommandHandler{TCommand, TResult}"/> 
    /// service interface, according to the corresponding marker interface implemented by the 
    /// <paramref name="command"/> instance (<see cref="ICommand"/>, <see cref="ICommand{TResult}"/>, 
    /// <see cref="IAsyncCommand"/> or <see cref="IAsyncCommand{TResult}"/> respectively).
    /// </remarks>
    public bool CanHandle(IExecutable command)
        => canHandleMap.GetOrAdd(command.GetType(), type
            => services.GetService(GetHandlerType(GetCommandType(command))) != null);

    /// <summary>
    /// Executes the given synchronous command.
    /// </summary>
    /// <param name="command">The command parameters for the execution.</param>
    public void Execute(ICommand command)
    {
        var type = GetCommandType(command);
        if (type.IsPublic)
            // For public types, we can use the faster dynamic dispatch approach
            ExecuteImpl((dynamic)command);
        else
            voidExecutors.GetOrAdd(type, type
                => (VoidDispatcher)Activator.CreateInstance(
                    typeof(VoidDispatcher<>).MakeGenericType(type),
                    services))
            .Execute(command);
    }

    /// <summary>
    /// Executes the given synchronous command and returns a result from it.
    /// </summary>
    /// <typeparam name="TResult">The return type of the command execution.</typeparam>
    /// <param name="command">The command parameters for the execution.</param>
    /// <returns>The result of executing the command.</returns>
    public TResult Execute<TResult>(ICommand<TResult> command)
    {
        var type = GetCommandType(command);
        if (type.IsPublic)
            // For public types, we can use the faster dynamic dispatch approach
            return WithResult<TResult>().Execute((dynamic)command);

        return (TResult)resultExecutors.GetOrAdd(type, type
                => (ResultDispatcher)Activator.CreateInstance(
                   typeof(ResultDispatcher<,>).MakeGenericType(type, typeof(TResult)),
                   services))
            .Execute(command)!;
    }

    /// <summary>
    /// Executes the given asynchronous command.
    /// </summary>
    /// <param name="command">The command parameters for the execution.</param>
    /// <param name="cancellation">Cancellation token to cancel command execution.</param>
    public Task ExecuteAsync(IAsyncCommand command, CancellationToken cancellation)
    {
        var type = GetCommandType(command);
        if (type.IsPublic)
            // For public types, we can use the faster dynamic dispatch approach
            return ExecuteAsyncImpl((dynamic)command, cancellation);

        return voidAsyncExecutors.GetOrAdd(type, type
            => (VoidAsyncDispatcher)Activator.CreateInstance(
                typeof(VoidAsyncDispatcher<>).MakeGenericType(type),
                services))
            .ExecuteAsync(command, cancellation);
    }

    /// <summary>
    /// Executes the given asynchronous command and returns a result from it.
    /// </summary>
    /// <typeparam name="TResult">The return type of the command execution.</typeparam>
    /// <param name="command">The command parameters for the execution.</param>
    /// <param name="cancellation">Cancellation token to cancel command execution.</param>
    /// <returns>The result of executing the command.</returns>
    public Task<TResult> ExecuteAsync<TResult>(IAsyncCommand<TResult> command, CancellationToken cancellation)
    {
        var type = GetCommandType(command);
        if (type.IsPublic)
            // For public types, we can use the faster dynamic dispatch approach
            return WithResult<TResult>().ExecuteAsync((dynamic)command, cancellation);

        return (Task<TResult>)resultAsyncExecutors.GetOrAdd(type, type
            => (ResultAsyncDispatcher)Activator.CreateInstance(
                typeof(ResultAsyncDispatcher<,>).MakeGenericType(type, typeof(TResult)),
                services))
            .ExecuteAsync(command, cancellation);
    }

    /// <summary>
    /// Notifies the bus of an event.
    /// </summary>
    public void Notify<TEvent>(TEvent e)
    {
        var type = (e ?? throw new ArgumentNullException(nameof(e))).GetType();

        OnUsingEvent(type);

        // TODO: if we prevent Notify for externally produced events, we won't be 
        // able to notify base event subscribers when those events are produced. 
        //var producer = services.GetService<IObservable<TEvent>>();
        //if (producer != null)
        //    throw new NotSupportedException($"Cannot explicitly notify event {type} because it is externally produced by {producer.GetType()}.");

        // We call all subjects that are compatible with
        // the event type, not just concrete event type subscribers.
        var compatible = compatibleSubjects.GetOrAdd(type, eventType => subjects.Keys
            .Where(subjectEventType => subjectEventType.IsAssignableFrom(eventType))
            .Select(subjectEventType => subjects[subjectEventType])
            .ToArray());

        foreach (var subject in compatible)
        {
            subject.OnNext(e);
        }
    }

    /// <summary>
    /// Observes the events of a given type <typeparamref name="TEvent"/>.
    /// </summary>
    public IObservable<TEvent> Observe<TEvent>()
    {
        OnUsingEvent(typeof(TEvent));

        // NOTE: in order for the base event subscription to work properly for external
        // producers, they must register the service for each T in the TEvent hierarchy.
        var producers = services.GetServices<IObservable<TEvent>>().ToArray();

        var subject = (IObservable<TEvent>)subjects.GetOrAdd(typeof(TEvent), type =>
        {
            // If we're creating a new subject, we need to clear the cache of compatible subjects
            compatibleSubjects.Clear();
            return new Subject<TEvent>();
        });

        if (producers.Length == 0)
            return subject;

        // Merge with any externally-produced observables that are compatible
        return new CompositeObservable<TEvent>(new[] { subject }.Concat(producers).ToArray());
    }

    /// <summary>
    /// Derived classes can inspect the types of events that are being observed or notified 
    /// on the message bus.
    /// </summary>
    protected virtual void OnUsingEvent(Type eventType) { }

    static Type GetHandlerType(Type commandType)
    {
        return handlerTypeMap.GetOrAdd(commandType, type =>
        {
            if (typeof(ICommand).IsAssignableFrom(commandType))
                return typeof(ICommandHandler<>).MakeGenericType(type);

            if (type.GetInterfaces().FirstOrDefault(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>)) is Type iface)
                return typeof(ICommandHandler<,>).MakeGenericType(type, iface.GetGenericArguments()[0]);

            if (typeof(IAsyncCommand).IsAssignableFrom(commandType))
                return typeof(IAsyncCommandHandler<>).MakeGenericType(type);

            if (type.GetInterfaces().FirstOrDefault(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncCommand<>)) is Type iface2)
                return typeof(IAsyncCommandHandler<,>).MakeGenericType(type, iface2.GetGenericArguments()[0]);

            throw new InvalidOperationException($"Type {type} does not implement any command interface.");
        });
    }

    static Type GetCommandType(IExecutable command)
        => command?.GetType() ?? throw new ArgumentNullException(nameof(command));

    void ExecuteImpl<TCommand>(TCommand command) where TCommand : ICommand
        => services.GetRequiredService<ICommandHandler<TCommand>>().Execute(command);

    Task ExecuteAsyncImpl<TCommand>(TCommand command, CancellationToken cancellation) where TCommand : IAsyncCommand
        => services.GetRequiredService<IAsyncCommandHandler<TCommand>>().ExecuteAsync(command, cancellation);

    // dynamic dispatch cannot infer TResult from TCommand, so we need to use a generic method
    // that first "sets" the TResult and then use dynamic dispatch on the resulting instance.
    With<TResult> WithResult<TResult>() => new(services);

    struct With<TResult>
    {
        readonly IServiceProvider services;

        public With(IServiceProvider services) => this.services = services;

        public TResult Execute<TCommand>(TCommand command) where TCommand : ICommand<TResult>
            => services.GetRequiredService<ICommandHandler<TCommand, TResult>>().Execute(command);

        public Task<TResult> ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellation) where TCommand : IAsyncCommand<TResult>
            => services.GetRequiredService<IAsyncCommandHandler<TCommand, TResult>>().ExecuteAsync(command, cancellation);
    }

    #region Event Helpers

    class CompositeObservable<T> : IObservable<T>
    {
        readonly IObservable<T>[] observables;

        public CompositeObservable(IObservable<T>[] observables)
            => this.observables = observables;

        public IDisposable Subscribe(IObserver<T> observer)
            => new CompositeDisposable(observables
                .Select(observable => observable.Subscribe(observer)).ToArray());
    }

    #endregion

    #region Command Dispatchers

    // In order for the caller to avoid having to specify both the command 
    // type and the result type when executing, we resort to inferring the 
    // latter from the former. But in order for this to work, we need to 
    // introduce an intermediary "dispatcher" which is typed to the actual 
    // command instance received in the various Execute* overloads. 
    // 
    // This might seem like unnecessary complexity, but it actually produces 
    // a very performant invocation pattern (no reflection, lazy instantiation 
    // of the executors per command-type) while keeping the calling pattern on 
    // the command bus simple like:
    // 
    //     FooResult result = commandBus.Execute(new FooCommand());
    // 
    // Without the executors, inference wouldn't "just work" and both types 
    // (command and result) would need to be specified, as in:
    // 
    //     FooResult result = commandBus.Execute&lt;FooCommand, FooResult&gt;(new FooCommand());
    // 
    // which is quite awful.
    // 
    // Each of the four invocation styles gets its own non-generic executor, so that the 
    // Execute overloads can invoke them with the generic command argument, as well as the 
    // generic implementations that contain the actual invocation and downcast.

    abstract class VoidDispatcher
    {
        public abstract void Execute(IExecutable command);
    }

    class VoidDispatcher<TCommand> : VoidDispatcher where TCommand : ICommand
    {
        readonly IServiceProvider services;

        public VoidDispatcher(IServiceProvider services)
            => this.services = services;

        public override void Execute(IExecutable command)
            => services.GetRequiredService<ICommandHandler<TCommand>>().Execute((TCommand)command);
    }

    abstract class ResultDispatcher
    {
        public abstract object? Execute(IExecutable command);
    }

    class ResultDispatcher<TCommand, TResult> : ResultDispatcher where TCommand : ICommand<TResult>
    {
        readonly IServiceProvider services;

        public ResultDispatcher(IServiceProvider services)
            => this.services = services;

        public override object? Execute(IExecutable command)
            => services.GetRequiredService<ICommandHandler<TCommand, TResult>>().Execute((TCommand)command);
    }

    abstract class VoidAsyncDispatcher
    {
        public abstract Task ExecuteAsync(IExecutable command, CancellationToken cancellation);
    }

    class VoidAsyncDispatcher<TCommand> : VoidAsyncDispatcher where TCommand : IAsyncCommand
    {
        readonly IServiceProvider services;

        public VoidAsyncDispatcher(IServiceProvider services)
            => this.services = services;

        public override Task ExecuteAsync(IExecutable command, CancellationToken cancellation)
            => services.GetRequiredService<IAsyncCommandHandler<TCommand>>().ExecuteAsync((TCommand)command, cancellation);
    }

    abstract class ResultAsyncDispatcher
    {
        public abstract object ExecuteAsync(IExecutable command, CancellationToken cancellation);
    }

    class ResultAsyncDispatcher<TCommand, TResult> : ResultAsyncDispatcher where TCommand : IAsyncCommand<TResult>
    {
        readonly IServiceProvider services;

        public ResultAsyncDispatcher(IServiceProvider services)
            => this.services = services;

        public override object ExecuteAsync(IExecutable command, CancellationToken cancellation)
            => services.GetRequiredService<IAsyncCommandHandler<TCommand, TResult>>().ExecuteAsync((TCommand)command, cancellation);
    }

    #endregion
}
