using System;
using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;

namespace Merq;

public record MessageBusServiceSpec(ITestOutputHelper Output)
{
    IMessageBus bus = new ServiceCollection()
        .AddMessageBus()
        .BuildServiceProvider()
        .GetRequiredService<IMessageBus>();

    [Fact]
    public void when_subscribing_external_producer_then_succeeds()
    {
        var producer = new Subject<int>();
        var collection = new ServiceCollection()
            .AddMessageBus()
            .AddSingleton<IObservable<int>>(producer);

        var services = collection.BuildServiceProvider();
        var bus = services.GetRequiredService<IMessageBus>();

        int? value = default;

        bus.Observe<int>().Subscribe(i => value = i);

        producer.OnNext(42);

        Assert.Equal(42, value);
    }

    [Fact]
    public void when_notifying_external_producer_then_throws()
    {
        var producer = new Subject<int>();
        var collection = new ServiceCollection()
            .AddMessageBus()
            .AddSingleton<IObservable<int>>(producer);

        var services = collection.BuildServiceProvider();
        var bus = services.GetRequiredService<IMessageBus>();

        Assert.Throws<NotSupportedException>(() => bus.Notify(42));
    }

    [Fact]
    public void when_subscribing_subject_then_succeeds()
    {
        int? value = default;
        bus.Observe<int>().Subscribe(i => value = i);

        bus.Notify(42);

        Assert.Equal(42, value);
    }

    [Fact]
    public void when_notifying_null_event_then_throws()
        => Assert.Throws<ArgumentNullException>(() => bus.Notify<object?>(null));

    [Fact]
    public void when_notifying_non_public_event_type_then_calls_subscriber()
    {
        var called = false;

        bus.Observe<NonPublicEvent>().Subscribe(x => called = true);

        bus.Notify(new NonPublicEvent());

        Assert.True(called);
    }

    [Fact]
    public void when_notifying_nested_non_public_event_type_then_calls_subscriber()
    {
        var called = false;

        bus.Observe<NestedEvent>().Subscribe(x => called = true);

        bus.Notify(new NestedEvent());

        Assert.True(called);
    }

    [Fact]
    public void when_notifying_non_subscribed_event_then_does_not_call_subscriber()
    {
        var called = false;

        using var subs = bus.Observe<ConcreteEvent>().Subscribe(c => called = true);

        bus.Notify(new AnotherEvent());

        Assert.False(called);
    }

    [Fact]
    public void when_notifying_subscribed_event_using_base_type_then_calls_derived_subscriber()
    {
        var called = false;
        using var subscription = bus.Observe<ConcreteEvent>().Subscribe(c => called = true);

        BaseEvent e = new ConcreteEvent();
        bus.Notify(e);

        Assert.True(called);
    }

    [Fact]
    public void when_subscribing_as_event_interface_then_calls_subscriber()
    {
        var called = false;
        using var subs = bus.Observe<IBaseEvent>().Subscribe(c => called = true);

        bus.Notify(new ConcreteEvent());

        Assert.True(called);
    }


    [Fact]
    public void given_two_observables_when_subscribing_base_event_then_receives_both()
    {
        var subject1 = new Subject<ConcreteEvent>();
        var subject2 = new Subject<AnotherEvent>();
        var bus = new ServiceCollection()
            .AddMessageBus()
            .AddSingleton<IObservable<ConcreteEvent>>(subject1)
            // NOTE: the producer needs to properly register as the base types too
            .AddSingleton<IObservable<BaseEvent>>(subject1)
            .AddSingleton(subject2)
            .AddSingleton<IObservable<AnotherEvent>>(subject2)
            .AddSingleton<IObservable<BaseEvent>>(subject2)
            .BuildServiceProvider()
            .GetRequiredService<IMessageBus>();

        var called = 0;

        using var subs = bus.Observe<BaseEvent>().Subscribe(c => called++);

        subject1.OnNext(new ConcreteEvent());
        subject2.OnNext(new AnotherEvent());

        Assert.Equal(2, called);
    }

    class NestedEvent { }
}