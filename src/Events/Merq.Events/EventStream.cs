using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Subjects;
using System.Reflection;
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
		ConcurrentDictionary<TypeInfo, object> subjects = new ConcurrentDictionary<TypeInfo, object>();
		ConcurrentDictionary<TypeInfo, object[]> compatibleSubjects = new ConcurrentDictionary<TypeInfo, object[]>();

		/// <summary>
		/// Pushes an event to the stream, causing any  subscriber to be invoked if appropriate.
		/// </summary>
		public void Push<TEvent>(TEvent @event)
		{
			Guard.NotNull ("@event", @event);
			if (!IsValid<TEvent> ())
				throw new NotSupportedException (Strings.EventStream.PublishedEventNotPublic);

			var eventType = @event.GetType().GetTypeInfo();

			InvokeCompatible (@eventType, @event);
		}

		/// <summary>
		/// Observes the events of a given type <typeparamref name="TEvent"/>.
		/// </summary>
		public IObservable<TEvent> Of<TEvent>()
		{
			if (!IsValid<TEvent> ())
				throw new NotSupportedException (Strings.EventStream.SubscribedEventNotPublic);

			return (IObservable<TEvent>)subjects.GetOrAdd (typeof (TEvent).GetTypeInfo(), info => {
				// If we're creating a new subject, we need to clear the cache of compatible subjects
				compatibleSubjects.Clear ();
				return new Subject<TEvent> ();
			});
		}

		void InvokeCompatible (TypeInfo info, object @event)
		{
			// We will call all subjects that are compatible with
			// the event type, not just concrete event type subscribers.
			var compatible = compatibleSubjects.GetOrAdd(info, eventType => subjects.Keys
				.Where(subjectEventType => subjectEventType.IsAssignableFrom(eventType))
				.Select(subjectEventType => subjects[subjectEventType])
				.ToArray());

			foreach (dynamic subject in compatible) {
				subject.OnNext ((dynamic)@event);
			}
		}

		static bool IsValid<TEvent> () => typeof (TEvent).GetTypeInfo().IsPublic || typeof (TEvent).GetTypeInfo().IsNestedPublic;
	}
}
