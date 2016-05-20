using System;
using System.Threading.Tasks;

namespace Merq
{
	/// <summary>
	/// A unified way of starting and executing asynchronous tasks to avoid deadlocks.
	/// </summary>
	/// <remarks>
	/// There are three rules that should be strictly followed when using or interacting
	/// with tasks that have UI/Main thread interaction requirements:
	/// 1. If a method has certain thread apartment requirements (STA or MTA) it must either:
	///		a)	Have an asynchronous signature, and asynchronously marshal to the appropriate
	///			thread if it isn't originally invoked on a compatible thread.
	///			The recommended way to switch to the main thread is:
	///			<code>
	///			await asyncManager.SwitchToMainThread();
	///			</code>
	///		b)	Have a synchronous signature, and throw an exception when called on the wrong thread.
	///			In particular, no method is allowed to synchronously marshal work to another thread
	///			(blocking while that work is done). Synchronous blocks in general are to be avoided
	///			whenever possible.
	/// 
	/// 2. When an implementation of an already-shipped public API must call asynchronous code
	/// and block for its completion, it must do so by following this simple pattern:
	///		<code>
	///		asyncManager.Run(async () => await SomeOperationAsync(...));
	///		</code>
	/// 
	/// 3. If ever awaiting work that was started earlier, that work must be Joined.
	/// For example, one service kicks off some asynchronous work that may later become
	/// synchronously blocking:
	///		<code>
	///		var longRunningAsyncWork = asyncManager.RunAsync(async () => await SomeOperationAsync(...));
	///		</code>
	/// 
	/// Then later that async work becomes blocking:
	/// <code>
	/// await longRunningAsyncWork;
	/// </code>
	/// </remarks>
	public interface IAsyncManager : IFluentInterface
	{
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
		void Run (Func<Task> asyncMethod);

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
		TResult Run<TResult>(Func<Task<TResult>> asyncMethod);

		/// <summary>
		/// Invokes an async delegate on the caller's thread, and yields back to the caller when the async method yields.
		/// The async delegate is invoked in such a way as to mitigate deadlocks in the event that the async method
		/// requires the main thread while the main thread is blocked waiting for the async method's completion.
		/// </summary>
		/// <param name="asyncMethod">The method that, when executed, will begin the async operation.</param>
		/// <returns>An object that tracks the completion of the async operation, and allows for later synchronous blocking of the main thread for completion if necessary.</returns>
		/// <remarks>
		/// <para>Exceptions thrown by the delegate are captured by the returned awaitable.</para>
		/// <para>When the delegate resumes from a yielding await, the default behavior is to resume in its original context
		/// as an ordinary async method execution would. For example, if the caller was on the main thread, execution
		/// resumes after an await on the main thread; but if it started on a threadpool thread it resumes on a threadpool thread.</para>
		/// </remarks>
		IAwaitable RunAsync (Func<Task> asyncMethod);

		/// <summary>
		/// Invokes an async delegate on the caller's thread, and yields back to the caller when the async method yields.
		/// The async delegate is invoked in such a way as to mitigate deadlocks in the event that the async method
		/// requires the main thread while the main thread is blocked waiting for the async method's completion.
		/// </summary>
		/// <typeparam name="TResult">The type of value returned by the asynchronous operation.</typeparam>
		/// <param name="asyncMethod">The method that, when executed, will begin the async operation.</param>
		/// <returns>
		/// An object that tracks the completion of the async operation, and allows for later synchronous blocking of the main thread for completion if necessary.
		/// </returns>
		/// <remarks>
		/// <para>Exceptions thrown by the delegate are captured by the returned awaitable.</para>
		/// <para>When the delegate resumes from a yielding await, the default behavior is to resume in its original context
		/// as an ordinary async method execution would. For example, if the caller was on the main thread, execution
		/// resumes after an await on the main thread; but if it started on a threadpool thread it resumes on a threadpool thread.</para>
		/// </remarks>
		IAwaitable<TResult> RunAsync<TResult>(Func<Task<TResult>> asyncMethod);

		/// <summary>
		/// Gets an awaitable that schedules continuations on the default background scheduler.
		/// </summary>
		IAwaitable SwitchToBackground ();

		/// <summary>
		/// Gets an awaitable whose continuations execute on the synchronization context that 
		/// the manager was initialized with, in such a way as to mitigate both deadlocks and reentrancy.
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
		IAwaitable SwitchToMainThread ();
	}
}
