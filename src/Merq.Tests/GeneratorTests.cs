using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Merq.Sample;
using Microsoft.Extensions.DependencyInjection;

namespace Merq;

public record GeneratorTests(ITestOutputHelper Output)
{
    [Fact]
    public void CanRegisterProjectReferenceHandler()
    {
        var collection = new ServiceCollection();

        collection.AddMessageBus();

        var services = collection.BuildServiceProvider();

        services.GetRequiredService<IMessageBus>().Execute(new MessageCommand("Hello"));
    }

    [Fact]
    public void CanRegisterScopedHandlers()
    {
        var collection = new ServiceCollection();

        collection.AddMessageBus();

        var services = collection.BuildServiceProvider();

        Assert.NotNull(services.GetRequiredService<ScopedService>());
        Assert.NotNull(services.GetRequiredService<ICommandHandler<ScopedCommand>>());
        Assert.NotNull(services.GetRequiredService<ICanExecute<ScopedCommand>>());
        Assert.NotNull(services.GetRequiredService<IExecutableCommandHandler<ScopedCommand>>());
    }

    [Fact]
    public void CanRegisterTransientHandlers()
    {
        var collection = new ServiceCollection();

        collection.AddMessageBus();

        var services = collection.BuildServiceProvider();

        var first = services.GetRequiredService<TransientService>();
        var second = services.GetRequiredService<TransientService>();

        Assert.NotSame(first, second);
    }

    [Fact]
    public void CanRegisterSingletonHandlers()
    {
        var collection = new ServiceCollection();

        collection.AddMessageBus();

        var services = collection.BuildServiceProvider();

        var first = services.GetRequiredService<SingletonService>();
        var second = services.GetRequiredService<SingletonService>();
        var handler = services.GetRequiredService<ICommandHandler<SingletonCommand, string>>();
        var executable = services.GetRequiredService<IExecutableCommandHandler<SingletonCommand, string>>();
        var can = services.GetRequiredService<ICanExecute<SingletonCommand>>();

        Assert.Same(first, second);
        Assert.Same(first, handler);
        Assert.Same(first, executable);
        Assert.Same(first, can);
    }

    [Fact]
    public void CanRegisterObservableEvent()
    {
        var collection = new ServiceCollection();

        collection.AddMessageBus();

        var services = collection.BuildServiceProvider();

        Assert.NotNull(services.GetService<IObservable<ConcreteEvent>>());
        Assert.NotNull(services.GetService<IObservable<BaseEvent>>());
        Assert.NotNull(services.GetService<IObservable<IBaseEvent>>());
        Assert.Null(services.GetService<IObservable<object>>());

        var bus = services.GetService<IMessageBus>();
        IBaseEvent? data = null;
        bus!.Observe<IBaseEvent>().Subscribe(e => data = e);

        var producer = services.GetService<IObserver<ConcreteEvent>>();
        producer!.OnNext(new ConcreteEvent());

        Assert.NotNull(data);
    }
}

record ScopedCommand : ICommand { }

[Service(ServiceLifetime.Scoped)]
class ScopedService : ICommandHandler<ScopedCommand>
{
    public bool CanExecute(ScopedCommand command) => throw new System.NotImplementedException();
    public void Execute(ScopedCommand command) => throw new System.NotImplementedException();
}

record SingletonCommand : ICommand<string> { }

[Service(ServiceLifetime.Singleton)]
class SingletonService : ICommandHandler<SingletonCommand, string>
{
    public bool CanExecute(SingletonCommand command) => throw new System.NotImplementedException();
    public string Execute(SingletonCommand command) => throw new System.NotImplementedException();
}

record TransientCommand : IAsyncCommand { }

[Service(ServiceLifetime.Transient)]
class TransientService : IAsyncCommandHandler<TransientCommand>
{
    public bool CanExecute(TransientCommand command) => throw new System.NotImplementedException();
    public Task ExecuteAsync(TransientCommand command, CancellationToken cancellation) => throw new System.NotImplementedException();
}

record TransientResultCommand : IAsyncCommand<string> { }

[Service(ServiceLifetime.Transient)]
class TransientService2 : IAsyncCommandHandler<TransientResultCommand, string>
{
    public bool CanExecute(TransientResultCommand command) => throw new System.NotImplementedException();
    public Task<string> ExecuteAsync(TransientResultCommand command, CancellationToken cancellation) => throw new System.NotImplementedException();
}

[Service(ServiceLifetime.Transient)]
class TransientService3
{
}

[Service]
public class ObservableService : IObservable<ConcreteEvent>, IObserver<ConcreteEvent>
{
    Subject<ConcreteEvent> subject = new();

    public void OnCompleted() => subject.OnCompleted();
    public void OnError(Exception error) => subject.OnError(error);
    public void OnNext(ConcreteEvent value) => subject.OnNext(value);
    public IDisposable Subscribe(IObserver<ConcreteEvent> observer) => subject.Subscribe(observer);
}