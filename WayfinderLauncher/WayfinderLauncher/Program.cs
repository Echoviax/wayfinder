using System.Diagnostics;

namespace WayfinderLauncher
{
    class Program
    {
        static void Main()
        {
            string currentDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)!;
            string hookPath = Path.Combine(currentDir, "RuntimePatcher.dll");

            bool isWindows = OperatingSystem.IsWindows();
            string targetExe = isWindows ? "Neverway.exe" : "Neverway";
            string gamePath = Path.Combine(currentDir, targetExe);

            if (!File.Exists(gamePath))
            {
                Console.WriteLine($"Could not find {targetExe} in {currentDir}");
                Console.ReadLine();
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = gamePath,
                UseShellExecute = false,
            };

            psi.EnvironmentVariables["DOTNET_STARTUP_HOOKS"] = hookPath;

            try
            {
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