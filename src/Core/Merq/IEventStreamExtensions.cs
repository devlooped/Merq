using System.ComponentModel;

namespace Merq
{
    /// <summary>
    /// Usability overloads for <see cref="IEventStream"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class IEventStreamExtensions
    {
        /// <summary>
        /// Pushes a new instance of <typeparamref name="TEvent"/> through the stream.
        /// </summary>
        public static void Push<TEvent>(this IEventStream events) where TEvent : new()
            => events.Push(new TEvent());
    }
}
