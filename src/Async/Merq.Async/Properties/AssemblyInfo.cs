using System.Reflection;

#if DEBUG
[assembly: AssemblyConfiguration("DEBUG")]
#endif
#if RELEASE
[assembly: AssemblyConfiguration ("RELEASE")]
#endif

[assembly: AssemblyTitle(ThisAssembly.Project.AssemblyName)]
[assembly: AssemblyDescription(ThisAssembly.Project.AssemblyName)]
[assembly: AssemblyCompany("Mobile Essentials")]
[assembly: AssemblyProduct("Merq")]
[assembly: AssemblyCopyright("Copyright © Mobile Essentials 2016")]

[assembly: AssemblyVersion("1.0.0")]
[assembly: AssemblyFileVersion(ThisAssembly.Git.SemVer.Major + "." + ThisAssembly.Git.SemVer.Minor + "." + ThisAssembly.Git.SemVer.Patch)]
[assembly: AssemblyInformationalVersion(ThisAssembly.Git.SemVer.Major + "." + ThisAssembly.Git.SemVer.Minor + "." + ThisAssembly.Git.SemVer.Patch + "-" + ThisAssembly.Git.Branch + "+" + ThisAssembly.Git.Commit)]
