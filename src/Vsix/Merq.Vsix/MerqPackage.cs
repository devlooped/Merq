using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

[assembly: ProvideCodeBase]
[assembly: ProvideCodeBase(AssemblyName = "Merq.Commands")]
[assembly: ProvideCodeBase(AssemblyName = "Merq.Events")]
[assembly: ProvideCodeBase(AssemblyName = "Merq.Events.EventStream")]
[assembly: ProvideCodeBase(AssemblyName = "Merq.Async")]
[assembly: ProvideCodeBase(AssemblyName = "Merq.Async.AsyncManager")]

namespace Merq
{
	/// <summary>
	/// Package providing Merq registration.
	/// </summary>
	[Guid("49A95AF4-CB3D-4770-BD67-B0BBB46C6463")]
	[PackageRegistration(RegisterUsing = RegistrationMethod.CodeBase, UseManagedResourcesOnly = true)]
	[ProvideBindingPath]
	public class MerqPackage : Package
	{
	}
}