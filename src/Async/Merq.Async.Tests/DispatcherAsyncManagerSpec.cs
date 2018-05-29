using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Merq
{
	// Poor-man's async manager in case we can't have the full Threading library in VS2012.
	// NOTE: this is a sample
	class DispatcherAsyncManagerSpec
	{
		ITestOutputHelper output;

		public DispatcherAsyncManagerSpec(ITestOutputHelper output)
		{
			this.output = output;
		}

		[StaFact]
		public async void when_doing_then_does_async()
		{
			var manager = new DispatcherAsyncManager(Dispatcher.CurrentDispatcher);

			await manager.SwitchToMainThread();
			var mainThreadId = Thread.CurrentThread.ManagedThreadId;

			var backgroundId = 0;
			var foregroundId = mainThreadId;

			await manager.SwitchToBackground();

			backgroundId = Thread.CurrentThread.ManagedThreadId;

			Assert.NotEqual(backgroundId, foregroundId);

			await manager.SwitchToMainThread();

			foregroundId = Thread.CurrentThread.ManagedThreadId;
			Assert.Equal(mainThreadId, foregroundId);

			await manager.RunAsync(async () =>
			{
				Assert.Equal(foregroundId, Thread.CurrentThread.ManagedThreadId);
				output.WriteLine("RunAsync on {0} thread", (Thread.CurrentThread.ManagedThreadId == foregroundId) ? "main" : "background");
				await Task.Yield();
			});

			var message = await manager.RunAsync(async () =>
			{
				Assert.Equal(foregroundId, Thread.CurrentThread.ManagedThreadId);
				await Task.Yield();
				return string.Format("RunAsync<string> on {0} thread", (Thread.CurrentThread.ManagedThreadId == foregroundId) ? "main" : "background");
			});

			output.WriteLine(message);

			await manager.SwitchToBackground();

			Assert.NotEqual(foregroundId, Thread.CurrentThread.ManagedThreadId);

			await manager.RunAsync(async () =>
			{
				Assert.NotEqual(foregroundId, Thread.CurrentThread.ManagedThreadId);
				output.WriteLine("RunAsync on {0} thread", (Thread.CurrentThread.ManagedThreadId == foregroundId) ? "main" : "background");
				await Task.Yield();
			});

			message = await manager.RunAsync(async () =>
			{
				Assert.NotEqual(foregroundId, Thread.CurrentThread.ManagedThreadId);
				await Task.Yield();
				return string.Format("RunAsync<string> on {0} thread", (Thread.CurrentThread.ManagedThreadId == foregroundId) ? "main" : "background");
			});

			output.WriteLine(message);
		}
	}
}
