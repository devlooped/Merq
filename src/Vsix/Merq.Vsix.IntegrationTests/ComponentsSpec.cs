using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ComponentModel.Composition.Diagnostics;
using Microsoft.VisualStudio.ComponentModelHost;
using Xunit;
using Xunit.Abstractions;

namespace Merq
{
	public class ComponentsSpec
	{
		ITestOutputHelper output;

		public ComponentsSpec(ITestOutputHelper output)
		{
			this.output = output;
		}

		[InlineData(typeof(IAsyncManager))]
		[InlineData(typeof(ICommandBus))]
		[InlineData(typeof(IEventStream))]
		[VsixTheory]
		public void when_retrieving_exports_then_reports_duplicate_services(Type serviceType)
		{
			var componentModel = GlobalServices.GetService<SComponentModel, IComponentModel>();
			var contractName = AttributedModelServices.GetContractName(serviceType);
			var components = componentModel.DefaultExportProvider
				.GetExports<object, IDictionary<string, object>>(contractName)
				.ToArray();

			if (components.Length != 1)
			{
				var info = new CompositionInfo(componentModel.DefaultCatalog, componentModel.DefaultExportProvider);
				var log = Path.GetTempFileName();
				using (var writer = new StreamWriter(log))
				{
					CompositionInfoTextFormatter.Write(info, writer);
					writer.Flush();
				}

				output.WriteLine(log);
				// Process.Start(new ProcessStartInfo("notepad", log) { UseShellExecute = true });

				Assert.False(true, $"Expected only one component of {serviceType.Name}. Composition log at {log}");
			}
		}

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
