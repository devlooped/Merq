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

		protected override IAsyncManager CreateAsyncManager () => new TaskSchedulerAsyncManager ();
	}
}