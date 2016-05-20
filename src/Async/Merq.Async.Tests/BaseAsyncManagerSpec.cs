using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Merq
{
	public abstract class BaseAsyncManagerSpec
	{
		ITestOutputHelper output;

		public BaseAsyncManagerSpec (ITestOutputHelper output)
		{
			this.output = output;
		}

		protected abstract IAsyncManager CreateAsyncManager();

		[StaFact]
		public async void when_switching_to_background_thread_then_changes_current_thread_id ()
		{
			var manager = CreateAsyncManager();

			var initialThreadId = Thread.CurrentThread.ManagedThreadId;

			await manager.SwitchToBackground ();

			Assert.NotEqual (initialThreadId, Thread.CurrentThread.ManagedThreadId);
		}

		[StaFact]
		public async void when_switching_to_main_thread_then_switches_to_initial_thread ()
		{
			var manager = CreateAsyncManager();
			var initialThreadId = Thread.CurrentThread.ManagedThreadId;

			await manager.SwitchToBackground ();

			Assert.NotEqual (initialThreadId, Thread.CurrentThread.ManagedThreadId);

			await manager.SwitchToMainThread ();

			Assert.Equal (initialThreadId, Thread.CurrentThread.ManagedThreadId);
		}

		[StaFact]
		public async void when_switching_to_main_thread_then_switches_to_dispatcher_thread ()
		{
			var manager = CreateAsyncManager();
			var dispatcherThreadId = Dispatcher.CurrentDispatcher.Thread.ManagedThreadId;

			await manager.SwitchToBackground ();

			Assert.NotEqual (dispatcherThreadId, Thread.CurrentThread.ManagedThreadId);

			await manager.SwitchToMainThread ();

			Assert.Equal (dispatcherThreadId, Thread.CurrentThread.ManagedThreadId);
		}

		[Fact]
		public void when_running_action_then_runs_on_current_thread ()
		{
			var manager = CreateAsyncManager();
			var initialThreadId = Thread.CurrentThread.ManagedThreadId;

			manager.Run (async () => {
				Assert.Equal (initialThreadId, Thread.CurrentThread.ManagedThreadId);
				await Task.Yield ();
			});
		}

		[Fact]
		public void when_running_function_then_runs_on_current_thread ()
		{
			var manager = CreateAsyncManager();
			var initialThreadId = Thread.CurrentThread.ManagedThreadId;

			var runThreadId = manager.Run (async () => {
				var threadId = Thread.CurrentThread.ManagedThreadId;
				Assert.Equal (initialThreadId, threadId);
				await Task.Yield ();
				return threadId;
			});

			Assert.Equal (initialThreadId, runThreadId);
		}

		[Fact]
		public void when_run_result_then_returns_value ()
		{
			var manager = CreateAsyncManager();
			var initialThreadId = Thread.CurrentThread.ManagedThreadId;

			var result = manager.Run (() => Task.FromResult("foo"));

			Assert.Equal ("foo", result);
		}

		[Fact]
		public async Task when_runasync_result_then_returns_value ()
		{
			var manager = CreateAsyncManager();
			var initialThreadId = Thread.CurrentThread.ManagedThreadId;

			var result = await manager.RunAsync (() => Task.FromResult("foo"));
			
			await Task.Yield ();

			Assert.Equal ("foo", result);
		}
		
		[Fact]
		public void when_run_throws_then_propagates_exception ()
		{
			var manager = CreateAsyncManager();
			var initialThreadId = Thread.CurrentThread.ManagedThreadId;

			Assert.Throws<InvalidOperationException>(() => manager.Run (async () => {
				await Task.Yield ();
				throw new InvalidOperationException ();
			}));
		}

		[Fact]
		public void when_runasync_throws_then_propagates_exception ()
		{
			var manager = CreateAsyncManager();
			var initialThreadId = Thread.CurrentThread.ManagedThreadId;

			Assert.ThrowsAsync<InvalidOperationException> (async () => await manager.RunAsync (async () => {
				await Task.Yield ();
				throw new InvalidOperationException ();
			}));
		}

		[Fact]
		public void when_run_result_throws_then_propagates_exception ()
		{
			var manager = CreateAsyncManager();
			var initialThreadId = Thread.CurrentThread.ManagedThreadId;

			Assert.Throws<InvalidOperationException> (() => manager.Run<bool> (async () => {
				await Task.Yield ();
				throw new InvalidOperationException ();
			}));
		}

		[Fact]
		public void when_runasync_result_throws_then_propagates_exception ()
		{
			var manager = CreateAsyncManager();
			var initialThreadId = Thread.CurrentThread.ManagedThreadId;

			Assert.ThrowsAsync<InvalidOperationException> (async () => await manager.RunAsync<bool> (async () => {
				await Task.Yield ();
				throw new InvalidOperationException ();
			}));
		}

		[Fact]
		public async void when_running_async_action_then_runs_on_current_thread ()
		{
			var manager = CreateAsyncManager();
			var initialThreadId = Thread.CurrentThread.ManagedThreadId;

			await manager.RunAsync (async () => {
				Assert.Equal (initialThreadId, Thread.CurrentThread.ManagedThreadId);
				await Task.Yield ();
			});
		}

		[Fact]
		public async void when_running_async_function_then_runs_on_current_thread ()
		{
			var manager = CreateAsyncManager();
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
			var manager = CreateAsyncManager();

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

			var runThreadId = await manager.RunAsync (async () => {
				output.WriteLine ("RunAsync on {0} thread", (Thread.CurrentThread.ManagedThreadId == foregroundId) ? "main" : "background");
				await Task.Yield ();
				return Thread.CurrentThread.ManagedThreadId;
			});

			Assert.Equal (foregroundId, runThreadId);

			var message = await manager.RunAsync(async () => {
				runThreadId = Thread.CurrentThread.ManagedThreadId;
				await Task.Yield ();
				return string.Format ("RunAsync<string> on {0} thread", (Thread.CurrentThread.ManagedThreadId == foregroundId) ? "main" : "background");
			});

			Assert.Equal (foregroundId, runThreadId);

			output.WriteLine (message);

			await manager.SwitchToBackground ();

			Assert.NotEqual (foregroundId, Thread.CurrentThread.ManagedThreadId);

			runThreadId = await manager.RunAsync (async () => {
				output.WriteLine ("RunAsync on {0} thread", (Thread.CurrentThread.ManagedThreadId == foregroundId) ? "main" : "background");
				await Task.Yield ();
				return Thread.CurrentThread.ManagedThreadId;
			});

			Assert.NotEqual (foregroundId, runThreadId);

			message = await manager.RunAsync (async () => {
				runThreadId = Thread.CurrentThread.ManagedThreadId;
				await Task.Yield ();
				return string.Format ("RunAsync<string> on {0} thread", (Thread.CurrentThread.ManagedThreadId == foregroundId) ? "main" : "background");
			});

			Assert.NotEqual (foregroundId, runThreadId);

			output.WriteLine (message);
		}
	}
}
