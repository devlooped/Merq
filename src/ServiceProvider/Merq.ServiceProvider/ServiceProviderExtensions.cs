using System;
using System.ComponentModel;
using System.Diagnostics;
using Merq;
using Merq.Properties;

namespace Merq
{
	/// <summary>
	/// Defines extension methods related to <see cref="IServiceProvider"/>.
	/// </summary>
	[EditorBrowsable (EditorBrowsableState.Never)]
	public static class ServiceProviderExtensions
	{
		/// <summary>
		/// Gets type-based services from the service provider.
		/// </summary>
		/// <typeparam name="T">The type of the service to get.</typeparam>
		/// <param name="provider" this="true">The service provider.</param>
		/// <returns>The requested service, or a <see langword="null"/> reference if the service could not be located.</returns>
		public static T TryGetService<T>(this IServiceProvider provider)
		{
			Guard.NotNull ("provider", provider);

			return (T)provider.GetService (typeof (T));
		}

		/// <summary>
		/// Gets type-based services from the service provider.
		/// </summary>
		/// <typeparam name="T">The type of the service to get.</typeparam>
		/// <param name="provider" this="true">The service provider.</param>
		/// <exception cref="MissingDependencyException">The requested service was not found.</exception>
		/// <returns>The requested service, or throws an <see cref="MissingDependencyException"/>
		/// if the service was not found.</returns>
		public static T GetService<T>(this IServiceProvider provider)
		{
			Guard.NotNull ("provider", provider);

			var service = (T)provider.GetService(typeof(T));
			if (service == null)
				throw new MissingDependencyException (Strings.ServiceProvider.MissingDependency (typeof (T)));

			return service;
		}
	}
}