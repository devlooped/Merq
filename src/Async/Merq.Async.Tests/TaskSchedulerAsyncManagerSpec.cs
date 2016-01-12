using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.VisualStudio.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Merq
{
	public class TaskSchedulerAsyncManagerSpec : BaseAsyncManagerSpec
	{
		public TaskSchedulerAsyncManagerSpec (ITestOutputHelper output)
			: base (output)
		{
		}

		[Fact]
		public void when_scheduler_is_null_then_throws ()
		{
			Assert.Throws<ArgumentNullException> (() => new TaskSchedulerAsyncManager (null));
		}

		protected override IAsyncManager CreateAsyncManager () => new TaskSchedulerAsyncManager ();
	}
}