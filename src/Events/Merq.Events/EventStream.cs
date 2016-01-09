using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Subjects;
using Merq.Properties;

namespace Merq
{
	/// <summary>
	/// Provides the implementation for a reactive extensions event stream,
	/// allowing trending and analysis queries to be performed in real-time
	/// over the events pushed through the stream, as well as loosly coupled
	/// communication across components.
	/// </summary>
	public class EventStream : IEventStream
	{
		ConcurrentDictionary<Type, object> subjects = new ConcurrentDictionary<Type, object>();
		ConcurrentDictionary<Type, object[]> compatibleSubjects = new ConcurrentDictionary<Type, object[]>();

		/// <summary>
		/// Pushes an event to the stream, causing any  subscriber to be invoked if appropriate.
		/// </summary>
		public void Push<TEvent>(TEvent @event)
		{
			Guard.NotNull ("@event", @event);
			if (!IsValid<TEvent> ())
				throw new NotSupportedException (Strings.EventStream.PublishedEventNotPublic);

			var eventType = @event.GetType();

			InvokeCompatible (@eventType, @event);
		}

		/// <summary>
		/// Observes the events of a given type <typeparamref name="TEvent"/>.
		/// </summary>
		public IObservable<TEvent> Of<TEvent>()
		{
			if (!IsValid<TEvent> ())
				throw new NotSupportedException (Strings.EventStream.SubscribedEventNotPublic);

			return (IObservable<TEvent>)subjects.GetOrAdd (typeof (TEvent), type => {
				// If we're creating a new subject, we need to clear the cache of compatible subjects
				compatibleSubjects.Clear ();
				return new Subject<TEvent> ();
			});
		}

		void InvokeCompatible (Type type, object @event)
		{
			// We will call all subjects that are compatible with
			// the event type, not just concrete event type subscribers.
			var compatible = compatibleSubjects.GetOrAdd(type, eventType => subjects.Keys
				.Where(subjectEventType => subjectEventType.IsAssignableFrom(eventType))
				.Select(subjectEventType => subjects[subjectEventType])
				.ToArray());

			foreach (dynamic subject in compatible) {
				subject.OnNext ((dynamic)@event);
			}
		}

		static bool IsValid<TEvent> () => typeof (TEvent).IsPublic || typeof (TEvent).IsNestedPublic;
	}
}
