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
		[ImportingConstructor]
		public AsyncManagerProvider (JoinableTaskContext context)
			=> AsyncManager = new AsyncManager(context);

		/// <summary>
		/// Exports the <see cref="IAsyncManager"/>.
		/// </summary>
		[Export]
		public IAsyncManager AsyncManager { get; }
	}
}