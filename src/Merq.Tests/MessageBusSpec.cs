using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Merq;

partial record Foo(string Message, string Format)
{
    internal static Foo Create(dynamic value) => new(value.Message, value.Format);
}

partial record Foo { }

public record MessageBusSpec(ITestOutputHelper Output)
{
    readonly IMessageBus bus = new MessageBus(new ServiceCollection().BuildServiceProvider());

#if NET6_0_OR_GREATER

    [Fact]
    public async Task when_executing_stream_then_gets_results()
    {
        var handler = new StreamCommandHandler();
        var command = new StreamCommand(3);

        var bus = new MessageBus(new ServiceCollection()
            .AddSingleton<IStreamCommandHandler<StreamCommand, int>>(handler)
            .AddSingleton<ICanExecute<StreamCommand>>(handler)
            .BuildServiceProvider());

        Assert.True(bus.CanHandle(command));
        Assert.True(bus.CanExecute(command));

        var values = new List<int>();

        await foreach (var value in bus.ExecuteStream(command, CancellationToken.None))
            values.Add(value);

        Assert.Equal(new[] { 0, 1, 2 }, values);
    }

    [Fact]
    public async Task when_executing_stream_then_can_cancel_enumeration()
    {
        var handler = new StreamCommandHandler();
        var command = new StreamCommand(3);
        var cts = new CancellationTokenSource();

        var bus = new MessageBus(new ServiceCollection()
            .AddSingleton<IStreamCommandHandler<StreamCommand, int>>(handler)
            .AddSingleton<ICanExecute<StreamCommand>>(handler)
            .BuildServiceProvider());

        Assert.True(bus.CanExecute(command));

        var values = new List<int>();

        try
        {
            await foreach (var value in bus.ExecuteStream(command, cts.Token))
            {
                values.Add(value);
                if (values.Count == 2)
                    cts.Cancel();
            }
        }
        catch (TaskCanceledException)
        {
        }

        Assert.Equal(new[] { 0, 1 }, values);
    }

#endif

    [Fact]
    public void when_subscribing_external_producer_then_succeeds()
    {
        var producer = new Subject<int>();
        var collection = new ServiceCollection()
            .AddSingleton<IObservable<int>>(producer);

        var services = collection.BuildServiceProvider();
        var bus = new MessageBus(services);

        int? value = default;

        bus.Observe<int>().Subscribe(i => value = i);

        producer.OnNext(42);

        Assert.Equal(42, value);
    }

    [Fact]
    public void given_external_producer_when_subscribing_to_base_then_notifies()
    {
        var producer = new Subject<ConcreteEvent>();

        var bus = new MessageBus(new ServiceCollection()
            .AddSingleton<IObservable<ConcreteEvent>>(producer)
            .AddSingleton<IObservable<BaseEvent>>(producer)
            .BuildServiceProvider());

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

        var bus = new MessageBus(new ServiceCollection()
            .AddSingleton<IObservable<ConcreteEvent>>(producer)
            .AddSingleton<IObservable<BaseEvent>>(producer)
            .BuildServiceProvider());

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
        var bus = new MessageBus(new ServiceCollection()
            .AddSingleton<IObservable<ConcreteEvent>>(subject1)
            // NOTE: the producer needs to properly register as the base types too
            .AddSingleton<IObservable<BaseEvent>>(subject1)
            .AddSingleton(subject2)
            .AddSingleton<IObservable<AnotherEvent>>(subject2)
            .AddSingleton<IObservable<BaseEvent>>(subject2)
            .BuildServiceProvider());

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

        var bus = new MessageBus(new ServiceCollection()
            .AddSingleton(handler)
            .AddSingleton<IExecutableCommandHandler<Command>>(handler)
            .BuildServiceProvider());

        Assert.True(bus.CanHandle(new Command()));
    }

    [Fact]
    public void when_can_execute_requested_then_invokes_sync_handler()
    {
        var command = new Command();
        var handler = Mock.Of<ICommandHandler<Command>>(c => c.CanExecute(command) == true);

        var bus = new MessageBus(new ServiceCollection()
            .AddSingleton(handler)
            .AddSingleton<IExecutableCommandHandler<Command>>(handler)
            .BuildServiceProvider());

        Assert.True(bus.CanExecute(command));
    }

