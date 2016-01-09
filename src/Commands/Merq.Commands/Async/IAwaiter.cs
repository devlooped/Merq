using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Merq
{
	/// <summary>
	/// An awaiter returned from <see cref="IAwaitable.GetAwaiter" />.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public interface IAwaiter : INotifyCompletion
	{
		/// <summary>
		/// Ends the wait for the completion of the asynchronous task.
		/// </summary>
		[EditorBrowsable (EditorBrowsableState.Never)]
		void GetResult ();

		/// <summary>Gets a value that indicates whether the asynchronous task has completed.</summary>
		[EditorBrowsable (EditorBrowsableState.Never)]
		bool IsCompleted { get; }
	}

	/// <summary>
	/// An awaiter returned from <see cref="IAwaitable{TResult}.GetAwaiter" />.
	/// </summary>
	/// <typeparam name="TResult">Type of result returned by the awaited job.</typeparam>
	[EditorBrowsable (EditorBrowsableState.Never)]
	public interface IAwaiter<TResult> : INotifyCompletion
	{
		/// <summary>
		/// Ends the wait for the completion of the asynchronous task.
		/// </summary>
		[EditorBrowsable (EditorBrowsableState.Never)]
		TResult GetResult ();

		/// <summary>Gets a value that indicates whether the asynchronous task has completed.</summary>
		[EditorBrowsable (EditorBrowsableState.Never)]
		bool IsCompleted { get; }
	}
}