using System;
using System.Runtime.CompilerServices;
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

		public DispatcherAsyncManagerSpec (ITestOutputHelper output)
		{
			this.output = output;
		}

		[StaFact]
		public async void when_doing_then_does_async ()
		{
			var manager = new DispatcherAsyncManager(Dispatcher.CurrentDispatcher);

			await manager.SwitchToMainThread ();
			var mainThreadId = Thread.CurrentThread.ManagedThreadId;

			var backgroundId = 0;
			var foregroundId = mainThreadId;

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

			message = await manager.RunAsync (async () => {
				Assert.NotEqual (foregroundId, Thread.CurrentThread.ManagedThreadId);
				await Task.Yield ();
				return string.Format ("RunAsync<string> on {0} thread", (Thread.CurrentThread.ManagedThreadId == foregroundId) ? "main" : "background");
			});

			output.WriteLine (message);
		}
	}

	public class DispatcherAsyncManager : IAsyncManager
	{
		Dispatcher dispatcher;

        public DispatcherAsyncManager (Dispatcher dispatcher)
		{
			this.dispatcher = dispatcher;
		}

		public void Run (Func<Task> asyncMethod)
		{
			var done = new ManualResetEventSlim();
			AggregateException ex = null;
			asyncMethod ().ContinueWith (task => {
				ex = task.Exception;
				done.Set ();
			});

			done.Wait ();
			if (ex != null)
				throw ex.GetBaseException ();
		}

		public TResult Run<TResult>(Func<Task<TResult>> asyncMethod)
		{
			var done = new ManualResetEventSlim();
			AggregateException ex = null;
			TResult result = default(TResult);
			asyncMethod ().ContinueWith (task => {
				ex = task.Exception;
				if (!task.IsFaulted)
					result = task.Result;
				done.Set ();
			});

			done.Wait ();
			if (ex != null)
				throw ex.GetBaseException ();

			return result;
		}

		public IAwaitable RunAsync (Func<Task> asyncMethod) => new TaskAwaitable (asyncMethod ());

		public IAwaitable<TResult> RunAsync<TResult> (Func<Task<TResult>> asyncMethod) => new TaskAwaitable<TResult> (asyncMethod ());

		public IAwaitable SwitchToBackground () => new TaskSchedulerAwaitable (TaskScheduler.Default);

		public IAwaitable SwitchToMainThread () => new DispatcherAwaitable (dispatcher);

		class TaskAwaitable : IAwaitable
		{
			Task task;

			public TaskAwaitable (Task task)
			{
				this.task = task;
			}

			public IAwaiter GetAwaiter () => new Awaiter (task.GetAwaiter ());

			class Awaiter : IAwaiter
			{
				TaskAwaiter awaiter;

				public Awaiter (TaskAwaiter awaiter)
				{
					this.awaiter = awaiter;
				}

				public bool IsCompleted => awaiter.IsCompleted;

				public void GetResult ()
				{
					awaiter.GetResult ();	
				}

				public void OnCompleted (Action continuation)
				{
					awaiter.OnCompleted (continuation);
				}
			}
		}

		class TaskAwaitable<TResult> : IAwaitable<TResult>
		{
			Task<TResult> task;

			public TaskAwaitable (Task<TResult> task)
			{
				this.task = task;
			}

			public IAwaiter<TResult> GetAwaiter () => new Awaiter (task.GetAwaiter ());

			class Awaiter : IAwaiter<TResult>
			{
				TaskAwaiter<TResult> awaiter;

				public Awaiter (TaskAwaiter<TResult> awaiter)
				{
					this.awaiter = awaiter;
				}

				public bool IsCompleted => awaiter.IsCompleted;

				public TResult GetResult () => awaiter.GetResult ();

				public void OnCompleted (Action continuation)
				{
					awaiter.OnCompleted (continuation);
				}
			}
		}

		class TaskSchedulerAwaitable : IAwaitable
		{
			TaskScheduler scheduler;

			public TaskSchedulerAwaitable (TaskScheduler scheduler)
			{
				this.scheduler = scheduler;
			}

			public IAwaiter GetAwaiter () => scheduler.GetAwaiter ();
		}

		class DispatcherAwaitable : IAwaitable
		{
			Dispatcher dispatcher;

			public DispatcherAwaitable (Dispatcher dispatcher)
			{
				this.dispatcher = dispatcher;
			}

			public IAwaiter GetAwaiter () => new DispatcherAwaiter (dispatcher);

			class DispatcherAwaiter : IAwaiter
			{
				Dispatcher dispatcher;

				public DispatcherAwaiter (Dispatcher dispatcher)
				{
					this.dispatcher = dispatcher;
				}

				public bool IsCompleted => dispatcher.Thread == Thread.CurrentThread;

				public void GetResult ()
				{
				}

				public void OnCompleted (Action continuation)
				{
					dispatcher.BeginInvoke (continuation);
				}
			}
		}
	}
}
