using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Merq
{
	public class CommandBusSpec
	{
		[Fact]
		public void when_registering_non_generic_handler_then_throws ()
		{
			Assert.Throws<ArgumentException> (() => new CommandBus (Mock.Of<IAsyncManager>(), Mock.Of<ICommandHandler> ()));
		}

		[Fact]
		public void when_registering_duplicate_handlers_then_throws ()
		{
			Assert.Throws<ArgumentException> (() => new CommandBus (
				Mock.Of<IAsyncManager> (),
				Mock.Of<ICommandHandler<Command>> (),
				Mock.Of<ICommandHandler<Command>> ()));
		}

		[Fact]
		public void when_executing_command_without_handler_then_throws ()
		{
			var bus = new CommandBus(Mock.Of<IAsyncManager>());

			Assert.Throws<NotSupportedException> (() => bus.Execute (new Command ()));
		}

		[Fact]
		public void when_executing_command_with_result_without_handler_then_throws ()
		{
			var bus = new CommandBus(Mock.Of<IAsyncManager>());

			Assert.Throws<NotSupportedException> (() => bus.Execute<Result> (new CommandWithResult ()));
		}

		[Fact]
		public async void when_executing_async_command_without_handler_then_throws ()
		{
			var bus = new CommandBus(Mock.Of<IAsyncManager>());

			await Assert.ThrowsAsync<NotSupportedException> (() => bus.ExecuteAsync (new Command (), CancellationToken.None));
		}

		[Fact]
		public async void when_executing_async_command_with_result_without_handler_then_throws ()
		{
			var bus = new CommandBus(Mock.Of<IAsyncManager>());

			await Assert.ThrowsAsync<NotSupportedException> (() => bus.ExecuteAsync<Result> (new CommandWithResult (), CancellationToken.None));
		}

		[Fact]
		public void when_can_handle_requested_for_non_registered_handler_then_returns_false ()
		{
			var bus = new CommandBus(Mock.Of<IAsyncManager>());

			Assert.False (bus.CanHandle<Command> ());
		}

		[Fact]
		public void when_can_handle_requested_for_registered_handler_type_then_returns_true ()
		{
			var bus = new CommandBus(Mock.Of<IAsyncManager>(), Mock.Of<ICommandHandler<Command>>());

			Assert.True (bus.CanHandle<Command> ());
		}

		[Fact]
		public void when_can_handle_requested_for_registered_handler_instance_then_returns_true ()
		{
			var bus = new CommandBus(Mock.Of<IAsyncManager>(), Mock.Of<ICommandHandler<Command>>());

			Assert.True (bus.CanHandle (new Command ()));
		}

		[Fact]
		public void when_can_handle_requested_for_null_command_then_throws ()
		{
			var bus = new CommandBus(Mock.Of<IAsyncManager>(), Mock.Of<ICommandHandler<Command>>());

			Assert.Throws<ArgumentNullException> (() => bus.CanHandle ((Command)null));
		}

		[Fact]
		public void when_can_execute_requested_and_no_handler_registered_then_throws ()
		{
			var bus = new CommandBus(Mock.Of<IAsyncManager>());

			Assert.Throws<NotSupportedException> (() => bus.CanExecute (new Command ()));
		}

		[Fact]
		public void when_can_execute_requested_then_invokes_sync_handler ()
		{
			var command = new Command();
			var bus = new CommandBus(Mock.Of<IAsyncManager>(), Mock.Of<ICommandHandler<Command>>(c => c.CanExecute(command) == true));

			Assert.True (bus.CanExecute (command));
		}

		[Fact]
		public void when_can_execute_requested_then_invokes_async_handler ()
		{
			var command = new Command();
			var bus = new CommandBus(Mock.Of<IAsyncManager>(), Mock.Of<IAsyncCommandHandler<Command>>(c => c.CanExecute(command) == true));

			Assert.True (bus.CanExecute (command));
		}

		[Fact]
		public void when_executing_sync_command_then_invokes_sync_handler ()
		{
			var handler = new Mock<ICommandHandler<Command>>();
			var command = new Command();
			var bus = new CommandBus(Mock.Of<IAsyncManager>(), handler.Object);

			bus.Execute (command);

			handler.Verify (x => x.Execute (command));
		}

		[Fact]
		public void when_executing_sync_command_then_invokes_sync_handler_with_result ()
		{
			var handler = new Mock<ICommandHandler<CommandWithResult, Result>>();
			var command = new CommandWithResult();
			var bus = new CommandBus(Mock.Of<IAsyncManager>(), handler.Object);

			bus.Execute (command);

			handler.Verify (x => x.Execute (command));
		}

		[Fact]
		public void when_executing_sync_command_then_invokes_async_handler ()
		{
			var handler = new Mock<IAsyncCommandHandler<Command>>();
			var async = new Mock<IAsyncManager>();
			var command = new Command();

			handler.Setup (x => x.ExecuteAsync (command, CancellationToken.None)).Returns (Task.FromResult (true));
			async.Setup (x => x.Run (It.IsAny<Func<Task>> ()))
				.Callback<Func<Task>> (asyncMethod => asyncMethod ().Wait());

			var bus = new CommandBus(async.Object, handler.Object);

			bus.Execute (command);

			handler.Verify (x => x.ExecuteAsync (command, CancellationToken.None));
		}

		[Fact]
		public void when_executing_sync_command_then_invokes_async_handler_with_result ()
		{
			var handler = new Mock<IAsyncCommandHandler<CommandWithResult, Result>>();
			var async = new Mock<IAsyncManager>();
            var command = new CommandWithResult();

			handler.Setup (x => x.ExecuteAsync (command, CancellationToken.None)).Returns (Task.FromResult (new Result ()));
			async.Setup (x => x.Run (It.IsAny<Func<Task<Result>>> ()))
				.Returns<Func<Task<Result>>> (asyncMethod => asyncMethod ().Result);

			var bus = new CommandBus(async.Object, handler.Object);

			bus.Execute (command);

			handler.Verify (x => x.ExecuteAsync (command, CancellationToken.None));
		}

		[Fact]
		public void when_executing_sync_command_with_result_then_invokes_sync_handler_with_result ()
		{
			var handler = new Mock<ICommandHandler<CommandWithResult, Result>>();
			var command = new CommandWithResult();
			var expected = new Result();

			handler.Setup (x => x.Execute (command)).Returns (expected);
			var bus = new CommandBus(Mock.Of<IAsyncManager>(), handler.Object);

			var result = bus.Execute<Result> (command);

			Assert.Same (expected, result);
		}

		[Fact]
		public void when_executing_sync_command_with_result_then_invokes_async_handler_with_result ()
		{
			var handler = new Mock<IAsyncCommandHandler<CommandWithResult, Result>>();
			var async = new Mock<IAsyncManager>();
			var command = new CommandWithResult();
			var expected = new Result();

			handler.Setup (x => x.ExecuteAsync (command, CancellationToken.None)).Returns (Task.FromResult (expected));
			async.Setup (x => x.Run (It.IsAny<Func<Task<Result>>> ()))
				.Returns<Func<Task<Result>>> (asyncMethod => asyncMethod ().Result);

			var bus = new CommandBus(async.Object, handler.Object);

			var result = bus.Execute<Result> (command);

			Assert.Same (expected, result);
		}

		[Fact]
		public void when_executing_sync_command_with_result_then_throws_for_sync_handler_without_result ()
		{
			var handler = new Mock<ICommandHandler<CommandWithResult>>();
			var command = new CommandWithResult();
			var expected = new Result();

			Assert.Throws<ArgumentException> (() => new CommandBus (Mock.Of<IAsyncManager> (), handler.Object));
		}

		[Fact]
		public void when_registering_sync_command_with_result_then_throws_for_async_handler_without_result ()
		{
			var handler = new Mock<IAsyncCommandHandler<CommandWithResult>>();

			Assert.Throws<ArgumentException> (() => new CommandBus (Mock.Of<IAsyncManager> (), handler.Object));
		}

		[Fact]
		public async void when_executing_async_command_then_invokes_sync_handler ()
		{
			var handler = new Mock<ICommandHandler<Command>>();
			var command = new Command();

			var bus = new CommandBus(Mock.Of<IAsyncManager>(), handler.Object);

			await bus.ExecuteAsync (command, CancellationToken.None);

			handler.Verify (x => x.Execute (command));
		}

		[Fact]
		public async void when_executing_async_command_then_invokes_sync_handler_with_result ()
		{
			var handler = new Mock<ICommandHandler<CommandWithResult, Result>>();
			var command = new CommandWithResult();

			handler.Setup (x => x.Execute (command)).Returns (new Result ());
			var bus = new CommandBus(Mock.Of<IAsyncManager>(), handler.Object);

			await bus.ExecuteAsync (command, CancellationToken.None);

			handler.Verify (x => x.Execute (command));
		}

		[Fact]
		public async void when_executing_async_command_then_invokes_async_handler ()
		{
			var handler = new Mock<IAsyncCommandHandler<Command>>();
			var command = new Command();

			handler.Setup (x => x.ExecuteAsync (command, CancellationToken.None)).Returns (Task.FromResult (true));
			var bus = new CommandBus(Mock.Of<IAsyncManager>(), handler.Object);

			await bus.ExecuteAsync (command, CancellationToken.None);

			handler.Verify (x => x.ExecuteAsync (command, CancellationToken.None));
		}

		[Fact]
		public async void when_executing_async_command_then_invokes_async_handler_with_result ()
		{
			var handler = new Mock<IAsyncCommandHandler<CommandWithResult, Result>>();
			var command = new CommandWithResult();
			var result = new Result();

			handler.Setup (x => x.ExecuteAsync (command, CancellationToken.None)).Returns (Task.FromResult (result));
			var bus = new CommandBus(Mock.Of<IAsyncManager>(), handler.Object);

			await bus.ExecuteAsync (command, CancellationToken.None);

			handler.Verify (x => x.ExecuteAsync (command, CancellationToken.None));
		}

		[Fact]
		public async void when_executing_async_command_with_result_then_invokes_sync_handler_with_result ()
		{
			var handler = new Mock<ICommandHandler<CommandWithResult, Result>>();
			var command = new CommandWithResult();
			var result = new Result();

			handler.Setup (x => x.Execute (command)).Returns (result);
			var bus = new CommandBus(Mock.Of<IAsyncManager>(), handler.Object);

			var actual = await bus.ExecuteAsync<Result> (command, CancellationToken.None);

			Assert.Same (result, actual);
		}

		[Fact]
		public async void when_executing_async_command_with_result_then_invokes_async_handler_with_result ()
		{
			var handler = new Mock<IAsyncCommandHandler<CommandWithResult, Result>>();
			var command = new CommandWithResult();
			var result = new Result();

			handler.Setup (x => x.ExecuteAsync (command, CancellationToken.None)).Returns (Task.FromResult (result));
			var bus = new CommandBus(Mock.Of<IAsyncManager>(), handler.Object);

			var actual = await bus.ExecuteAsync<Result> (command, CancellationToken.None);

			Assert.Same (result, actual);
		}

		[Fact]
		public void when_registering_async_command_with_result_then_throws_for_sync_handler_without_result ()
		{
			var handler = new Mock<ICommandHandler<CommandWithResult>>();

			Assert.Throws<ArgumentException> (() => new CommandBus (Mock.Of<IAsyncManager> (), handler.Object));
		}

		[Fact]
		public void when_registering_async_command_with_result_then_throws_for_async_handler_without_result ()
		{
			var handler = new Mock<IAsyncCommandHandler<CommandWithResult>>();

			Assert.Throws<ArgumentException> (() => new CommandBus (Mock.Of<IAsyncManager> (), handler.Object));
		}

		public class Command : ICommand { }

		public class CommandWithResult : ICommand<Result> { }

		public class Result { }
	}
}
