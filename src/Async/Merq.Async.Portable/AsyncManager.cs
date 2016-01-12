using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Merq
{
	/// <summary>
	/// Default implementation of <see cref="IAsyncManager"/>, based on the
	/// Microsoft.VisualStudio.Threading nuget package, which provides 
	/// reentrancy deadlocks detection and avoidance.
	/// </summary>
	/// <remarks>
	/// This class must be intantiated only once for the entire app domain, 
	/// or a singleton <see cref="JoinableTaskContext"/> must be provided, 
	/// in order for the deadlock and reentrancy detection to work properly.
	/// </remarks>
	/// <seealso cref="IAsyncManager"/>
	public class AsyncManager : IAsyncManager
	{
		readonly JoinableTaskContext context;

		/// <summary>
		/// Initializes a new instance of the <see cref="AsyncManager" /> class 
		/// using the current synchronization context as the application main thread.
		/// </summary>
		public AsyncManager ()
			: this (new JoinableTaskContext ())
		{
		}

		/// <summary>
		/// In order to ensure proper behavior for this manager, the received 
		/// <see cref="JoinableTaskContext"/> must be an application-wide singleton.
		/// </summary>
		public AsyncManager (JoinableTaskContext context)
		{
			if (context == null) throw new ArgumentNullException (nameof (context));

			this.context = context;
		}

		/// <summary>
		/// See <see cref="IAsyncManager.SwitchToBackground"/>.
		/// </summary>
		public virtual IAwaitable SwitchToBackground () => new TaskSchedulerAwaitable (TaskScheduler.Default.SwitchTo ());

		/// <summary>
		/// See <see cref="IAsyncManager.SwitchToMainThread"/>.
		/// </summary>
		public virtual IAwaitable SwitchToMainThread () => new MainThreadAwaitable (context.Factory.SwitchToMainThreadAsync ());

		/// <summary>
		/// See <see cref="IAsyncManager.Run(Func{Task})"/>.
		/// </summary>
		public virtual void Run (Func<Task> asyncMethod)
		{
			context.Factory.Run (asyncMethod);
		}

		/// <summary>
		/// See <see cref="IAsyncManager.Run{TResult}(Func{Task{TResult}})"/>.
		/// </summary>
		public virtual T Run<T> (Func<Task<T>> asyncMethod) => context.Factory.Run (asyncMethod);

		/// <summary>
		/// See <see cref="IAsyncManager.RunAsync(Func{Task})"/>.
		/// </summary>
		public virtual IAwaitable RunAsync (Func<Task> asyncMethod) => new JoinableTaskAwaitable (context.Factory.RunAsync (asyncMethod));

		/// <summary>
		/// See <see cref="IAsyncManager.RunAsync{TResult}(Func{Task{TResult}})"/>.
		/// </summary>
		public virtual IAwaitable<TResult> RunAsync<TResult> (Func<Task<TResult>> asyncMethod) => new JoinableTaskAwaitable<TResult> (context.Factory.RunAsync (asyncMethod));

		class TaskSchedulerAwaitable : IAwaitable
		{
			Microsoft.VisualStudio.Threading.AwaitExtensions.TaskSchedulerAwaitable awaitable;

			public TaskSchedulerAwaitable (Microsoft.VisualStudio.Threading.AwaitExtensions.TaskSchedulerAwaitable awaitable)
			{
				this.awaitable = awaitable;
			}

			public IAwaiter GetAwaiter () => new TaskSchedulerAwaiter (awaitable.GetAwaiter ());

			class TaskSchedulerAwaiter : IAwaiter
			{
				Microsoft.VisualStudio.Threading.AwaitExtensions.TaskSchedulerAwaiter awaiter;

				public TaskSchedulerAwaiter (Microsoft.VisualStudio.Threading.AwaitExtensions.TaskSchedulerAwaiter awaiter)
				{
					this.awaiter = awaiter;
				}

				public bool IsCompleted => awaiter.IsCompleted;

				public void GetResult () => awaiter.GetResult ();

				public void OnCompleted (Action continuation) => awaiter.OnCompleted (continuation);
			}
		}

		class MainThreadAwaitable : IAwaitable
		{
			JoinableTaskFactory.MainThreadAwaitable awaitable;

			public MainThreadAwaitable (JoinableTaskFactory.MainThreadAwaitable awaitable)
			{
				this.awaitable = awaitable;
			}


			public IAwaiter GetAwaiter () => new MainThreadAwaiter (awaitable.GetAwaiter ());

			class MainThreadAwaiter : IAwaiter
			{
				JoinableTaskFactory.MainThreadAwaiter awaiter;

				public MainThreadAwaiter (JoinableTaskFactory.MainThreadAwaiter awaiter)
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

		class JoinableTaskAwaitable : IAwaitable
		{
			JoinableTask task;

			public JoinableTaskAwaitable (JoinableTask task)
			{
				this.task = task;
			}

			public IAwaiter GetAwaiter () => new TaskAwaiter (task);

			class TaskAwaiter : IAwaiter
			{
				System.Runtime.CompilerServices.TaskAwaiter awaiter;

				public TaskAwaiter (JoinableTask task)
				{
					awaiter = task.GetAwaiter ();
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

		class JoinableTaskAwaitable<TResult> : IAwaitable<TResult>
		{
			JoinableTask<TResult> task;

			public JoinableTaskAwaitable (JoinableTask<TResult> task)
			{
				this.task = task;
			}

			public IAwaiter<TResult> GetAwaiter () => new TaskAwaiter (task);

			class TaskAwaiter : IAwaiter<TResult>
			{
				TaskAwaiter<TResult> awaiter;

				public TaskAwaiter (JoinableTask<TResult> task)
				{
					awaiter = task.GetAwaiter ();
				}

				public bool IsCompleted => awaiter.IsCompleted;

				public TResult GetResult () => awaiter.GetResult ();

				public void OnCompleted (Action continuation)
				{
					awaiter.OnCompleted (continuation);
				}
			}
		}
	}
}
