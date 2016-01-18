using System;

namespace Merq
{
	/// <summary>
	/// Provides an observable stream of events that can be used for analysis 
	/// or real-time handling.
	/// </summary>
	/// <remarks>
	/// Leveraging the Reactive Extensions (Rx), it's possible to build fairly 
	/// complicated event reaction chains by simply issuing Linq-style queries 
	/// over the event stream. 
	/// </remarks>
	public interface IEventStream : IFluentInterface
	{
		/// <summary>
		/// Pushes an event to the stream, causing any subscriber to be invoked if
		/// appropriate.
		/// </summary>
		void Push<TEvent>(TEvent args);

		/// <summary>
		/// Observes the events of a given type <typeparamref name="TEvent"/>.
		/// </summary>
		IObservable<TEvent> Of<TEvent>();
	}
}