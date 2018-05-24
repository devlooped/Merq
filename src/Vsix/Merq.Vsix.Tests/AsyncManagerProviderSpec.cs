using System;
using System.Reflection;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Moq;
using Xunit;

namespace Merq
{
	public class AsyncManagerProviderSpec
	{
		[Fact]
		public void when_retrieving_async_manager_dev12_or_greater_then_uses_new_task_scheduler_service()
		{
			var context = new JoinableTaskContext();
			var manager = new AsyncManagerProvider(context);
			var actual = typeof(AsyncManager).GetField("context", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager.AsyncManager);

			Assert.Same(context, actual);
		}
	}
}
