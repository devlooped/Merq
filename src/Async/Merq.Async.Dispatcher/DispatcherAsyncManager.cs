using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Merq
{
	/// <summary>
	/// Alternative implementation of <see cref="IAsyncManager"/> based on 
	/// a <see cref="Dispatcher"/>
	/// </summary>
	/// <remarks>
	/// This is a very simple async manager that does not perform any 
	/// reentrancy detection and deadlock avoidance. For those features
	/// use the <c>Merq.Async.Core</c> package and its <c>AsyncManager</c> instead.
	/// </remarks>
	public class DispatcherAsyncManager : IAsyncManager
	{
		readonly Dispatcher dispatcher;

		/// <summary>
		/// Initializes the <see cref="DispatcherAsyncManager"/> using the 
		/// specified <paramref name="dispatcher"/> as the main 
		/// thread scheduler.
		/// </summary>
		public DispatcherAsyncManager(Dispatcher dispatcher)
			=> this.dispatcher = dispatcher;

		/// <summary>
		/// Runs the specified asynchronous method to completion while synchronously blocking the calling thread.
		/// </summary>
		/// <param name="asyncMethod">The asynchronous method to execute.</param>
		/// <remarks>
		/// <para>Any exception thrown by the delegate is rethrown in its original type to the caller of this method.</para>
		/// <para>When the delegate resumes from a yielding await, the default behavior is to resume in its original context
		/// as an ordinary async method execution would. For example, if the caller was on the main thread, execution
		/// resumes after an await on the main thread; but if it started on a threadpool thread it resumes on a threadpool thread.</para>
		/// <example>
		/// <code>
		/// // On threadpool or Main thread, this method will block
		/// // the calling thread until all async operations in the
		/// // delegate complete.
		/// asyncManager.Run(async () => {
		///		// still on the threadpool or Main thread as before.
		///		await OperationAsync();
		///		// still on the threadpool or Main thread as before.
		///		await Task.Run(async () => {
		///			// Now we're on a threadpool thread.
		///			await Task.Yield();
		///			// still on a threadpool thread.
		///		});
		///	});
		/// // Now back on the Main thread (or threadpool thread if that's where we started).
		/// </code>
		/// </example>
		/// </remarks>
		public void Run(Func<Task> asyncMethod)
		{
			var done = new ManualResetEventSlim();
			AggregateException ex = null;
			asyncMethod().ContinueWith(task =>
			{
				ex = task.Exception;
				done.Set();
			});

			done.Wait();
			if (ex != null)
				throw ex.GetBaseException();
		}

		/// <summary>
		/// Runs the specified asynchronous method to completion while synchronously blocking the calling thread.
		/// </summary>
		/// <typeparam name="TResult">The type of value returned by the asynchronous operation.</typeparam>
		/// <param name="asyncMethod">The asynchronous method to execute.</param>
		/// <returns>The result of the Task returned by <paramref name="asyncMethod" />.</returns>
		/// <remarks>
		/// <para>Any exception thrown by the delegate is rethrown in its original type to the caller of this method.</para>
		/// <para>When the delegate resumes from a yielding await, the default behavior is to resume in its original context
		/// as an ordinary async method execution would. For example, if the caller was on the main thread, execution
		/// resumes after an await on the main thread; but if it started on a threadpool thread it resumes on a threadpool thread.</para>
		/// <para>See the <see cref="Run(Func{Task})" /> overload documentation for an example.</para>
		/// </remarks>
		public TResult Run<TResult>(Func<Task<TResult>> asyncMethod)
		{
			var done = new ManualResetEventSlim();
			AggregateException ex = null;
			TResult result = default(TResult);
			asyncMethod().ContinueWith(task =>
			{
				ex = task.Exception;
				if (!task.IsFaulted)
					result = task.Result;
				done.Set();
			});

			done.Wait();
			if (ex != null)
				throw ex.GetBaseException();

			return result;
		}

		/// <summary>
		/// Invokes an async delegate on the caller's thread, and yields back to the caller when the async method yields.
		/// </summary>
		/// <param name="asyncMethod">The method that, when executed, will begin the async operation.</param>
		public IAwaitable RunAsync(Func<Task> asyncMethod)
			=> new TaskAwaitable(asyncMethod());

		/// <summary>
		/// Invokes an async delegate on the caller's thread, and yields back to the caller when the async method yields.
		/// </summary>
		/// <typeparam name="TResult">The type of value returned by the asynchronous operation.</typeparam>
		/// <param name="asyncMethod">The method that, when executed, will begin the async operation.</param>
		public IAwaitable<TResult> RunAsync<TResult>(Func<Task<TResult>> asyncMethod)
			=> new TaskAwaitable<TResult>(asyncMethod());

		/// <summary>
		/// Gets an awaitable that schedules continuations on the default background scheduler.
		/// </summary>
		public IAwaitable SwitchToBackground() => new TaskSchedulerAwaitable(TaskScheduler.Default);

		/// <summary>
		/// Gets an awaitable whose continuations execute on the dispatcher context that 
		/// the manager was initialized with.
		/// </summary>
		/// <remarks>
		/// <example>
		/// <code>
		/// async Task SomeOperationAsync() {
		///		// on the caller's thread.
		///		await DoAsync();
		///		
		///		// Now switch to a threadpool thread explicitly.
		///		await asyncManager.SwitchToBackground();
		///		
		///		// Now switch to the Main thread to talk to some STA object.
		///		await asyncManager.SwitchToMainThread();
		///		STAService.DoSomething();
		/// }
		/// </code>
		/// </example>
		/// </remarks>
		public IAwaitable SwitchToMainThread() => new DispatcherAwaitable(dispatcher);

		class DispatcherAwaitable : IAwaitable
		{
			readonly Dispatcher dispatcher;

			public DispatcherAwaitable(Dispatcher dispatcher)
				=> this.dispatcher = dispatcher;

			public IAwaiter GetAwaiter() 
				=> new DispatcherAwaiter(dispatcher);

			class DispatcherAwaiter : IAwaiter
			{
				readonly Dispatcher dispatcher;

				public DispatcherAwaiter(Dispatcher dispatcher)
					=> this.dispatcher = dispatcher;

				public bool IsCompleted 
					=> dispatcher.Thread == Thread.CurrentThread;

				public void GetResult() { }

				public void OnCompleted(Action continuation)
					=> dispatcher.BeginInvoke(continuation);
			}
		}
	}
}
