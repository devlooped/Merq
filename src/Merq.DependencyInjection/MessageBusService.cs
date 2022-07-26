using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Merq;

class MessageBusService : IMessageBus
{
    // All subjects active in the event stream.
    readonly ConcurrentDictionary<Type, Subject> subjects = new();
    // An cache of subjects indexed by the compatible event types, used to quickly lookup the subjects to 
    // invoke in a Push. Refreshed whenever a new Of<T> subscription is added.
    readonly ConcurrentDictionary<Type, Subject[]> compatibleSubjects = new();

    readonly IServiceProvider services;

    public MessageBusService(IServiceProvider services) => this.services = services;

    public bool CanExecute<TCommand>(TCommand command) where TCommand : IExecutable => throw new NotImplementedException();
    public bool CanHandle<TCommand>() where TCommand : IExecutable => throw new NotImplementedException();
    public bool CanHandle(IExecutable command) => throw new NotImplementedException();
    public void Execute(ICommand command) => throw new NotImplementedException();
    public TResult? Execute<TResult>(ICommand<TResult> command) => throw new NotImplementedException();
    public Task ExecuteAsync(IAsyncCommand command, CancellationToken cancellation) => throw new NotImplementedException();
    public Task<TResult> ExecuteAsync<TResult>(IAsyncCommand<TResult> command, CancellationToken cancellation) => throw new NotImplementedException();

    public void Notify<TEvent>(TEvent e)
    {
        var type = (e ?? throw new ArgumentNullException(nameof(e))) .GetType();
        var producer = services.GetService<IObservable<TEvent>>();

        if (producer != null)
            throw new NotSupportedException($"Cannot explicitly notify event {type} because it is externally produced by {producer.GetType()}.");

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
        var producers = services.GetServices<IObservable<TEvent>>().ToArray();
        var subject = (IObservable<TEvent>)subjects.GetOrAdd(typeof(TEvent), _ =>
        {
            // If we're creating a new subject, we need to clear the cache of compatible subjects
            compatibleSubjects.Clear();
            return new Subject<TEvent>();
        });

        if (producers.Length == 0)
            return subject;

        return new CompositeObservable<TEvent>(new[] { subject }.Concat(producers).ToArray());
    }

    abstract class Subject
    {
        public abstract void OnNext(object value);
    }

    class Subject<T> : Subject, IObservable<T>
    {
        readonly ConcurrentDictionary<IObserver<T>, object> observers = new();

        public override void OnNext(object value)
        {
            var next = (T)value;

            foreach (var item in observers.Keys.ToArray())
            {
                // Don't let misbehaving subscribers prevent calling the others
                try
                {
                    item.OnNext(next);
                }
                catch (Exception ex)
                {
                    // Flag them and remove them to avoid perf. issues from 
                    // constantly throwing subscribers.
                    item.OnError(ex);
                    observers.TryRemove(item, out _);
                }
            }
        }

        IDisposable IObservable<T>.Subscribe(IObserver<T> observer)
            => new Subscription(this, observer);

        class Subscription : IDisposable
        {
            readonly Subject<T> subject;
            readonly IObserver<T> observer;

            public Subscription(Subject<T> subject, IObserver<T> observer)
            {
                this.subject = subject;
                this.observer = observer;
                subject.observers.TryAdd(observer, observer);
            }

            public void Dispose() => subject.observers.TryRemove(observer, out _);
        }
    }

    class CompositeObservable<T> : IObservable<T>
    {
        readonly IObservable<T>[] observables;

        public CompositeObservable(IObservable<T>[] observables)
            => this.observables = observables;

        public IDisposable Subscribe(IObserver<T> observer)
            => new CompositeDisposable(observables
                .Select(observable => observable.Subscribe(observer)).ToArray());
    }
}
