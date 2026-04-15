using System.Diagnostics;

namespace Wayfinder.Launcher
{
    class Program
    {
        static void Main()
        {
            string currentDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)!;

            string wayfinderDir = Path.Combine(currentDir, "Wayfinder");
            string hookPath = Path.Combine(wayfinderDir, "Wayfinder.Patcher.dll");

            bool isWindows = OperatingSystem.IsWindows();
            string targetExe = isWindows ? "Neverway.exe" : "Neverway";
            string gamePath = Path.Combine(currentDir, targetExe);

            if (!File.Exists(gamePath))
            {
                Console.WriteLine($"[Error] Could not find {targetExe} in {currentDir}");
                Console.ReadLine();
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = gamePath,
                UseShellExecute = false,
                WorkingDirectory = currentDir
            };

            psi.EnvironmentVariables["DOTNET_STARTUP_HOOKS"] = hookPath;
            psi.EnvironmentVariables["DOTNET_ROOT"] = wayfinderDir;

            if (!isWindows)
                psi.EnvironmentVariables["LD_LIBRARY_PATH"] = wayfinderDir;

            try
            {
                Console.WriteLine($"Launching {targetExe} with Wayfinder Hook...");
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to launch game: " + ex.Message);
                Console.ReadLine();
            }
        }
    }
}