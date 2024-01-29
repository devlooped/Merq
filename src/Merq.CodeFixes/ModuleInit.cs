using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Merq;

#if DEBUG
static class ModuleInit
{
    /// <summary>
    /// Ensures we can load Superpower from the same directory as the code fix 
    /// assembly, for cases where the code fix is referenced via a project reference, 
    /// which we'll only support in debug builds.
    /// </summary>
    [ModuleInitializer]
    public static void Init()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            var name = new AssemblyName(e.Name);
            if (name.Name == "Superpower" &&
                Path.GetDirectoryName(typeof(ModuleInit).Assembly.ManifestModule.FullyQualifiedName) is string dir &&
                Path.Combine(dir, "Superpower.dll") is string path &&
                File.Exists(path) &&
                Assembly.LoadFrom(path) is Assembly asm)
            {
                return asm;
            }

            return null;
        };
    }
}
#endif