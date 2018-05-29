using System;
using Xunit;
using Xunit.Abstractions;

namespace Merq
{
	public class TaskSchedulerAsyncManagerSpec : BaseAsyncManagerSpec
	{
		public TaskSchedulerAsyncManagerSpec(ITestOutputHelper output)
			: base(output)
		{
		}

		[Fact]
		public void when_scheduler_is_null_then_throws()
			=> Assert.Throws<ArgumentNullException>(() => new TaskSchedulerAsyncManager(null));

		protected override IAsyncManager CreateAsyncManager() => new TaskSchedulerAsyncManager();
	}
}