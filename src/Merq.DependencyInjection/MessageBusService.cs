using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Merq;

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
///     FooResult result = messageBus.Execute(new FooCommand());
/// 
/// Without the executors, inference wouldn't "just work" and both types 
/// (command and result) would need to be specified, as in:
/// 
///     FooResult result = messageBus.Execute&lt;FooCommand, FooResult&gt;(new FooCommand());
/// 
/// which is quite awful.
/// 
/// Each of the four invocation styles gets its own non-generic executor, so that the 
/// Execute overloads can invoke them with the generic command argument, as well as the 
/// generic implementations that contain the actual invocation and downcast.
/// </devdoc>
class MessageBusService : IMessageBus
{
    static readonly MethodInfo canHandleMethod = typeof(MessageBusService).GetMethod(nameof(CanHandle), Type.EmptyTypes);

    // All subjects active in the event stream.
    readonly ConcurrentDictionary<Type, Subject> subjects = new();
    // An cache of subjects indexed by the compatible event types, used to quickly lookup the subjects to 
    // invoke in a Push. Refreshed whenever a new Of<T> subscription is added.
    readonly ConcurrentDictionary<Type, Subject[]> compatibleSubjects = new();

    readonly ConcurrentDictionary<Type, bool> canHandleMap = new();

    readonly ConcurrentDictionary<Type, VoidExecutor> voidExecutors = new();
    readonly ConcurrentDictionary<Type, VoidAsyncExecutor> voidAsyncExecutors = new();
    readonly ConcurrentDictionary<Type, ResultExecutor> resultExecutors = new();
    readonly ConcurrentDictionary<Type, ResultAsyncExecutor> resultAsyncExecutors = new();

    readonly IServiceProvider services;

    public MessageBusService(IServiceProvider services) => this.services = services;

    public bool CanExecute<TCommand>(TCommand command) where TCommand : IExecutable
        => CanHandle<TCommand>() &&
           services.GetRequiredService<IExecutableCommandHandler<TCommand>>() is ICanExecute<TCommand> canExec &&
           canExec.CanExecute(command);

    public bool CanHandle<TCommand>() where TCommand : IExecutable
        => canHandleMap.GetOrAdd(typeof(TCommand), type
            => services.GetService<IExecutableCommandHandler<TCommand>>() != null);

    public bool CanHandle(IExecutable command)
        => canHandleMap.GetOrAdd(command.GetType(), type
            => (bool)canHandleMethod.MakeGenericMethod(type).Invoke(this, null));

    public void Execute(ICommand command)
        => voidExecutors.GetOrAdd(
            GetCommandType(command),
            type => (VoidExecutor)Activator.CreateInstance(
                typeof(VoidExecutor<>).MakeGenericType(type),
                services))
            .Execute(command);

    public TResult? Execute<TResult>(ICommand<TResult> command)
        => (TResult)resultExecutors.GetOrAdd(
            GetCommandType(command),
            type => (ResultExecutor)Activator.CreateInstance(
                typeof(ResultExecutor<,>).MakeGenericType(type, typeof(TResult)),
                services))
            .Execute(command)!;

    public Task ExecuteAsync(IAsyncCommand command, CancellationToken cancellation)
        => voidAsyncExecutors.GetOrAdd(
            GetCommandType(command),
            type => (VoidAsyncExecutor)Activator.CreateInstance(
                typeof(VoidAsyncExecutor<>).MakeGenericType(type),
                services))
            .ExecuteAsync(command, cancellation);

    public Task<TResult> ExecuteAsync<TResult>(IAsyncCommand<TResult> command, CancellationToken cancellation)
        => (Task<TResult>)resultAsyncExecutors.GetOrAdd(
            GetCommandType(command),
            type => (ResultAsyncExecutor)Activator.CreateInstance(
                typeof(ResultAsyncExecutor<,>).MakeGenericType(type, typeof(TResult)),
                services))
            .ExecuteAsync(command, cancellation);

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
        var compatible = compatibleSubjects.GetOrAdd(type, eventType => subjects.Keys
            .Where(subjectEventType => subjectEventType.IsAssignableFrom(eventType))
            .Select(subjectEventType => subjects[subjectEventType])
            .ToArray());

        foreach (var subject in compatible)
        {
            subject.OnNext(e);
        }
    }

    public IObservable<TEvent> Observe<TEvent>()
    {
        // NOTE: in order for the base event subscription to work properly for external
        // producers, they must register the service for each T in the TEvent hierarchy.
        var producers = services.GetServices<IObservable<TEvent>>().ToArray();
        
        var subject = (IObservable<TEvent>)subjects.GetOrAdd(typeof(TEvent).GetTypeInfo(), info =>
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

    static Type GetCommandType(IExecutable command)
        => command?.GetType() ?? throw new ArgumentNullException(nameof(command));

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

    #region Command Executors

    abstract class VoidExecutor
    {
        public abstract void Execute(IExecutable command);
    }

    class VoidExecutor<TCommand> : VoidExecutor where TCommand : ICommand
    {
        readonly IServiceProvider services;

        public VoidExecutor(IServiceProvider services)
            => this.services = services;

        public override void Execute(IExecutable command)
            => services.GetRequiredService<ICommandHandler<TCommand>>().Execute((TCommand)command);
    }

    abstract class ResultExecutor
    {
        public abstract object? Execute(IExecutable command);
    }

    class ResultExecutor<TCommand, TResult> : ResultExecutor where TCommand : ICommand<TResult>
    {
        readonly IServiceProvider services;

        public ResultExecutor(IServiceProvider services)
            => this.services = services;

        public override object? Execute(IExecutable command)
            => services.GetRequiredService<ICommandHandler<TCommand, TResult>>().Execute((TCommand)command);
    }

    abstract class VoidAsyncExecutor
    {
        public abstract Task ExecuteAsync(IExecutable command, CancellationToken cancellation);
    }

    class VoidAsyncExecutor<TCommand> : VoidAsyncExecutor where TCommand : IAsyncCommand
    {
        readonly IServiceProvider services;

        public VoidAsyncExecutor(IServiceProvider services)
            => this.services = services;

        public override Task ExecuteAsync(IExecutable command, CancellationToken cancellation)
            => services.GetRequiredService<IAsyncCommandHandler<TCommand>>().ExecuteAsync((TCommand)command, cancellation);
    }

    abstract class ResultAsyncExecutor
    {
        public abstract object ExecuteAsync(IExecutable command, CancellationToken cancellation);
    }

    class ResultAsyncExecutor<TCommand, TResult> : ResultAsyncExecutor where TCommand : IAsyncCommand<TResult>
    {
        readonly IServiceProvider services;

        public ResultAsyncExecutor(IServiceProvider services)
            => this.services = services;

        public override object ExecuteAsync(IExecutable command, CancellationToken cancellation)
            => services.GetRequiredService<IAsyncCommandHandler<TCommand, TResult>>().ExecuteAsync((TCommand)command, cancellation);
    }

    #endregion
}
