using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;

namespace Merq;

public class CommandBusSpec
{
    [Fact]
    public void when_registering_non_generic_handler_then_throws()
    {
        Assert.Throws<ArgumentException>(() => new CommandBus(Mock.Of<ICommandHandler>()));
    }

    [Fact]
    public void when_registering_duplicate_handlers_then_throws()
    {
        Assert.Throws<ArgumentException>(() => new CommandBus(
           Mock.Of<ICommandHandler<Command>>(),
           Mock.Of<ICommandHandler<Command>>()));
    }

    [Fact]
    public void when_executing_command_without_handler_then_throws()
    {
        var bus = new CommandBus();

        Assert.Throws<NotSupportedException>(() => bus.Execute(new Command()));
    }

    [Fact]
    public void when_executing_command_with_result_without_handler_then_throws()
    {
        var bus = new CommandBus();

        Assert.Throws<NotSupportedException>(() => bus.Execute<Result>(new CommandWithResult()));
    }

    [Fact]
    public async Task when_executing_async_command_without_handler_then_throws()
    {
        var bus = new CommandBus();

        await Assert.ThrowsAsync<NotSupportedException>(() => bus.ExecuteAsync(new AsyncCommand(), CancellationToken.None));
    }

    [Fact]
    public async Task when_executing_async_command_with_result_without_handler_then_throws()
    {
        var bus = new CommandBus();

        await Assert.ThrowsAsync<NotSupportedException>(() => bus.ExecuteAsync<Result>(new AsyncCommandWithResult(), CancellationToken.None));
    }

    [Fact]
    public void when_can_handle_requested_for_non_registered_handler_then_returns_false()
    {
        var bus = new CommandBus();

        Assert.False(bus.CanHandle<Command>());
    }

    [Fact]
    public void when_can_handle_requested_for_registered_handler_type_then_returns_true()
    {
        var bus = new CommandBus(Mock.Of<ICommandHandler<Command>>());

        Assert.True(bus.CanHandle<Command>());
    }

    [Fact]
    public void when_can_handle_requested_for_registered_handler_instance_then_returns_true()
    {
        var bus = new CommandBus(Mock.Of<ICommandHandler<Command>>());

        Assert.True(bus.CanHandle(new Command()));
    }

    [Fact]
    public void when_can_handle_requested_for_null_command_then_returns_false()
    {
        var bus = new CommandBus(Mock.Of<ICommandHandler<Command>>());

        Assert.False(bus.CanHandle(default(Command)!));
    }

    [Fact]
    public void when_can_execute_requested_and_no_handler_registered_then_returns_false()
    {
        var bus = new CommandBus();

        Assert.False(bus.CanExecute(new Command()));
    }

    [Fact]
    public void when_can_execute_requested_then_invokes_sync_handler()
    {
        var command = new Command();
        var bus = new CommandBus(Mock.Of<ICommandHandler<Command>>(c => c.CanExecute(command) == true));

        Assert.True(bus.CanExecute(command));
    }

    [Fact]
    public void when_can_execute_requested_then_invokes_async_handler()
    {
        var command = new AsyncCommand();
        var bus = new CommandBus(Mock.Of<IAsyncCommandHandler<AsyncCommand>>(c => c.CanExecute(command) == true));

        Assert.True(bus.CanExecute(command));
    }

    [Fact]
    public void when_executing_sync_command_then_invokes_sync_handler()
    {
        var handler = new Mock<ICommandHandler<Command>>();
        var command = new Command();
        var bus = new CommandBus(handler.Object);

        bus.Execute(command);

        handler.Verify(x => x.Execute(command));
    }

    [Fact]
    public void when_executing_sync_command_then_invokes_sync_handler_with_result()
    {
        var handler = new Mock<ICommandHandler<CommandWithResult, Result>>();
        var command = new CommandWithResult();
        var bus = new CommandBus(handler.Object);

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
        var bus = new CommandBus(handler.Object);

        var result = bus.Execute(command);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task when_executing_async_command_then_invokes_async_handler()
    {
        var handler = new Mock<IAsyncCommandHandler<AsyncCommand>>();
        var command = new AsyncCommand();

        handler.Setup(x => x.ExecuteAsync(command, CancellationToken.None)).Returns(Task.FromResult(true));
        var bus = new CommandBus(handler.Object);

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
        var bus = new CommandBus(handler.Object);

        await bus.ExecuteAsync(command, CancellationToken.None);

        handler.Verify(x => x.ExecuteAsync(command, CancellationToken.None));
    }

    [Fact]
    public void when_constructing_with_null_handlers_then_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CommandBus(default(IEnumerable<ICommandHandler>)!));
    }

    [Fact]
    public void when_can_handle_with_null_command_then_returns_false()
    {
        Assert.False(new CommandBus().CanHandle(null!));
    }

    [Fact]
    public void when_can_execute_with_null_command_then_returns_false()
    {
        Assert.False(new CommandBus().CanExecute<Command>(null!));
    }

    [Fact]
    public void when_execute_with_null_command_then_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CommandBus().Execute(default(Command)!));
    }

    [Fact]
    public void when_execute_result_with_null_command_then_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CommandBus().Execute(default(CommandWithResult)!));
    }

    [Fact]
    public async Task when_executeasync_with_null_command_then_throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => new CommandBus().ExecuteAsync(default(AsyncCommand)!, CancellationToken.None));
    }

    [Fact]
    public async Task when_executeasync_result_with_null_command_then_throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => new CommandBus().ExecuteAsync(default(AsyncCommandWithResult)!, CancellationToken.None));
    }

    [Fact]
    public void when_executing_non_public_command_handler_then_invokes_handler_with_result()
    {
        var handler = new NonPublicCommandHandlerWithResults(new Result());
        var bus = new CommandBus(handler);

        var results = bus.Execute(new CommandWithResults());

        Assert.Single(results);
    }

    [Fact]
    public void when_executing_command_as_explicit_ICommand_then_invokes_handler()
    {
        var handler = new Mock<ICommandHandler<Command>>();
        var command = new Command();
        var bus = new CommandBus(handler.Object);

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
        var bus = new CommandBus(handler.Object);

        var actual = Assert.Throws<InvalidOperationException>(() => bus.Execute((ICommand)command));

        Assert.Same(exception, actual);
    }

    public class AsyncCommand : IAsyncCommand { }

    public class AsyncCommandWithResult : IAsyncCommand<Result> { }

    public class Command : ICommand { }

    public class CommandWithResult : ICommand<Result> { }

    public class CommandWithResults : ICommand<IEnumerable<Result>> { }

    public class Result { }

    class NonPublicCommandHandlerWithResults : ICommandHandler<CommandWithResults, IEnumerable<Result>>
    {
        Result result;

        public NonPublicCommandHandlerWithResults(Result result)
        {
            this.result = result;
        }

        bool ICanExecute<CommandWithResults>.CanExecute(CommandWithResults command)
        {
            return true;
        }

        IEnumerable<Result> ICommandHandler<CommandWithResults, IEnumerable<Result>>.Execute(CommandWithResults command)
        {
            yield return result;
        }
    }

    // Ensure all test to be run using a derived command bus class
    class CommandBus : Merq.CommandBus
    {
        public CommandBus(IEnumerable<ICommandHandler> handlers) : base(handlers) { }

        public CommandBus(params ICommandHandler[] handlers) : base(handlers) { }
    }
}
