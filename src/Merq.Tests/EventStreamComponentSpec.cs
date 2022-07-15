using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Moq;
using Xunit;

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
