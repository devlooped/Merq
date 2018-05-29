using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Merq
{
	internal class TaskAwaitable : IAwaitable
	{
		readonly Task task;

		public TaskAwaitable(Task task)
			=> this.task = task;

		public IAwaiter GetAwaiter()
			=> new Awaiter(task.GetAwaiter());

		class Awaiter : IAwaiter
		{
			readonly TaskAwaiter awaiter;

			public Awaiter(TaskAwaiter awaiter)
				=> this.awaiter = awaiter;

			public bool IsCompleted
				=> awaiter.IsCompleted;

			public void GetResult()
				=> awaiter.GetResult();

			public void OnCompleted(Action continuation)
				=> awaiter.OnCompleted(continuation);
		}
	}

	internal class TaskAwaitable<TResult> : IAwaitable<TResult>
	{
		readonly Task<TResult> task;

		public TaskAwaitable(Task<TResult> task)
			=> this.task = task;

		public IAwaiter<TResult> GetAwaiter()
			=> new Awaiter(task.GetAwaiter());

		class Awaiter : IAwaiter<TResult>
		{
			readonly TaskAwaiter<TResult> awaiter;

			public Awaiter(TaskAwaiter<TResult> awaiter)
				=> this.awaiter = awaiter;

			public bool IsCompleted
				=> awaiter.IsCompleted;

			public TResult GetResult()
				=> awaiter.GetResult();

			public void OnCompleted(Action continuation)
				=> awaiter.OnCompleted(continuation);
		}
	}
}
