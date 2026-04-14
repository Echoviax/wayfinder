using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

internal class StartupHook
{
    public static void Initialize()
    {
        string gameFolder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)!;

        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            string assemblyName = new AssemblyName(args.Name).Name!;
            string potentialPath = Path.Combine(gameFolder, assemblyName + ".dll");

            if (File.Exists(potentialPath))
            {
                return Assembly.LoadFrom(potentialPath);
            }
            return null;
        };

        BootModLoader();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void BootModLoader()
    {
        try
        {
            string gameFolder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)!;
            string loaderPath = Path.Combine(gameFolder, "NeverwayModLoader.dll");

            Assembly loaderAssembly = Assembly.LoadFrom(loaderPath);
            loaderAssembly.GetType("NeverwayModLoader.LoaderCore")!
                          .GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public)!
                          .Invoke(null, null);
        }
        catch (Exception ex)
        {
            string gameFolder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)!;
            File.WriteAllText(Path.Combine(gameFolder, "Patcher_CrashLog.txt"), "Bootstrapper failed: " + ex.Message);
        }
    }
}