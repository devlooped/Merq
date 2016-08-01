using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Merq.Properties;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Merq
{
	/// <summary>
	/// We switch the implementation of the async manager depending 
	/// on the Visual Studio version. 
	/// </summary>
	[PartCreationPolicy (CreationPolicy.Shared)]
	internal class AsyncManagerProvider
	{
		IServiceProvider serviceProvider;
		Lazy<IAsyncManager> manager;

		/// <summary>
		/// Initializes the provider with the given VS services.
		/// </summary>
		/// <param name="serviceProvider"></param>
		[ImportingConstructor]
		public AsyncManagerProvider ([Import (typeof (SVsServiceProvider))] IServiceProvider serviceProvider)
		{
			this.serviceProvider = serviceProvider;
			manager = new Lazy<IAsyncManager> (() => CreateAsyncManager ());
		}

		/// <summary>
		/// Exports the <see cref="IAsyncManager"/>.
		/// </summary>
		[Export]
		public IAsyncManager AsyncManager => manager.Value;

		IAsyncManager CreateAsyncManager ()
		{
			// NOTE: under VS2012, an installer should install the Microsoft.VisualStudio.Threading.Downlevel.vsix 
			// from the Microsoft.VisualStudio.Threading.DownlevelInstaller nuget package via an MSI.
			// VS2012 case
			var context = (JoinableTaskContext)serviceProvider.GetService(typeof(SVsJoinableTaskContext));
			if (context != null)
				return new AsyncManager (context);

			// VS2013+ case
			var schedulerService = (IVsTaskSchedulerService2)serviceProvider.GetService(typeof(SVsTaskSchedulerService));
			if (schedulerService != null)
				return new AsyncManager ((JoinableTaskContext)schedulerService.GetAsyncTaskContext ());

			throw new NotSupportedException(Strings.AsyncManagerProvider.NoTaskContext);
		}
	}
}

namespace Microsoft.VisualStudio.Threading
{
	/// <summary>
	/// The legacy VS2012 type serving as the service guid for acquiring an instance of JoinableTaskContext.
	/// </summary>
	[Guid("8767A7D4-ECFC-4627-8FC0-A1685E3B0493")]
	public interface SVsJoinableTaskContext
	{
	}
}

namespace Microsoft.VisualStudio.Shell.Interop
{
	/// <summary>
	/// Internal.
	/// </summary>
	[ComImport, Guid("8176DC77-36E2-4987-955B-9F63C6F3F229"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), TypeIdentifier]
	public interface IVsTaskSchedulerService2
	{
		/// <summary>
		/// Internal.
		/// </summary>
		[return: MarshalAs(UnmanagedType.IUnknown)]
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		object GetAsyncTaskContext();
	}
}