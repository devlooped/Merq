using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

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
    static readonly MethodInfo canExecuteMethod = typeof(MessageBus).GetMethod(nameof(CanExecute));

    // All subjects active in the event stream.
    readonly ConcurrentDictionary<Type, Subject> subjects = new();
    // An cache of subjects indexed by the compatible event types, used to quickly lookup the subjects to 
    // invoke in a Notify. Refreshed whenever a new Observe<T> subscription is added.
    readonly ConcurrentDictionary<Type, Subject[]> compatibleSubjects = new();
    // An cache of subjects indexed by event full name, used to quickly lookup the subjects to 
    // invoke in a Notify via a factory to perform conversion. 
    readonly ConcurrentDictionary<string, ConcurrentDictionary<Type, Subject?>> dynamicSubjects = new();

    readonly ConcurrentDictionary<Type, bool> canHandleMap = new();
    readonly ConcurrentDictionary<Type, ServiceDescriptor?> mappedHandlers = new();
    readonly ConcurrentDictionary<Type, (Type TargetCommand, Func<dynamic, object> CommandFactory)?> mappedCommands = new();

    readonly IServiceProvider services;
    readonly IServiceCollection? collection;
    readonly bool enableDynamicMapping;

    readonly ConcurrentDictionary<Type, VoidDispatcher> voidExecutors = new();
    readonly ConcurrentDictionary<Type, VoidAsyncDispatcher> voidAsyncExecutors = new();
    readonly ConcurrentDictionary<Type, ResultDispatcher> resultExecutors = new();
    readonly ConcurrentDictionary<Type, ResultAsyncDispatcher> resultAsyncExecutors = new();

    /// <summary>
    /// Instantiates the message bus with the given <see cref="IServiceProvider"/> 
    /// that resolves instances of command handlers and external event producers.
    /// </summary>
    public MessageBus(IServiceProvider services) : this(services, true) { }

    /// <summary>
    /// Instantiates the message bus with the given <see cref="IServiceProvider"/> 
    /// that resolves instances of command handlers and external event producers.
    /// </summary>
    /// <param name="services">The <see cref="IServiceProvider"/> that contains the registrations for 
    /// command handlers and event producers.
    /// </param>
    /// <param name="enableDynamicMapping">Whether to support dynamic mapping for command and event type (a.k.a. 'duck typing').</param>
    protected MessageBus(IServiceProvider services, bool enableDynamicMapping)
    {
        this.services = services;
        if (enableDynamicMapping)
            // This allows MEF-based services to not throw when we request a non-required service.
            collection = services.GetServices<IServiceCollection>().FirstOrDefault();

        this.enableDynamicMapping = enableDynamicMapping;
    }

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
    {
        var type = GetCommandType(command);
        if (services.GetService(GetHandlerType(type)) is ICanExecute<TCommand> canExec)
            return canExec.CanExecute(command);

        // See if we can convert from the TCommand to a compatible type with 
        // a registered command handler
        if (FindCommandMapper(type, out var commandType) is Func<dynamic, object> factory &&
            commandType is not null &&
            factory.Invoke(command) is IExecutable converted)
        {
            if (commandType.IsPublic)
                // For public types, we can use the faster dynamic dispatch approach
                return CanExecute((dynamic)converted);
            else
                // Cache this as a delegate too?
                // For non-public types, we need to use the slower reflection approach
                return (bool?)canExecuteMethod.MakeGenericMethod(commandType).Invoke(this, new[] { converted }) == true;
        }

        return false;
    }

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
    public bool CanHandle<TCommand>() where TCommand : IExecutable => canHandleMap.GetOrAdd(typeof(TCommand), type =>
    {
        var handler = services.GetService(GetHandlerType(type));
        if (handler != null)
            return true;

        return FindCommandMapper(type, out _) is not null;
    });

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
    public bool CanHandle(IExecutable command) => canHandleMap.GetOrAdd(GetCommandType(command), type =>
    {
        var handler = services.GetService(GetHandlerType(type));
        if (handler != null)
            return true;

        return FindCommandMapper(type, out _) is not null;
    });

    /// <summary>
    /// Executes the given synchronous command.
    /// </summary>
    /// <param name="command">The command parameters for the execution.</param>
    public void Execute(ICommand command)
    {
        var type = GetCommandType(command);
        if (type.IsPublic)
            // For public types, we can use the faster dynamic dispatch approach
            ExecuteCore((dynamic)command);
        else
            voidExecutors.GetOrAdd(type, type
                => (VoidDispatcher)Activator.CreateInstance(
                    typeof(VoidDispatcher<>).MakeGenericType(type),
                    this))
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
                   this))
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
            return ExecuteAsyncCore((dynamic)command, cancellation);

        return voidAsyncExecutors.GetOrAdd(type, type
            => (VoidAsyncDispatcher)Activator.CreateInstance(
                typeof(VoidAsyncDispatcher<>).MakeGenericType(type),
                this))
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
                this))
            .ExecuteAsync(command, cancellation);
    }

    /// <summary>
    /// Notifies the bus of an event.
    /// </summary>
    public void Notify<TEvent>(TEvent e)
    {
        var type = (e ?? throw new ArgumentNullException(nameof(e))).GetType();

        // TODO: if we prevent Notify for externally produced events, we won't be 
        // able to notify base event subscribers when those events are produced. 
        //var producer = services.GetService<IObservable<TEvent>>();
        //if (producer != null)
        //    throw new NotSupportedException($"Cannot explicitly notify event {type} because it is externally produced by {producer.GetType()}.");

        // We call all subjects that are compatible with
        // the event type, not just concrete event type subscribers.
        // Also adds as compatible the dynamic conversion ones.
        var compatible = compatibleSubjects.GetOrAdd(type, eventType => subjects.Keys
            .Where(subjectEventType => subjectEventType.IsAssignableFrom(eventType))
            .Select(subjectEventType => subjects[subjectEventType])
            .Concat(dynamicSubjects
                .GetOrAdd(type.FullName, _ => new())
                .Where(pair => pair.Key != type && pair.Value != null)
                .Select(pair => pair.Value!))
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
        var eventType = typeof(TEvent);

        // NOTE: in order for the base event subscription to work properly for external
        // producers, they must register the service for each T in the TEvent hierarchy.
        var producers = services.GetServices<IObservable<TEvent>>().ToArray();

        var typedSubject = (IObservable<TEvent>)subjects.GetOrAdd(eventType, type =>
        {
            // If we're creating a new subject, we need to clear the cache of compatible subjects
            compatibleSubjects.Clear();
            return new Subject<TEvent>();
        });

        var dynamicSubject = enableDynamicMapping ? (Subject<TEvent>?)dynamicSubjects.GetOrAdd(eventType.FullName, _ => new()).GetOrAdd(eventType, type =>
        {
            var factory = FindFactory(type);
            if (factory != null)
            {
                // If we're creating a new subject, we need to clear the cache of compatible subjects
                compatibleSubjects.Clear();
                return new Subject<TEvent>((Func<dynamic, TEvent>)Delegate.CreateDelegate(typeof(Func<dynamic, TEvent>), factory, true));
            }

            // The null value will be added so we don't check the assembly for the factory delegate again.
            return null;
        }) : null;

        if (producers.Length == 0)
        {
            if (dynamicSubject == null)
                return typedSubject;
            else
                return new CompositeObservable<TEvent>(typedSubject, dynamicSubject);
        }

        // Merge with any externally-produced observables that are compatible
        if (dynamicSubject == null)
            return new CompositeObservable<TEvent>(new[] { typedSubject }.Concat(producers).ToArray());
        else
            return new CompositeObservable<TEvent>(new[] { typedSubject, dynamicSubject }.Concat(producers).ToArray());
    }

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

    // Tries to locate a command handler service, which by definition will implement 
    // IExecutableCommandHandler<TCommand>, so we look up a matching TCommand by 
    // full name against the type parameter. If we find such a handler, we then need 
    // to locate an appropriate conversion factory method. If neither is found, we 
    // cannot map.
    Func<dynamic, object>? FindCommandMapper(Type sourceType, out Type? targetType)
    {
        targetType = null;
        if (collection == null)
            return null;

        var map = mappedCommands.GetOrAdd(sourceType, type =>
        {
            // This is obviously not a cheap lookup, but we do it once per bus per type
            if (collection.FirstOrDefault(x =>
                x.ServiceType.IsGenericType &&
                x.ServiceType.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IExecutableCommandHandler<>) &&
                    i.GetGenericArguments()[0] is Type argType &&
                    argType.FullName == type.FullName)) is ServiceDescriptor descriptor &&
                descriptor.ServiceType.GetGenericArguments()[0] is Type commandType &&
                FindFactory(commandType) is MethodInfo factory)
                return (commandType, (Func<dynamic, object>)Delegate.CreateDelegate(typeof(Func<dynamic, object>), null, factory, true));

            return default;
        });

        targetType = map?.TargetCommand;
        return map?.CommandFactory;
    }

    void ExecuteCore<TCommand>(TCommand command) where TCommand : ICommand
    {
        var handler = services.GetService<ICommandHandler<TCommand>>();
        if (handler != null)
        {
            handler.Execute(command);
            return;
        }

        // See if we can convert from the TCommand to a compatible type with 
        // a registered command handler
        if (FindCommandMapper(typeof(TCommand), out var commandType) is Func<dynamic, object> factory &&
            commandType is not null &&
            factory.Invoke(command) is ICommand converted)
        {
            Execute(converted);
            return;
        }

        throw new InvalidOperationException($"No service for type '{typeof(ICommandHandler<TCommand>)}' has been registered.");
    }

    Task ExecuteAsyncCore<TCommand>(TCommand command, CancellationToken cancellation) where TCommand : IAsyncCommand
    {
        var handler = services.GetService<IAsyncCommandHandler<TCommand>>();
        if (handler != null)
        {
            return handler.ExecuteAsync(command, cancellation);
        }

        // See if we can convert from the TCommand to a compatible type with 
        // a registered command handler
        if (FindCommandMapper(typeof(TCommand), out var commandType) is Func<dynamic, object> factory &&
            commandType is not null &&
            factory.Invoke(command) is IAsyncCommand converted)
        {
            return ExecuteAsync(converted, cancellation);
        }

        throw new InvalidOperationException($"No service for type '{typeof(IAsyncCommandHandler<TCommand>)}' has been registered.");
    }

    TResult ExecuteCore<TCommand, TResult>(TCommand command) where TCommand : ICommand<TResult>
    {
        var handler = services.GetService<ICommandHandler<TCommand, TResult>>();
        if (handler != null)
            return handler.Execute(command);

        // See if we can convert from the TCommand to a compatible type with 
        // a registered command handler
        if (FindCommandMapper(typeof(TCommand), out var commandType) is Func<dynamic, object> factory &&
            commandType is not null &&
            factory.Invoke(command) is ICommand<TResult> converted)
        {
            return Execute(converted);
        }

        throw new InvalidOperationException($"No service for type '{typeof(ICommandHandler<TCommand, TResult>)}' has been registered.");
    }

    Task<TResult> ExecuteAsyncCore<TCommand, TResult>(TCommand command, CancellationToken cancellation) where TCommand : IAsyncCommand<TResult>
    {
        var handler = services.GetService<IAsyncCommandHandler<TCommand, TResult>>();
        if (handler != null)
            return handler.ExecuteAsync(command, cancellation);

        // See if we can convert from the TCommand to a compatible type with 
        // a registered command handler
        if (FindCommandMapper(typeof(TCommand), out var commandType) is Func<dynamic, object> factory &&
            commandType is not null &&
            factory.Invoke(command) is IAsyncCommand<TResult> converted)
        {
            return ExecuteAsync(converted, cancellation);
        }

        throw new InvalidOperationException($"No service for type '{typeof(IAsyncCommandHandler<TCommand, TResult>)}' has been registered.");
    }

    /// <summary>
    /// Locates either a generated or custom factory method that can perform conversion from one 
    /// type to another of the same shape.
    /// </summary>
    MethodInfo? FindFactory(Type type)
    {
        if (type.GetMethod("Create", BindingFlags.Public | BindingFlags.Static) is MethodInfo createMethod &&
            createMethod.GetParameters().Length == 1 &&
            createMethod.ReturnType == type &&
            createMethod.GetParameters()[0].ParameterType == typeof(object))
        {
            return createMethod;
        }

        // else, if there's a converter in the event type assembly, use that to provide dynamic 
        // conversion support.
        if (type.Assembly.GetType($"{type.Namespace}.__{type.Name}Factory") is Type factoryType &&
            factoryType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static) is MethodInfo factoryMethod &&
            factoryMethod.ReturnType == type)
        {
            return factoryMethod;
        }

        return null;
    }

    // dynamic dispatch cannot infer TResult from TCommand, so we need to use a generic method
    // that first "sets" the TResult and then use dynamic dispatch on the resulting instance.
    With<TResult> WithResult<TResult>() => new(this);

    readonly struct With<TResult>
    {
        readonly MessageBus bus;

        public With(MessageBus bus) => this.bus = bus;

        public TResult Execute<TCommand>(TCommand command) where TCommand : ICommand<TResult>
            => bus.ExecuteCore<TCommand, TResult>(command);

        public Task<TResult> ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellation) where TCommand : IAsyncCommand<TResult>
            => bus.ExecuteAsyncCore<TCommand, TResult>(command, cancellation);
    }

    #region Event Helpers

    class CompositeObservable<T> : IObservable<T>
    {
        readonly IObservable<T>[] observables;

        public CompositeObservable(params IObservable<T>[] observables)
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
        readonly MessageBus bus;

        public VoidDispatcher(MessageBus bus) => this.bus = bus;

        public override void Execute(IExecutable command) => bus.ExecuteCore((TCommand)command);
    }

    abstract class ResultDispatcher
    {
        public abstract object? Execute(IExecutable command);
    }

    class ResultDispatcher<TCommand, TResult> : ResultDispatcher where TCommand : ICommand<TResult>
    {
        readonly MessageBus bus;

        public ResultDispatcher(MessageBus bus) => this.bus = bus;

        public override object? Execute(IExecutable command) => bus.ExecuteCore<TCommand, TResult>((TCommand)command);
    }

    abstract class VoidAsyncDispatcher
    {
        public abstract Task ExecuteAsync(IExecutable command, CancellationToken cancellation);
    }

    class VoidAsyncDispatcher<TCommand> : VoidAsyncDispatcher where TCommand : IAsyncCommand
    {
        readonly MessageBus bus;

        public VoidAsyncDispatcher(MessageBus bus) => this.bus = bus;

        public override Task ExecuteAsync(IExecutable command, CancellationToken cancellation) => bus.ExecuteAsyncCore((TCommand)command, cancellation);
    }

    abstract class ResultAsyncDispatcher
    {
        public abstract object ExecuteAsync(IExecutable command, CancellationToken cancellation);
    }

    class ResultAsyncDispatcher<TCommand, TResult> : ResultAsyncDispatcher where TCommand : IAsyncCommand<TResult>
    {
        readonly MessageBus bus;

        public ResultAsyncDispatcher(MessageBus bus) => this.bus = bus;

        public override object ExecuteAsync(IExecutable command, CancellationToken cancellation)
            => bus.ExecuteAsyncCore<TCommand, TResult>((TCommand)command, cancellation);
    }

    #endregion
}
