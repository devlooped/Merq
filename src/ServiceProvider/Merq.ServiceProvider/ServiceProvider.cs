using System;
using System.Diagnostics;
using Merq.Properties;

namespace Merq
{
	/// <summary>
	/// Provides a global entry point for retrieving services from the
	/// hosting environment.
	/// </summary>
	public static class ServiceProvider
	{
		static readonly ITracer tracer = Tracer.Get (typeof (ServiceProvider));

		/// <summary>
		/// The service provider instance to use in the current application.
		/// </summary>
		public static IServiceProvider Instance { get; set; }

		/// <summary>
		/// Gets type-based services from the configured service provider <see cref="Instance"/>.
		/// </summary>
		/// <typeparam name="T">The type of the service to get.</typeparam>
		/// <returns>The requested service, or a <see langword="null"/> reference if the service could not be located.</returns>
		/// <exception cref="InvalidOperationException">The <see cref="Instance"/> service provider has not been initialized.</exception>
		public static T TryGetService<T>()
		{
			ThrowIfNullInstance ();

			return Instance.TryGetService<T> ();
		}

		/// <summary>
		/// Gets type-based services from the configured service provider <see cref="Instance"/>.
		/// </summary>
		/// <typeparam name="T">The type of the service to get.</typeparam>
		/// <exception cref="MissingDependencyException">The requested service was not found</exception>
		/// <exception cref="InvalidOperationException">The <see cref="Instance"/> service provider has not been initialized.</exception>
		/// <returns>The requested service, or throws an <see cref="MissingDependencyException"/>
		/// if the service was not found.</returns>
		public static T GetService<T>()
		{
			ThrowIfNullInstance ();

			return Instance.GetService<T> ();
		}

		static void ThrowIfNullInstance ()
		{
			if (Instance == null) {
				tracer.Error (Strings.ServiceProvider.NotInitialized);
				throw new InvalidOperationException (Strings.ServiceProvider.NotInitialized);
			}
		}
	}
}