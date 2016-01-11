using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.VisualStudio.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Merq
{
	public class ThreadingAsyncManagerSpec : BaseAsyncManagerSpec
	{
		public ThreadingAsyncManagerSpec (ITestOutputHelper output)
			: base (output)
		{
		}

		protected override IAsyncManager CreateAsyncManager () => new AsyncManager ();

		[StaFact]
		public async void when_initializing_context_then_succeeds ()
		{
			var context = new JoinableTaskContext(Thread.CurrentThread, SynchronizationContext.Current);

			await context.Factory.SwitchToMainThreadAsync ();
			await TaskScheduler.Default.SwitchTo ();
			await context.Factory.SwitchToMainThreadAsync ();
		}
	}
}