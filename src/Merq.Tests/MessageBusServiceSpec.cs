using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Merq;

public record MessageBusServiceSpec(ITestOutputHelper Output)
{
    IMessageBus bus = new ServiceCollection()
        .AddMessageBus(false)
        .BuildServiceProvider()
        .GetRequiredService<IMessageBus>();

    [Fact]
    public void when_subscribing_external_producer_then_succeeds()
    {
        var producer = new Subject<int>();
        var collection = new ServiceCollection()
            .AddMessageBus(false)
            .AddSingleton<IObservable<int>>(producer);

        var services = collection.BuildServiceProvider();
        var bus = services.GetRequiredService<IMessageBus>();
        
        int? value = default;

        bus.Observe<int>().Subscribe(i => value = i);

        producer.OnNext(42);

        Assert.Equal(42, value);
    }

    [Fact]
    public void given_external_producer_when_subscribing_to_base_then_notifies()
    {
        var producer = new Subject<ConcreteEvent>();

        var bus = new ServiceCollection()
            .AddMessageBus(false)
            .AddSingleton<IObservable<ConcreteEvent>>(producer)
            .AddSingleton<IObservable<BaseEvent>>(producer)
            .BuildServiceProvider()
            .GetRequiredService<IMessageBus>();

        var expected = new ConcreteEvent();
        BaseEvent? actual = default;

        bus.Observe<BaseEvent>().Subscribe(e => actual = e);

        producer.OnNext(expected);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void given_external_producer_then_can_notify_event()
    {
        var producer = new Subject<ConcreteEvent>();

        var bus = new ServiceCollection()
            .AddMessageBus(false)
            .AddSingleton<IObservable<ConcreteEvent>>(producer)
            .AddSingleton<IObservable<BaseEvent>>(producer)
            .BuildServiceProvider()
            .GetRequiredService<IMessageBus>();

        var expected = new ConcreteEvent();
        ConcreteEvent? actual = default;

        bus.Observe<ConcreteEvent>().Subscribe(e => actual = e);

        bus.Notify(expected);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void when_subscribing_subject_then_succeeds()
    {
        int? value = default;
        bus.Observe<int>().Subscribe(i => value = i);

        bus.Notify(42);

        Assert.Equal(42, value);
    }

    [Fact]
    public void when_notifying_null_event_then_throws()
        => Assert.Throws<ArgumentNullException>(() => bus.Notify<object?>(null));

    [Fact]
    public void when_notifying_non_public_event_type_then_calls_subscriber()
    {
        var called = false;

        bus.Observe<NonPublicEvent>().Subscribe(x => called = true);

        bus.Notify(new NonPublicEvent());

        Assert.True(called);
    }

    [Fact]
    public void when_notifying_nested_non_public_event_type_then_calls_subscriber()
    {
        var called = false;

        bus.Observe<NestedEvent>().Subscribe(x => called = true);

        bus.Notify(new NestedEvent());

        Assert.True(called);
    }

    [Fact]
    public void when_notifying_non_subscribed_event_then_does_not_call_subscriber()
    {
        var called = false;

        using var subs = bus.Observe<ConcreteEvent>().Subscribe(c => called = true);

        bus.Notify(new AnotherEvent());

        Assert.False(called);
    }

    [Fact]
    public void when_notifying_subscribed_event_using_base_type_then_calls_derived_subscriber()
    {
        var called = false;
        using var subscription = bus.Observe<ConcreteEvent>().Subscribe(c => called = true);

        BaseEvent e = new ConcreteEvent();
        bus.Notify(e);

        Assert.True(called);
    }

    [Fact]
    public void when_subscribing_as_event_interface_then_calls_subscriber()
    {
        var called = false;
        using var subs = bus.Observe<IBaseEvent>().Subscribe(c => called = true);

        bus.Notify(new ConcreteEvent());

        Assert.True(called);
    }


    [Fact]
    public void given_two_observables_when_subscribing_base_event_then_receives_both()
    {
        var subject1 = new Subject<ConcreteEvent>();
        var subject2 = new Subject<AnotherEvent>();
        var bus = new ServiceCollection()
            .AddMessageBus(false)
            .AddSingleton<IObservable<ConcreteEvent>>(subject1)
            // NOTE: the producer needs to properly register as the base types too
            .AddSingleton<IObservable<BaseEvent>>(subject1)
            .AddSingleton(subject2)
            .AddSingleton<IObservable<AnotherEvent>>(subject2)
            .AddSingleton<IObservable<BaseEvent>>(subject2)
            .BuildServiceProvider()
            .GetRequiredService<IMessageBus>();

        var called = 0;

        using var subs = bus.Observe<BaseEvent>().Subscribe(c => called++);

        subject1.OnNext(new ConcreteEvent());
        subject2.OnNext(new AnotherEvent());

        Assert.Equal(2, called);
    }

    [Fact]
    public void when_executing_command_without_handler_then_throws()
    {
        Assert.False(bus.CanExecute(new Command()));
        Assert.False(bus.CanHandle<Command>());
        Assert.False(bus.CanHandle(new Command()));
        Assert.Throws<InvalidOperationException>(() => bus.Execute(new Command()));
    }

    [Fact]
    public void when_executing_command_with_result_without_handler_then_throws()
    {
        Assert.False(bus.CanExecute(new CommandWithResult()));
        Assert.False(bus.CanHandle<CommandWithResult>());
        Assert.False(bus.CanHandle(new CommandWithResult()));
        Assert.Throws<InvalidOperationException>(() => bus.Execute(new CommandWithResult()));
    }

    [Fact]
    public async Task when_executing_async_command_without_handler_then_throws()
    {
        Assert.False(bus.CanExecute(new AsyncCommand()));
        Assert.False(bus.CanHandle<AsyncCommand>());
        Assert.False(bus.CanHandle(new AsyncCommand()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.ExecuteAsync(new AsyncCommand(), CancellationToken.None));
    }

    [Fact]
    public async Task when_executing_async_command_with_result_without_handler_then_throws()
    {
        Assert.False(bus.CanExecute(new AsyncCommandWithResult()));
        Assert.False(bus.CanHandle<AsyncCommandWithResult>());
        Assert.False(bus.CanHandle(new AsyncCommandWithResult()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.ExecuteAsync(new AsyncCommandWithResult(), CancellationToken.None));
    }

    [Fact]
    public void when_can_handle_requested_for_registered_handler_then_returns_true()
    {
        var handler = Mock.Of<ICommandHandler<Command>>();

        var bus = new ServiceCollection()
            .AddMessageBus(false)
            .AddSingleton(handler)
            .AddSingleton<ICommandHandler<Command>>(handler)
            .BuildServiceProvider()
            .GetRequiredService<IMessageBus>();

        Assert.True(bus.CanHandle(new Command()));
    }

    [Fact]
    public void when_can_execute_requested_then_invokes_sync_handler()
    {
        var command = new Command();
        var handler = Mock.Of<ICommandHandler<Command>>(c => c.CanExecute(command) == true);

        var bus = new ServiceCollection()
            .AddMessageBus(false)
            .AddSingleton(handler)
            .AddSingleton<ICommandHandler<Command>>(handler)
            .BuildServiceProvider()
            .GetRequiredService<IMessageBus>();

        Assert.True(bus.CanExecute(command));
    }

    [Fact]
    public void when_can_execute_requested_then_invokes_async_handler()
    {
        var command = new AsyncCommand();
        var handler = Mock.Of<IAsyncCommandHandler<AsyncCommand>>(c => c.CanExecute(command) == true);

        var bus = new ServiceCollection()
            .AddMessageBus(false)
            .AddSingleton(handler)
            .AddSingleton<IAsyncCommandHandler<AsyncCommand>>(handler)
            .BuildServiceProvider()
            .GetRequiredService<IMessageBus>();

        Assert.True(bus.CanExecute(command));
    }

    [Fact]
    public void when_executing_sync_command_then_invokes_sync_handler()
    {
        var handler = new Mock<ICommandHandler<Command>>();
        var command = new Command();

        var bus = new ServiceCollection()
            .AddMessageBus(false)
            .AddSingleton(handler.Object)
            .AddSingleton<ICommandHandler<Command>>(handler.Object)
            .BuildServiceProvider()
            .GetRequiredService<IMessageBus>();

        bus.Execute(command);

        handler.Verify(x => x.Execute(command));
    }

    [Fact]
    public void when_executing_sync_command_then_invokes_sync_handler_with_result()
    {
        var handler = new Mock<ICommandHandler<CommandWithResult, Result>>();
        var command = new CommandWithResult();

        var bus = new ServiceCollection()
            .AddMessageBus(false)
            .AddSingleton(handler.Object)
            .AddSingleton<ICommandHandler<CommandWithResult, Result>>(handler.Object)
            .BuildServiceProvider()
            .GetRequiredService<IMessageBus>();

        bus.Execute(command);

        handler.Verify(x => x.Execute(command));
    }

    [Fact]
    public void when_executing_sync_command_with_result_then_invokes_sync_handler_with_result()
    {
        var handler = new Mock<ICommandHandler<CommandWithResult, Result>>();
        var command = new CommandWithResult();
        var expected = new Result();

        handler.Setup(x => x.Execute(command)).Returns(expected);

        var bus = new ServiceCollection()
            .AddMessageBus(false)
            .AddSingleton(handler.Object)
            .AddSingleton<ICommandHandler<CommandWithResult, Result>>(handler.Object)
            .BuildServiceProvider()
            .GetRequiredService<IMessageBus>();

        var result = bus.Execute(command);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task when_executing_async_command_then_invokes_async_handler()
    {
        var handler = new Mock<IAsyncCommandHandler<AsyncCommand>>();
        var command = new AsyncCommand();
        handler.Setup(x => x.ExecuteAsync(command, CancellationToken.None)).Returns(Task.FromResult(true));

        var bus = new ServiceCollection()
            .AddMessageBus(false)
            .AddSingleton(handler.Object)
            .AddSingleton<IAsyncCommandHandler<AsyncCommand>>(handler.Object)
            .BuildServiceProvider()
            .GetRequiredService<IMessageBus>();

        await bus.ExecuteAsync(command, CancellationToken.None);

        handler.Verify(x => x.ExecuteAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task when_executing_async_command_then_invokes_async_handler_with_result()
    {
        var handler = new Mock<IAsyncCommandHandler<AsyncCommandWithResult, Result>>();
        var command = new AsyncCommandWithResult();
        var result = new Result();
        handler.Setup(x => x.ExecuteAsync(command, CancellationToken.None)).Returns(Task.FromResult(result));
        
        var bus = new ServiceCollection()
            .AddMessageBus(false)
            .AddSingleton(handler.Object)
            .AddSingleton<IAsyncCommandHandler<AsyncCommandWithResult, Result>>(handler.Object)
            .BuildServiceProvider()
            .GetRequiredService<IMessageBus>();

        await bus.ExecuteAsync(command, CancellationToken.None);

        handler.Verify(x => x.ExecuteAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task when_execute_with_null_command_then_throwsAsync()
    {
        Assert.Throws<ArgumentNullException>(() => bus.Execute(default(Command)!));
        Assert.Throws<ArgumentNullException>(() => bus.Execute(default(CommandWithResult)!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => bus.ExecuteAsync(default(AsyncCommand)!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentNullException>(() => bus.ExecuteAsync(default(AsyncCommandWithResult)!, CancellationToken.None));
    }

    [Fact]
    public void when_executing_non_public_command_handler_then_invokes_handler_with_result()
    {
        var handler = new NonPublicCommandHandlerWithResults(new Result());
        var bus = new ServiceCollection()
            .AddMessageBus(false)
            .AddSingleton<ICommandHandler<CommandWithResults, IEnumerable<Result>>>(handler)
            .BuildServiceProvider()
            .GetRequiredService<IMessageBus>();

        var results = bus.Execute(new CommandWithResults());

        Assert.NotNull(results);
        Assert.Single(results);
    }

    [Fact]
    public void when_executing_command_as_explicit_ICommand_then_invokes_handler()
    {
        var handler = new Mock<ICommandHandler<Command>>();
        var command = new Command();
        var bus = new ServiceCollection()
            .AddMessageBus(false)
            .AddSingleton(handler.Object)
            .AddSingleton<ICommandHandler<Command>>(handler.Object)
            .BuildServiceProvider()
            .GetRequiredService<IMessageBus>();

        bus.Execute((ICommand)command);

        handler.Verify(x => x.Execute(command));
    }

    [Fact]
    public void when_command_execution_throws_then_throws_original_exception()
    {
        var handler = new Mock<ICommandHandler<Command>>();
        var command = new Command();
        var exception = new InvalidOperationException();
        handler.Setup(x => x.Execute(command)).Throws(exception);
        var bus = new ServiceCollection()
            .AddMessageBus(false)
            .AddSingleton(handler.Object)
            .AddSingleton<ICommandHandler<Command>>(handler.Object)
            .BuildServiceProvider()
            .GetRequiredService<IMessageBus>();

        var actual = Assert.Throws<InvalidOperationException>(() => bus.Execute((ICommand)command));

        Assert.Same(exception, actual);
    }

    class NestedEvent { }
}