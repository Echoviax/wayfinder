using System.Reflection;

internal class StartupHook
{
    public static void Initialize()
    {
        string hookFilePath = Assembly.GetExecutingAssembly().Location;
        if (string.IsNullOrEmpty(hookFilePath))
        {
            hookFilePath = AppContext.BaseDirectory;
        }

        string hookFolder = Path.GetDirectoryName(hookFilePath)!;
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            string assemblyName = new AssemblyName(args.Name).Name!;
            string potentialPath = Path.Combine(hookFolder, assemblyName + ".dll");

            if (File.Exists(potentialPath))
            {
                return Assembly.LoadFrom(potentialPath);
            }
            return null;
        };

        BootModLoader(hookFolder);
    }

    private static void BootModLoader(string hookFolder)
    {
        try
        {
            string loaderPath = Path.Combine(hookFolder, "Wayfinder.Core.dll");

            if (!File.Exists(loaderPath))
            {
                File.WriteAllText(Path.Combine(hookFolder, "Patcher_Error.txt"), $"Missing: {loaderPath}");
                return;
            }

            Assembly loaderAssembly = Assembly.LoadFrom(loaderPath);
            var type = loaderAssembly.GetType("Wayfinder.Core.LoaderCore");
            var method = type?.GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public);

            method?.Invoke(null, null);
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(hookFolder, "Patcher_CrashLog.txt"), ex.ToString());
        }
    }
}