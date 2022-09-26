using System;
using System.Management.Instrumentation;
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
        Assert.NotNull(services.GetRequiredService<ICommandHandler<Command>>());
        Assert.NotNull(services.GetRequiredService<ICanExecute<Command>>());
        Assert.NotNull(services.GetRequiredService<IExecutableCommandHandler<Command>>());
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
        var handler = services.GetRequiredService<ICommandHandler<CommandWithResult, Result>>();
        var executable = services.GetRequiredService<IExecutableCommandHandler<CommandWithResult, Result>>();
        var can = services.GetRequiredService<ICanExecute<CommandWithResult>>();

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

[Service(ServiceLifetime.Scoped)]
class ScopedService : ICommandHandler<Command>
{
    public bool CanExecute(Command command) => throw new System.NotImplementedException();
    public void Execute(Command command) => throw new System.NotImplementedException();
}


[Service(ServiceLifetime.Singleton)]
class SingletonService : ICommandHandler<CommandWithResult, Result>
{
    public bool CanExecute(CommandWithResult command) => throw new System.NotImplementedException();
    public Result Execute(CommandWithResult command) => throw new System.NotImplementedException();
}

[Service(ServiceLifetime.Transient)]
class TransientService : IAsyncCommandHandler<AsyncCommand>
{
    public bool CanExecute(AsyncCommand command) => throw new System.NotImplementedException();
    public Task ExecuteAsync(AsyncCommand command, CancellationToken cancellation) => throw new System.NotImplementedException();
}

[Service(ServiceLifetime.Transient)]
class TransientService2 : IAsyncCommandHandler<AsyncCommandWithResult, Result>
{
    public bool CanExecute(AsyncCommandWithResult command) => throw new System.NotImplementedException();
    public Task<Result> ExecuteAsync(AsyncCommandWithResult command, CancellationToken cancellation) => throw new System.NotImplementedException();
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