using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Merq;

/// <summary>
/// Allows awaiting a <see cref="TaskScheduler"/> to schedule 
/// continuations on it.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AwaitExtensions
{
	/// <summary>
	/// Gets an awaiter that schedules continuations on the specified scheduler.
	/// </summary>
	/// <param name="scheduler">The task scheduler used to execute continuations.</param>
	[EditorBrowsable (EditorBrowsableState.Never)]
	public static IAwaiter GetAwaiter (this TaskScheduler scheduler)
	{
		Guard.NotNull("scheduler", scheduler);
		return new TaskSchedulerAwaiter (scheduler);
	}

	class TaskSchedulerAwaiter : IAwaiter
	{
		TaskScheduler scheduler;

        public TaskSchedulerAwaiter (TaskScheduler scheduler)
		{
			this.scheduler = scheduler;
		}

		public bool IsCompleted
		{
			get
			{
				var isThreadPoolThread = SynchronizationContext.Current == null;
				return
					((scheduler == TaskScheduler.Default) & isThreadPoolThread) || 
					((scheduler == TaskScheduler.Current) && (TaskScheduler.Current != TaskScheduler.Default));
			}
		}

		public void GetResult ()
		{
		}

		public void OnCompleted (Action continuation)
		{
			Task.Factory.StartNew (continuation, CancellationToken.None, TaskCreationOptions.None, scheduler);
		}
	}
}