    [Fact]
    public void when_can_execute_requested_then_invokes_async_handler()
    {
        var command = new AsyncCommand();
        var handler = Mock.Of<IAsyncCommandHandler<AsyncCommand>>(c => c.CanExecute(command) == true);

        var bus = new MessageBus(new ServiceCollection()
            .AddSingleton(handler)
            .AddSingleton<IExecutableCommandHandler<AsyncCommand>>(handler)
            .BuildServiceProvider());

        Assert.True(bus.CanExecute(command));
    }

    [Fact]
    public void when_executing_sync_command_then_invokes_sync_handler()
    {
        var handler = new Mock<ICommandHandler<Command>>();
        var command = new Command();

        var bus = new MessageBus(new ServiceCollection()
            .AddSingleton(handler.Object)
            .AddSingleton<IExecutableCommandHandler<Command>>(handler.Object)
            .BuildServiceProvider());

        bus.Execute(command);

        handler.Verify(x => x.Execute(command));
    }

    [Fact]
    public void when_executing_sync_command_then_invokes_sync_handler_with_result()
    {
        var handler = new Mock<ICommandHandler<CommandWithResult, Result>>();
        var command = new CommandWithResult();

        var bus = new MessageBus(new ServiceCollection()
            .AddSingleton(handler.Object)
            .AddSingleton<IExecutableCommandHandler<CommandWithResult>>(handler.Object)
            .BuildServiceProvider());

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

        var bus = new MessageBus(new ServiceCollection()
            .AddSingleton(handler.Object)
            .AddSingleton<IExecutableCommandHandler<CommandWithResult>>(handler.Object)
            .BuildServiceProvider());

        var result = bus.Execute(command);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task when_executing_async_command_then_invokes_async_handler()
    {
        var handler = new Mock<IAsyncCommandHandler<AsyncCommand>>();
        var command = new AsyncCommand();
        handler.Setup(x => x.ExecuteAsync(command, CancellationToken.None)).Returns(Task.FromResult(true));

        var bus = new MessageBus(new ServiceCollection()
            .AddSingleton(handler.Object)
            .AddSingleton<IExecutableCommandHandler<AsyncCommand>>(handler.Object)
            .BuildServiceProvider());

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

        var bus = new MessageBus(new ServiceCollection()
            .AddSingleton(handler.Object)
            .AddSingleton<IExecutableCommandHandler<AsyncCommandWithResult>>(handler.Object)
            .BuildServiceProvider());

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
        var bus = new MessageBus(new ServiceCollection()
            .AddSingleton<ICommandHandler<CommandWithResults, IEnumerable<Result>>>(handler)
            .AddSingleton<IExecutableCommandHandler<CommandWithResults>>(handler)
            .BuildServiceProvider());

        var results = bus.Execute(new CommandWithResults());

        Assert.Single(results);
    }

    [Fact]
    public void when_executing_command_as_explicit_ICommand_then_invokes_handler()
    {
        var handler = new Mock<ICommandHandler<Command>>();
        var command = new Command();
        var bus = new MessageBus(new ServiceCollection()
            .AddSingleton(handler.Object)
            .AddSingleton<IExecutableCommandHandler<Command>>(handler.Object)
            .BuildServiceProvider());

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
        var bus = new MessageBus(new ServiceCollection()
            .AddSingleton(handler.Object)
            .AddSingleton<IExecutableCommandHandler<Command>>(handler.Object)
            .BuildServiceProvider());

        var actual = Assert.Throws<InvalidOperationException>(() => bus.Execute((ICommand)command));

        Assert.Same(exception, actual);
    }

    [Fact]
    public void when_executing_non_public_command_then_invokes_handler()
    {
        var handler = new NonPublicCommandHandler();
        var bus = new MessageBus(new ServiceCollection()
            .AddSingleton<ICommandHandler<NonPublicCommand>>(handler)
            .AddSingleton<IExecutableCommandHandler<NonPublicCommand>>(handler)
            .BuildServiceProvider());

        bus.Execute(new NonPublicCommand());
    }

    [Fact]
    public void when_executing_nested_public_command_then_invokes_handler()
    {
        var handler = new Mock<ICommandHandler<NestedPublicCommand>>();
        var bus = new MessageBus(new ServiceCollection()
            .AddSingleton(handler.Object)
            .AddSingleton<IExecutableCommandHandler<NestedPublicCommand>>(handler.Object)
            .BuildServiceProvider());

        var command = new NestedPublicCommand();

        bus.Execute(command);

        handler.Verify(x => x.Execute(command));
    }

    [Fact]
    public void when_executing_non_public_command_result_then_invokes_handler()
    {
        var handler = new NonPublicCommandResultHandler();
        var bus = new MessageBus(new ServiceCollection()
            .AddSingleton<ICommandHandler<NonPublicCommandResult, int>>(handler)
            .AddSingleton<IExecutableCommandHandler<NonPublicCommandResult>>(handler)
            .BuildServiceProvider());

        Assert.Equal(42, bus.Execute(new NonPublicCommandResult()));
    }

    [Fact]
    public async Task when_executing_non_public_asynccommand_then_invokes_handler()
    {
        var handler = new NonPublicAsyncCommandHandler();
        var bus = new MessageBus(new ServiceCollection()
            .AddSingleton<IAsyncCommandHandler<NonPublicAsyncCommand>>(handler)
            .AddSingleton<IExecutableCommandHandler<NonPublicAsyncCommand>>(handler)
            .BuildServiceProvider());

        await bus.ExecuteAsync(new NonPublicAsyncCommand(), CancellationToken.None);
    }

    [Fact]
    public async Task when_executing_non_public_àsynccommand_result_then_invokes_handler()
    {
        var handler = new NonPublicAsyncCommandResultHandler();
        var bus = new MessageBus(new ServiceCollection()
            .AddSingleton<IAsyncCommandHandler<NonPublicAsyncCommandResult, int>>(handler)
            .AddSingleton<IExecutableCommandHandler<NonPublicAsyncCommandResult>>(handler)
            .BuildServiceProvider());

        Assert.Equal(42, await bus.ExecuteAsync(new NonPublicAsyncCommandResult(), CancellationToken.None));
    }

    [Fact]
    public void when_notifying_can_access_event_from_activity_stop()
    {
        Activity? activity = default;
        using var listener = new ActivityListener
        {
            ActivityStopped = a =>
            {
                if (a.GetTagItem("messaging.destination.name") is "Merq.ConcreteEvent")
                    activity = a;
            },
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ShouldListenTo = source => source.Name == "Merq",
        };

        ActivitySource.AddActivityListener(listener);

        bus.Notify(new ConcreteEvent());

        Assert.NotNull(activity);
        Assert.IsType<ConcreteEvent>(activity.GetCustomProperty("Event"));
    }

    [Fact]
    public void when_executing_can_access_command_from_activity_started()
    {
        Activity? activity = default;
        using var listener = new ActivityListener
        {
            ActivityStarted = a =>
            {
                if (a.GetTagItem("messaging.destination.name") is "Merq.CommandWithResult")
                    activity = a;
            },
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => _.Source.Name == "Merq" ? ActivitySamplingResult.AllData : ActivitySamplingResult.None,
            ShouldListenTo = source => source.Name == "Merq",
        };

        ActivitySource.AddActivityListener(listener);

        Assert.Throws<InvalidOperationException>(() => bus.Execute(new CommandWithResult()));

        Assert.NotNull(activity);
        Assert.IsType<CommandWithResult>(activity.GetCustomProperty("Command"));
    }

    [Fact]
    public void when_execute_throws_activity_has_error_status()
    {
        Activity? activity = default;
        using var listener = new ActivityListener
        {
            ActivityStarted = x => activity = x,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => _.Source.Name == "Merq" ? ActivitySamplingResult.AllData : ActivitySamplingResult.None,
            ShouldListenTo = source => source.Name == "Merq",
        };

        ActivitySource.AddActivityListener(listener);

        Assert.Throws<InvalidOperationException>(() => bus.Execute(new Command()));

        Assert.NotNull(activity);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
    }

    class NestedEvent { }

    public class NestedPublicCommand : ICommand { }

#if NET6_0_OR_GREATER
    public record StreamCommand(int Count) : IStreamCommand<int>;

    class StreamCommandHandler : IStreamCommandHandler<StreamCommand, int>
    {
        public bool CanExecute(StreamCommand command) => command.Count > 0;

        public async IAsyncEnumerable<int> ExecuteSteam(StreamCommand command, [EnumeratorCancellation] CancellationToken cancellation)
        {
            for (var i = 0; i < command.Count; i++)
            {
                //if (cancellation.IsCancellationRequested)
                //    yield break;

                await Task.Delay(0, cancellation);
                yield return i;
            }
        }
    }
#endif
}