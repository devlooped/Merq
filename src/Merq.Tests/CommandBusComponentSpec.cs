using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Moq;
using Xunit;

namespace Merq
{
    public class CommandBusComponentSpec
    {
        [Fact]
        public void when_instantiating_with_service_provider_then_uses_component_model_service()
        {
            new CommandBusComponent(Mock.Of<IServiceProvider>(x =>
                x.GetService(typeof(SComponentModel)) == Mock.Of<IComponentModel>()));
        }

        [Fact]
        public void when_executing_command_without_handler_then_throws()
        {
            var bus = new CommandBusComponent(Mock.Of<IComponentModel>());

            Assert.Throws<NotSupportedException>(() => bus.Execute(new Command()));
        }

        [Fact]
        public void when_executing_command_with_result_without_handler_then_throws()
        {
            var bus = new CommandBusComponent(Mock.Of<IComponentModel>());

            Assert.Throws<NotSupportedException>(() => bus.Execute<Result>(new CommandWithResult()));
        }

        [Fact]
        public async Task when_executing_async_command_without_handler_then_throws()
        {
            var bus = new CommandBusComponent(Mock.Of<IComponentModel>());

            await Assert.ThrowsAsync<NotSupportedException>(() => bus.ExecuteAsync(new AsyncCommand(), CancellationToken.None));
        }

        [Fact]
        public async Task when_executing_async_command_with_result_without_handler_then_throws()
        {
            var bus = new CommandBusComponent(Mock.Of<IComponentModel>());

            await Assert.ThrowsAsync<NotSupportedException>(() => bus.ExecuteAsync<Result>(new AsyncCommandWithResult(), CancellationToken.None));
        }

        [Fact]
        public void when_can_handle_requested_for_non_registered_handler_then_returns_false()
        {
            var bus = new CommandBusComponent(Mock.Of<IComponentModel>());

            Assert.False(bus.CanHandle<Command>());
        }

        [Fact]
        public void when_can_handle_requested_for_registered_handler_type_then_returns_true()
        {
            var bus = new CommandBusComponent(Mock.Of<IComponentModel>(x =>
                x.GetExtensions<ICanExecute<Command>>() == new[] { Mock.Of<ICanExecute<Command>>() }));

            Assert.True(bus.CanHandle<Command>());
        }

        [Fact]
        public void when_can_handle_requested_for_registered_handler_instance_then_returns_true()
        {
            var bus = new CommandBusComponent(Mock.Of<IComponentModel>(x =>
                x.GetExtensions<ICanExecute<Command>>() == new[] { Mock.Of<ICanExecute<Command>>() }));

            Assert.True(bus.CanHandle(new Command()));
        }

        [Fact]
        public void when_can_handle_requested_for_null_command_then_throws()
        {
            var bus = new CommandBusComponent(Mock.Of<IComponentModel>());

            Assert.Throws<ArgumentNullException>(() => bus.CanHandle((Command)null!));
        }

        [Fact]
        public void when_can_execute_requested_and_no_handler_registered_then_returns_false()
        {
            var bus = new CommandBusComponent(Mock.Of<IComponentModel>());

            Assert.False(bus.CanExecute(new Command()));
        }

        [Fact]
        public void when_can_execute_requested_then_invokes_sync_handler()
        {
            var command = new Command();
            var bus = new CommandBusComponent(Mock.Of<IComponentModel>(x =>
                x.GetExtensions<ICanExecute<Command>>() == new[] { Mock.Of<ICanExecute<Command>>(c => c.CanExecute(command) == true) }));

            Assert.True(bus.CanExecute(command));
        }

        [Fact]
        public void when_can_execute_requested_then_invokes_async_handler()
        {
            var command = new AsyncCommand();
            var bus = new CommandBusComponent(Mock.Of<IComponentModel>(x =>
                x.GetExtensions<ICanExecute<AsyncCommand>>() == new[] { Mock.Of<ICanExecute<AsyncCommand>>(c => c.CanExecute(command) == true) }));

            Assert.True(bus.CanExecute(command));
        }

        [Fact]
        public void when_executing_sync_command_then_invokes_sync_handler()
        {
            var handler = new Mock<ICommandHandler<Command>>();
            var command = new Command();

            var bus = new CommandBusComponent(Mock.Of<IComponentModel>(x =>
                x.GetExtensions<ICommandHandler<Command>>() == new[] { handler.Object }));

            bus.Execute(command);

            handler.Verify(x => x.Execute(command));
        }

