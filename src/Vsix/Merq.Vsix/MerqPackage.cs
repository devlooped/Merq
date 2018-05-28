using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

[assembly: ProvideCodeBase]
[assembly: ProvideCodeBase(AssemblyName = "Merq")]
[assembly: ProvideCodeBase(AssemblyName = "Merq.Core")]
[assembly: ProvideCodeBase(AssemblyName = "Merq.Async")]
[assembly: ProvideCodeBase(AssemblyName = "Merq.Async.Core")]

namespace Merq
{
	/// <summary>
	/// Package providing Merq registration.
	/// </summary>
	[Guid("49A95AF4-CB3D-4770-BD67-B0BBB46C6463")]
	[InstalledProductRegistration("#100", "#110", 
		ThisAssembly.Git.SemVer.Major + "." + 
		ThisAssembly.Git.SemVer.Minor + "." + 
		ThisAssembly.Git.SemVer.Patch + ThisAssembly.Git.SemVer.DashLabel + " (" + 
		ThisAssembly.Git.Branch + "@" +
		ThisAssembly.Git.Commit + ")")]
	[PackageRegistration(RegisterUsing = RegistrationMethod.CodeBase, UseManagedResourcesOnly = true)]
	[ProvideBindingPath]
	public class MerqPackage : Package
	{
	}
}