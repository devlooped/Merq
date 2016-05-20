using System;
using Xunit;

namespace Merq
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
