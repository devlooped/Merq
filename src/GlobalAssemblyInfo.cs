using System.Reflection;
using System.Runtime.CompilerServices;

#if DEBUG
[assembly: AssemblyConfiguration ("DEBUG")]
#endif
#if RELEASE
[assembly: AssemblyConfiguration ("RELEASE")]
#endif

[assembly: AssemblyCompany ("MobileEssentials")]
[assembly: AssemblyProduct ("Merq")]
[assembly: AssemblyCopyright ("Copyright © MobileEssentials 2016")]

#pragma warning disable 0436
[assembly: AssemblyVersion (ThisAssembly.Git.SemVer.Major + "." + ThisAssembly.Git.SemVer.Minor + "." + ThisAssembly.Git.SemVer.Patch)]
[assembly: AssemblyFileVersion (ThisAssembly.Git.SemVer.Major + "." + ThisAssembly.Git.SemVer.Minor + "." + ThisAssembly.Git.SemVer.Patch)]
[assembly: AssemblyInformationalVersion (ThisAssembly.Git.SemVer.Major + "." + ThisAssembly.Git.SemVer.Minor + "." + ThisAssembly.Git.SemVer.Patch + "-" + ThisAssembly.Git.Branch + "+" + ThisAssembly.Git.Commit)]
#pragma warning restore 0436