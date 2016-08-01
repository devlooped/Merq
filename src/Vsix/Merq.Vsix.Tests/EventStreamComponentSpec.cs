using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Moq;
using Xunit;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Reactive.Linq;

namespace Merq
{
	public class EventStreamComponentSpec
	{
		[Fact]
		public async Task when_subscribing_to_externally_produced_event_then_fetches_observable_from_components()
		{
			var expected = new FooEvent();
			var observable = new[] { expected }.ToObservable();
			var stream = new EventStreamComponent(Mock.Of<IServiceProvider>(s =>
				s.GetService(typeof(SComponentModel)) == Mock.Of<IComponentModel>(c =>
					c.GetExtensions<IObservable<FooEvent>>() == new[] { observable })));

			var actual = await stream.Of<FooEvent>().FirstAsync();

			Assert.Same(expected, actual);
		}

		public class FooEvent { }
	}
}
