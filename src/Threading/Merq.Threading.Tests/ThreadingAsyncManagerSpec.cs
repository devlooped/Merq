using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.VisualStudio.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Merq
{
	public class ThreadingAsyncManagerSpec
	{
		ITestOutputHelper output;

		public ThreadingAsyncManagerSpec (ITestOutputHelper output)
		{
			this.output = output;
		}

		[StaFact]
		public async void when_initializing_context_then_succeeds ()
		{
			var context = new JoinableTaskContext(Thread.CurrentThread, SynchronizationContext.Current);

			await context.Factory.SwitchToMainThreadAsync ();
			await TaskScheduler.Default.SwitchTo ();
			await context.Factory.SwitchToMainThreadAsync ();
		}

		[StaFact]
		public async void when_switching_to_background_thread_then_changes_current_thread_id ()
		{
			var manager = new ThreadingAsyncManager();

			var initialThreadId = Thread.CurrentThread.ManagedThreadId;

			await manager.SwitchToBackground ();

			Assert.NotEqual (initialThreadId, Thread.CurrentThread.ManagedThreadId);
		}

		[StaFact]
		public async void when_switching_to_main_thread_then_switches_to_dispatcher_thread ()
		{
			var manager = new ThreadingAsyncManager();
			var dispatcherThreadId = Dispatcher.CurrentDispatcher.Thread.ManagedThreadId;

			await manager.SwitchToBackground ();

			Assert.NotEqual (dispatcherThreadId, Thread.CurrentThread.ManagedThreadId);

			await manager.SwitchToMainThread ();

			Assert.Equal (dispatcherThreadId, Thread.CurrentThread.ManagedThreadId);
		}

		[StaFact]
		public void when_running_action_then_runs_on_current_thread ()
		{
			var manager = new ThreadingAsyncManager();
			var initialThreadId = Thread.CurrentThread.ManagedThreadId;

			manager.Run (async () => {
				Assert.Equal (initialThreadId, Thread.CurrentThread.ManagedThreadId);
				await Task.Yield ();
			});
		}

		[StaFact]
		public void when_running_function_then_runs_on_current_thread ()
		{
			var manager = new ThreadingAsyncManager();
			var initialThreadId = Thread.CurrentThread.ManagedThreadId;

			var runThreadId = manager.Run (async () => {
				await Task.Yield ();
				return Thread.CurrentThread.ManagedThreadId;
            });

			Assert.Equal (initialThreadId, runThreadId);
		}

		[StaFact]
		public async void when_running_async_action_then_runs_on_current_thread ()
		{
			var manager = new ThreadingAsyncManager();
			var initialThreadId = Thread.CurrentThread.ManagedThreadId;

			await manager.RunAsync (async () => {
				Assert.Equal (initialThreadId, Thread.CurrentThread.ManagedThreadId);
				await Task.Yield ();
			});
		}

		[StaFact]
		public async void when_running_async_function_then_runs_on_current_thread ()
		{
			var manager = new ThreadingAsyncManager();
			var initialThreadId = Thread.CurrentThread.ManagedThreadId;

			var runThreadId = await manager.RunAsync (async () => {
				var threadId = Thread.CurrentThread.ManagedThreadId;
				Assert.Equal (initialThreadId, threadId);
				await Task.Yield ();
				return threadId;
			});

			Assert.Equal (initialThreadId, runThreadId);
		}

		[StaFact]
		public async void when_doing_then_does_async ()
		{
			var manager = new ThreadingAsyncManager();

			await manager.SwitchToMainThread ();
			var mainThreadId = Thread.CurrentThread.ManagedThreadId;

			var backgroundId = 0;
			var foregroundId = 0;

			await manager.SwitchToBackground ();

			backgroundId = Thread.CurrentThread.ManagedThreadId;

			Assert.NotEqual (backgroundId, foregroundId);

			await manager.SwitchToMainThread ();

			foregroundId = Thread.CurrentThread.ManagedThreadId;
			Assert.Equal (mainThreadId, foregroundId);

			await manager.RunAsync (async () => {
				Assert.Equal (foregroundId, Thread.CurrentThread.ManagedThreadId);
				output.WriteLine ("RunAsync on {0} thread", (Thread.CurrentThread.ManagedThreadId == foregroundId) ? "main" : "background");
				await Task.Yield ();
			});

			var message = await manager.RunAsync(async () => {
				Assert.Equal (foregroundId, Thread.CurrentThread.ManagedThreadId);
				await Task.Yield ();
				return string.Format ("RunAsync<string> on {0} thread", (Thread.CurrentThread.ManagedThreadId == foregroundId) ? "main" : "background");
			});

			output.WriteLine (message);

			await manager.SwitchToBackground ();

			Assert.NotEqual (foregroundId, Thread.CurrentThread.ManagedThreadId);

			await manager.RunAsync (async () => {
				Assert.NotEqual (foregroundId, Thread.CurrentThread.ManagedThreadId);
				output.WriteLine ("RunAsync on {0} thread", (Thread.CurrentThread.ManagedThreadId == foregroundId) ? "main" : "background");
				await Task.Yield ();
			});

			message = await manager.RunAsync(async () => {
				Assert.NotEqual (foregroundId, Thread.CurrentThread.ManagedThreadId);
				await Task.Yield ();
				return string.Format ("RunAsync<string> on {0} thread", (Thread.CurrentThread.ManagedThreadId == foregroundId) ? "main" : "background");
			});

			output.WriteLine (message);
		}
	}
}