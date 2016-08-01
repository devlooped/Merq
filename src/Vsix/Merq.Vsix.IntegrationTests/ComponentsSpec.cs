using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Xunit;

namespace Merq
{
	public class ComponentsSpec
	{
		[VsixFact]
		public void when_subscribing_and_pushing_events_then_succeeds()
		{
			var stream = GlobalServices.GetService<SComponentModel, IComponentModel>().GetService<IEventStream>();
			var expected = new FooEvent();

			FooEvent actual = null;

			var subscription = stream.Of<FooEvent>().Subscribe(e => actual = e);

			stream.Push(expected);

			Assert.Same(expected, actual);
		}

		[VsixFact]
		public void when_querying_command_bus_for_handler_then_succeeds()
		{
			var commands = GlobalServices.GetService<SComponentModel, IComponentModel>().GetService<ICommandBus>();

			Assert.False(commands.CanHandle(new FooCommand()));
			Assert.False(commands.CanHandle<FooAsyncCommand>());
		}

		[VsixFact]
		public async Task when_using_async_manager_then_succeeds()
		{
			var manager = GlobalServices.GetService<SComponentModel, IComponentModel>().GetService<IAsyncManager>();

			await manager.SwitchToBackground();

			Assert.True(true, "So true...");

			await manager.SwitchToMainThread();

			Assert.True(true, "Still true...");

			manager.Run(async () => await Task.Delay(10));

			await manager.RunAsync(async () => await Task.Delay(10));
		}


		public class FooCommand : ICommand { }

		public class FooAsyncCommand : IAsyncCommand { }

		public class FooEvent { }
	}
}
