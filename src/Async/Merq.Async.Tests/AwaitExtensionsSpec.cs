using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Merq.Async.Tests
{
	public class AwaitExtensionsSpec
	{
		[Fact]
		public void when_scheduler_is_null_then_throws ()
		{
			Assert.Throws<ArgumentNullException> (() => AwaitExtensions.GetAwaiter (null));
		}
	}
}
