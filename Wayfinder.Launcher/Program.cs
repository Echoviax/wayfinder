using System.Diagnostics;

namespace Wayfinder.Launcher
{
    class Program
    {
        static string GetDotnetCommand()
        {
            if (OperatingSystem.IsWindows())
                return "dotnet.exe";

            string pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var dir in pathVar.Split(':'))
            {
                string fullPath = Path.Combine(dir, "dotnet");
                if (File.Exists(fullPath))
                    return fullPath;
            }

            if (File.Exists("/usr/share/dotnet/dotnet")) 
                return "/usr/share/dotnet/dotnet";
            if (File.Exists("/usr/local/share/dotnet/dotnet")) 
                return "/usr/local/share/dotnet/dotnet";

            return "dotnet";
        }

        static void Main()
        {
            bool isWindows = OperatingSystem.IsWindows();

            string currentDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)!;
            string wayfinderDir = Path.Combine(currentDir, "Wayfinder");
            string hookPath = Path.Combine(wayfinderDir, "Wayfinder.Patcher.dll");

            string moddedDir = Path.Combine(currentDir, "extracted");
            string gameName = "Neverway";

            string targetDll = Path.Combine(moddedDir, $"{gameName}.dll");
            string runtimeConfig = Path.Combine(moddedDir, $"{gameName}.runtimeconfig.json");
            string depsFile = Path.Combine(moddedDir, $"{gameName}.deps.json");

            if (!Directory.Exists(moddedDir) || !File.Exists(targetDll))
            {
                Console.WriteLine($"[Error] Couldn't find Neverway.dll or couldn't find a folder titled `extracted`");
                Console.WriteLine("Please manually unpack the game assemblies before running Wayfinder.");
                Console.ReadLine();
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = GetDotnetCommand(),
                Arguments = $"exec --runtimeconfig \"{runtimeConfig}\" --depsfile \"{depsFile}\" --additionalProbingPath \"{moddedDir}\" \"{targetDll}\"",
                UseShellExecute = false,
                WorkingDirectory = currentDir
            };

            psi.EnvironmentVariables["DOTNET_STARTUP_HOOKS"] = hookPath;
            psi.EnvironmentVariables["DOTNET_ROOT"] = wayfinderDir;

            if (!isWindows)
                psi.EnvironmentVariables["LD_LIBRARY_PATH"] = wayfinderDir;

            try
            {
                Console.WriteLine($"Launching {gameName} through a sandbox with Wayfinder Hooked in...");
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to launch game: " + ex.Message);
                Console.WriteLine("Ensure the .NET 8 SDK is installed and accessible.");
                Console.ReadLine();
            }
        }
    }
}