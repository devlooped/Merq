using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Xunit;

namespace Merq
{
    public class EventStreamSpec
    {
        [Fact]
        public void when_pushing_null_event_then_throws()
        {
            var stream = new EventStream();

            Assert.Throws<ArgumentNullException>(() => stream.Push<object>(null));
        }

        [Fact]
        public void when_pushing_non_public_event_type_then_throws()
        {
            var stream = new EventStream();

            Assert.Throws<NotSupportedException>(() => stream.Push(new NonPublicEvent()));
        }

        [Fact]
        public void when_subscribing_non_public_event_type_then_throws()
        {
            var stream = new EventStream();

            Assert.Throws<NotSupportedException>(() => stream.Of<NonPublicEvent>());
        }

        [Fact]
        public void when_pushing_non_subscribed_event_then_does_not_call_subscriber()
        {
            var stream = new EventStream();
            var called = false;

            using (var subscription = stream.Of<ConcreteEvent>().Subscribe(c => called = true))
            {
                stream.Push(new AnotherEvent());
            }

            Assert.False(called);
        }

        [Fact]
        public void when_pushing_subscribed_nested_public_event_then_calls_subscriber()
        {
            var stream = new EventStream();
            var called = false;

            using (var subscription = stream.Of<NestedPublicEvent>().Subscribe(c => called = true))
            {
                stream.Push(new NestedPublicEvent());
            }

            Assert.True(called);
        }

        [Fact]
        public void when_pushing_subscribed_event_then_calls_subscriber()
        {
            var stream = new EventStream();
            var called = false;

            using (var subscription = stream.Of<ConcreteEvent>().Subscribe(c => called = true))
            {
                stream.Push(new ConcreteEvent());
            }

            Assert.True(called);
        }

        [Fact]
        public void when_pushing_subscribed_event_using_base_type_then_calls_subscriber()
        {
            var stream = new EventStream();
            var called = false;

            using (var subscription = stream.Of<ConcreteEvent>().Subscribe(c => called = true))
            {
                BaseEvent @event = new ConcreteEvent();
                stream.Push(@event);
            }

            Assert.True(called);
        }

        [Fact]
        public void when_subscribing_as_event_interface_then_calls_subscriber()
        {
            var stream = new EventStream();
            var called = false;

            using (var subscription = stream.Of<IBaseEvent>().Subscribe(c => called = true))
            {
                stream.Push(new ConcreteEvent());
            }

            Assert.True(called);
        }

        [Fact]
        public void given_an_observable_when_subscribing_event_then_subscribes_to_observable()
        {
            var subject = new Subject<ConcreteEvent>();
            var stream = new EventStream(subject);
            var called = false;

            using (var subscription = stream.Of<ConcreteEvent>().Subscribe(c => called = true))
            {
                subject.OnNext(new ConcreteEvent());
            }

            Assert.True(called);
        }

        [Fact]
        public void given_two_observables_when_subscribing_base_event_then_receives_both()
        {
            var subject1 = new Subject<ConcreteEvent>();
            var subject2 = new Subject<AnotherEvent>();
            var stream = new EventStream(subject1, subject2);
            var called = 0;

            using (var subscription = stream.Of<BaseEvent>().Subscribe(c => called++))
            {
                subject1.OnNext(new ConcreteEvent());
                subject2.OnNext(new AnotherEvent());
            }

            Assert.Equal(2, called);
        }

        [Fact]
        public void given_an_observable_when_pushing_compatible_event_then_subscriber_base_type_receives_both()
        {
            var subject = new Subject<ConcreteEvent>();
            var stream = new EventStream(subject);
            var called = 0;

            using (var subscription = stream.Of<BaseEvent>().Subscribe(c => called++))
            {
                subject.OnNext(new ConcreteEvent());
                stream.Push(new AnotherEvent());
            }

            Assert.Equal(2, called);
        }


        public class NestedPublicEvent { }
    }

    public interface IBaseEvent { }

    public class BaseEvent : IBaseEvent
    {
    }

    public class ConcreteEvent : BaseEvent { }

    public class AnotherEvent : BaseEvent { }

    class NonPublicEvent { }
}