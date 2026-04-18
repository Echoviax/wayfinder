using HarmonyLib;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Wayfinder.API;

namespace Wayfinder.Core
{
    public class ModInstance
    {
        public API.IWayfinderMod Mod { get; }
        public bool IsEnabled { get; set; }

        public ModInstance(API.IWayfinderMod mod, bool isEnabled)
        {
            Mod = mod;
            IsEnabled = isEnabled;
        }
    }

    public static class LoaderCore
    {
        public static List<ModInstance> LoadedMods { get; } = new List<ModInstance>();
        private static string logFilePath = "";

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        public static void Initialize()
        {
            try
            {
                AllocConsole();
                var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
                Console.SetOut(writer);
                Console.Title = "Wayfinder - Debug Terminal";
            }
            catch { }

            string coreAssemblyPath = Assembly.GetExecutingAssembly().Location;
            string wayfinderFolder = Path.GetDirectoryName(coreAssemblyPath)!;
            string exePath = Path.GetFullPath(Path.Combine(wayfinderFolder, ".."));
            string modsDirectory = Path.Combine(exePath, "Mods");

            logFilePath = Path.Combine(exePath, "Wayfinder_Log.txt");
            File.WriteAllText(logFilePath, $"--- Wayfinder Started at {DateTime.Now} ---\n");

            LogInfo("Initializing Wayfinder...");

            ApplyCorePatches();

            if (!Directory.Exists(modsDirectory))
            {
                Directory.CreateDirectory(modsDirectory);
                LogWarning("Mods directory not found. Created 'Mods' folder.");
                return;
            }

            string[] modFiles = Directory.GetFiles(modsDirectory, "*.dll");
            LogInfo($"Found {modFiles.Length} potential mod(s) in directory.");

            foreach (string modFile in modFiles)
            {
                try
                {
                    Assembly modAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(modFile);
                    StartMod(modAssembly);
                }
                catch (Exception ex)
                {
                    LogError($"Failed to load {Path.GetFileName(modFile)}: {ex.Message}");
                }
            }
        }

        private static void ApplyCorePatches()
        {
            try
            {
                var harmony = new Harmony("com.echoviax.wayfinder.core");

                harmony.PatchAll();
                LogSuccess("Core patches applied successfully!");
            }
            catch (Exception ex)
            {
                LogError("Failed to inject Wayfinder core patches: " + ex);
            }
        }

        private static void StartMod(Assembly modAssembly)
        {
            var modTypes = modAssembly.GetTypes().Where(t => typeof(IWayfinderMod).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract).ToList();
            string modName = modAssembly.GetName().Name ?? "Unknown Mod";
            Type? entryType = modAssembly.GetType("ModEntry");

            if (modTypes.Count == 0)
            {
                LogWarning($"Skipping {modAssembly.GetName().Name}, no IWayfinderMod implementation found.");
                return;
            }

            foreach (var type in modTypes)
            {
                try
                {
                    // Create mod instance
                    if (Activator.CreateInstance(type) is IWayfinderMod modInstance)
                    {
                        LogInfo($"Starting mod: {modInstance.Name} v{modInstance.Version} by {modInstance.Author}");

                        modInstance.Start();

                        LoadedMods.Add(new ModInstance(modInstance, true));
                        LogSuccess($"Successfully loaded {modInstance.Name}.");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error starting mod class {type.Name}: {ex.Message}");
                }
            }
        }

        // This WILL be used in the UI later..
        public static void ToggleMod(ModInstance mod)
        {
            try
            {
                if (mod.IsEnabled)
                {
                    LogInfo($"Stopping mod: {mod.Mod.Name}");
                    mod.Mod.Stop();
                    mod.IsEnabled = false;
                }
                else
                {
                    LogInfo($"Starting mod: {mod.Mod.Name}");
                    mod.Mod.Start();
                    mod.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to toggle {mod.Mod.Name}: {ex.Message}");
            }
        }

        #region Specialized Logging Functions

        public static void LogInfo(string message) => WriteLog("INFO", message, ConsoleColor.White);
        public static void LogSuccess(string message) => WriteLog("OKAY", message, ConsoleColor.Green);
        public static void LogWarning(string message) => WriteLog("WARN", message, ConsoleColor.Yellow);
        public static void LogError(string message) => WriteLog("ERRO", message, ConsoleColor.Red);

        private static void WriteLog(string prefix, string message, ConsoleColor color)
        {
            StackTrace stackTrace = new StackTrace();
            StackFrame? frame = stackTrace.GetFrame(2);
            MethodBase? caller = frame?.GetMethod();

            string sender = caller?.DeclaringType?.Assembly.GetName().Name ?? "CORE";

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string formattedMessage = $"[{timestamp}] [{prefix}] [{sender}] {message}";

            Console.ForegroundColor = color;
            Console.WriteLine(formattedMessage);
            Console.ResetColor();

            try
            {
                File.AppendAllText(logFilePath, formattedMessage + Environment.NewLine);
            }
            catch { }
        }

        #endregion
    }
}