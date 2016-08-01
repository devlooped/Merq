using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Moq;
using Xunit;

namespace Merq
{
	public class AsyncManagerProviderSpec
	{
		[Fact]
		public void when_retrieving_async_manager_without_task_context_then_throws()
		{
			var manager = new AsyncManagerProvider(Mock.Of<IServiceProvider>());

			Assert.Throws<NotSupportedException>(() => manager.AsyncManager);
		}

		[Fact]
		public void when_retrieving_async_manager_dev11_then_uses_legacy_task_context()
		{
			var context = new JoinableTaskContext();
			var services = Mock.Of<IServiceProvider>(x =>
				x.GetService(typeof(SVsJoinableTaskContext)) == context);

			var manager = new AsyncManagerProvider(services);
			var actual = typeof(AsyncManager).GetField("context", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager.AsyncManager);

			Assert.Same(context, actual);
		}

		[Fact]
		public void when_retrieving_async_manager_dev12_or_greater_then_uses_new_task_scheduler_service()
		{
			var context = new JoinableTaskContext();
			var services = Mock.Of<IServiceProvider>(x =>
				x.GetService(typeof(SVsTaskSchedulerService)) == Mock.Of<IVsTaskSchedulerService2>(t =>
					t.GetAsyncTaskContext() == context));

			var manager = new AsyncManagerProvider(services);
			var actual = typeof(AsyncManager).GetField("context", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager.AsyncManager);

			Assert.Same(context, actual);
		}



	}
}
