using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Merq
{
	public class ThreadingAsyncManagerSpec : BaseAsyncManagerSpec
	{
		public ThreadingAsyncManagerSpec(ITestOutputHelper output)
			: base(output)
		{
		}


		[Fact]
		public void when_context_is_null_then_throws()
		{
			Assert.Throws<ArgumentNullException>(() => new AsyncManager(null));
		}

		[StaFact]
		public async void when_initializing_context_then_succeeds()
		{
			var context = new JoinableTaskContext(Thread.CurrentThread, SynchronizationContext.Current);

			await context.Factory.SwitchToMainThreadAsync();
			await TaskScheduler.Default.SwitchTo();
			await context.Factory.SwitchToMainThreadAsync();

			Assert.True(true);
		}

		protected override IAsyncManager CreateAsyncManager() => new AsyncManager();
	}
}