using System.Threading.Tasks;

namespace Merq
{
	internal class TaskSchedulerAwaitable : IAwaitable
	{
		readonly TaskScheduler scheduler;

		public TaskSchedulerAwaitable(TaskScheduler scheduler)
			=> this.scheduler = scheduler;

		public IAwaiter GetAwaiter()
			=> scheduler.GetAwaiter();
	}
}
