using System.Reflection;

#if DEBUG
[assembly: AssemblyConfiguration("DEBUG")]
#endif
#if RELEASE
[assembly: AssemblyConfiguration ("RELEASE")]
#endif

#pragma warning disable 0436
[assembly: AssemblyTitle(ThisAssembly.Project.AssemblyName)]
[assembly: AssemblyDescription(ThisAssembly.Project.AssemblyName)]
[assembly: AssemblyCompany("Mobile Essentials")]
[assembly: AssemblyProduct("Merq")]
[assembly: AssemblyCopyright("Copyright © Mobile Essentials 2016")]

[assembly: AssemblyVersion(ThisAssembly.Git.BaseVersion.Major + "." + ThisAssembly.Git.BaseVersion.Minor + ".0")]
[assembly: AssemblyFileVersion(ThisAssembly.Git.SemVer.Major + "." + ThisAssembly.Git.SemVer.Minor + "." + ThisAssembly.Git.SemVer.Patch)]
[assembly: AssemblyInformationalVersion(ThisAssembly.Git.SemVer.Major + "." + ThisAssembly.Git.SemVer.Minor + "." + ThisAssembly.Git.SemVer.Patch + "-" + ThisAssembly.Git.Branch + "+" + ThisAssembly.Git.Commit)]
#pragma warning restore 0436