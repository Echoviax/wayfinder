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

        static void FixRuntimeConfig(string configPath)
        {
            if (!File.Exists(configPath)) return;

            string fixedJson = @"{
  ""runtimeOptions"": {
    ""tfm"": ""net8.0"",
    ""framework"": {
      ""name"": ""Microsoft.NETCore.App"",
      ""version"": ""8.0.0""
    }
  }
}";
            File.WriteAllText(configPath, fixedJson);
        }

        static void Main()
        {
            bool isWindows = OperatingSystem.IsWindows();

            string currentDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)!;
            string wayfinderDir = Path.Combine(currentDir, "Wayfinder");
            string hookPath = Path.Combine(wayfinderDir, "Wayfinder.Patcher.dll");

            string moddedDir = Path.Combine(currentDir, "ModdedCoreFiles");
            string gameName = "Neverway";

            string targetDll = Path.Combine(moddedDir, $"{gameName}.dll");
            string runtimeConfig = Path.Combine(moddedDir, $"{gameName}.runtimeconfig.json");
            string depsFile = Path.Combine(moddedDir, $"{gameName}.deps.json");

            if (!Directory.Exists(moddedDir) || !File.Exists(targetDll))
            {
                Console.WriteLine("[Wayfinder] Extracted files not found. Unpacking assemblies now (only happens on first boot)...");

                string gameExeName = isWindows ? $"{gameName}.exe" : gameName;
                string gameExePath = Path.Combine(currentDir, gameExeName);

                if (!File.Exists(gameExePath))
                {
                    Console.WriteLine($"[Error] Could not find {gameExeName} to extract from.");
                    Console.ReadLine();
                    return;
                }

                try
                {
                    Decompiler.Extractor.ExtractBundle(gameExePath, moddedDir);
                    FixRuntimeConfig(runtimeConfig);

                    var files = Directory.EnumerateFiles(currentDir, "*.*", SearchOption.AllDirectories)
                        .Where(file =>
                            !file.StartsWith(moddedDir, StringComparison.OrdinalIgnoreCase) &&
                            (file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                             file.EndsWith(".so", StringComparison.OrdinalIgnoreCase) ||
                             file.EndsWith(".dylib", StringComparison.OrdinalIgnoreCase)))
                        .Select(file => new
                        {
                            Source = file,
                            Destination = Path.Combine(moddedDir, Path.GetFileName(file))
                        });

                    foreach (var file in files)
                        File.Copy(file.Source, file.Destination, true);

                    string resourcesDir = Path.Combine(currentDir, "resources");
                    string targetResourcesDir = Path.Combine(moddedDir, "resources");

                    if (Directory.Exists(resourcesDir))
                    {
                        var resourceFiles = Directory.EnumerateFiles(resourcesDir, "*.*", SearchOption.AllDirectories);
                        foreach (var file in resourceFiles)
                        {
                            string relativePath = file.Substring(resourcesDir.Length + 1);
                            string destinationPath = Path.Combine(targetResourcesDir, relativePath);

                            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                            File.Copy(file, destinationPath, true);
                        }
                    }

                    string runtimesDir = Path.Combine(currentDir, "runtimes");
                    string targetRuntimesDir = Path.Combine(moddedDir, "runtimes");

                    if (Directory.Exists(runtimesDir))
                    {
                        var runtimeFiles = Directory.EnumerateFiles(runtimesDir, "*.*", SearchOption.AllDirectories);
                        foreach (var file in runtimeFiles)
                        {
                            string relativePath = file.Substring(runtimesDir.Length + 1);
                            string destinationPath = Path.Combine(targetRuntimesDir, relativePath);

                            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                            File.Copy(file, destinationPath, true);
                        }
                    }

                    Console.WriteLine("[Wayfinder] Extraction complete!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] Failed to extract: {ex.Message}");
                    Console.ReadLine();
                    return;
                }
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