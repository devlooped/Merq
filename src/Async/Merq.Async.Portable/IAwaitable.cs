using System.ComponentModel;

namespace Merq
{
	/// <summary>
	/// An asynchronous job that can be awaited.
	/// </summary>
	public interface IAwaitable : IFluentInterface
	{
		/// <summary>
		/// Method invoked when awaiting this instance.
		/// </summary>
		[EditorBrowsable (EditorBrowsableState.Never)]
		IAwaiter GetAwaiter ();
	}

	/// <summary>
	/// An asynchronous job that can be awaited.
	/// </summary>
	/// <typeparam name="TResult">Type of result returned by the job.</typeparam>
	public interface IAwaitable<TResult> : IFluentInterface
	{
		/// <summary>
		/// Method invoked when awaiting this instance.
		/// </summary>
		[EditorBrowsable (EditorBrowsableState.Never)]
		IAwaiter<TResult> GetAwaiter ();
	}
}