        [Fact]
        public void when_executing_sync_command_then_invokes_sync_handler_with_result()
        {
            var handler = new Mock<ICommandHandler<CommandWithResult, Result>>();
            var command = new CommandWithResult();
            var bus = new CommandBusComponent(Mock.Of<IComponentModel>(x =>
                x.GetExtensions<ICommandHandler<CommandWithResult, Result>>() == new[] { handler.Object }));

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
            var bus = new CommandBusComponent(Mock.Of<IComponentModel>(x =>
                x.GetExtensions<ICommandHandler<CommandWithResult, Result>>() == new[] { handler.Object }));

            var result = bus.Execute(command);

            Assert.Same(expected, result);
        }

        [Fact]
        public async Task when_executing_async_command_then_invokes_async_handler()
        {
            var handler = new Mock<IAsyncCommandHandler<AsyncCommand>>();
            var command = new AsyncCommand();

            handler.Setup(x => x.ExecuteAsync(command, CancellationToken.None)).Returns(Task.FromResult(true));
            var bus = new CommandBusComponent(Mock.Of<IComponentModel>(x =>
                x.GetExtensions<IAsyncCommandHandler<AsyncCommand>>() == new[] { handler.Object }));

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
            var bus = new CommandBusComponent(Mock.Of<IComponentModel>(x =>
                x.GetExtensions<IAsyncCommandHandler<AsyncCommandWithResult, Result>>() == new[] { handler.Object }));

            await bus.ExecuteAsync(command, CancellationToken.None);

            handler.Verify(x => x.ExecuteAsync(command, CancellationToken.None));
        }


        [Fact]
        public void when_can_handle_with_null_command_then_throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CommandBusComponent(Mock.Of<IComponentModel>()).CanHandle(null!));
        }

        [Fact]
        public void when_can_execute_with_null_command_then_throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CommandBusComponent(Mock.Of<IComponentModel>()).CanExecute<Command>(null!));
        }

        [Fact]
        public void when_execute_with_null_command_then_throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CommandBusComponent(Mock.Of<IComponentModel>()).Execute(default(Command)!));
        }

        [Fact]
        public void when_execute_result_with_null_command_then_throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CommandBusComponent(Mock.Of<IComponentModel>()).Execute(default(CommandWithResult)!));
        }

        [Fact]
        public async Task when_executeasync_with_null_command_then_throws()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => new CommandBusComponent(Mock.Of<IComponentModel>()).ExecuteAsync(default(AsyncCommand)!, CancellationToken.None));
        }

        [Fact]
        public async Task when_executeasync_result_with_null_command_then_throws()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => new CommandBusComponent(Mock.Of<IComponentModel>()).ExecuteAsync(default(AsyncCommandWithResult)!, CancellationToken.None));
        }

        [Fact]
        public void when_executing_non_public_command_handler_then_invokes_handler_with_result()
        {
            var handler = new NonPublicCommandHandlerWithResults(new Result());
            var bus = new CommandBusComponent(Mock.Of<IComponentModel>(x =>
                x.GetExtensions<ICommandHandler<CommandWithResults, IEnumerable<Result>>>() == new[] { handler }));

            var results = bus.Execute(new CommandWithResults());

            Assert.Single(results);
        }

        [Fact]
        public void when_executing_command_as_explicit_ICommand_then_invokes_handler()
        {
            var handler = new Mock<ICommandHandler<Command>>();
            var command = new Command();
            var bus = new CommandBusComponent(Mock.Of<IComponentModel>(x =>
                x.GetExtensions<ICommandHandler<Command>>() == new[] { handler.Object }));

            bus.Execute((ICommand)command);

            handler.Verify(x => x.Execute(command));
        }

        [Fact]
        public void when_execute_command_throws_then_rethrows_original_exception()
        {
            var command = new Command();
            var bus = new CommandBusComponent(Mock.Of<IComponentModel>(x =>
                x.GetExtensions<ICommandHandler<Command>>() == new[] { new ThrowingCommandHandler() }));

            var ex = Assert.Throws<InvalidOperationException>(() => bus.Execute((ICommand)command));

            Assert.Equal("Invalid", ex.Message);
        }

        public class ThrowingCommandHandler : ICommandHandler<Command>
        {
            public bool CanExecute(Command command) => true;

            public void Execute(Command command)
            {
                throw new InvalidOperationException("Invalid");
            }
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
    }
}
