using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using HarmonyLib;
using Wayfinder.API;

namespace Wayfinder.Core
{
    public class ModInstance
    {
        public IWayfinderMod Mod { get; }
        public bool IsEnabled { get; set; }
        public ModConfig Config { get; }

        public ModInstance(IWayfinderMod mod, ModConfig config, bool isEnabled)
        {
            Mod = mod;
            Config = config;
            IsEnabled = isEnabled;
        }
    }

    public static class LoaderCore
    {
        public static List<ModInstance> LoadedMods { get; } = new List<ModInstance>();

        private static string logFilePath = "";
        private static string masterConfigFilePath = "";
        private static string configsDirectory = "";

        private static Dictionary<string, bool> modStates = new Dictionary<string, bool>();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        public static void Initialize()
        {
            string coreAssemblyPath = Assembly.GetExecutingAssembly().Location;
            string wayfinderFolder = Path.GetDirectoryName(coreAssemblyPath)!;
            string exePath = Path.GetFullPath(Path.Combine(wayfinderFolder, ".."));
            string modsDirectory = Path.Combine(exePath, "Mods");

            configsDirectory = Path.Combine(modsDirectory, "Configs");
            logFilePath = Path.Combine(exePath, "Wayfinder_Log.txt");
            masterConfigFilePath = Path.Combine(configsDirectory, "Wayfinder_Config.json");

            File.WriteAllText(logFilePath, $"--- Wayfinder Started at {DateTime.Now} ---\n");

            try
            {
                AllocConsole();
                var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
                Console.SetOut(writer);
                Console.Title = "Wayfinder - Debug Terminal";
            }
            catch { }

            // intercepts console output to the log
            Console.SetOut(new ConsoleToFileWriter(Console.Out, logFilePath));

            LogInfo("Initializing Wayfinder...");

            ApplyCorePatches();
            LoadMasterConfig();

            if (!Directory.Exists(modsDirectory))
            {
                Directory.CreateDirectory(modsDirectory);
                LogWarning("Mods directory not found. Created 'Mods' folder");
                return;
            }

            if (!Directory.Exists(configsDirectory))
                Directory.CreateDirectory(configsDirectory);

            string[] modFiles = Directory.GetFiles(modsDirectory, "*.dll");
            LogInfo($"Found {modFiles.Length} potential mod(s) in directory.");

            List<IWayfinderMod> discoveredMods = new List<IWayfinderMod>();

            foreach (string modFile in modFiles)
            {
                try
                {
                    Assembly modAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(modFile);
                    var modTypes = modAssembly.GetTypes().Where(t => typeof(IWayfinderMod).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract).ToList();

                    if (modTypes.Count == 0)
                    {
                        LogWarning($"Skipping {modAssembly.GetName().Name}, no IWayfinderMod implementation found.");
                        continue;
                    }

                    foreach (var type in modTypes)
                    {
                        if (Activator.CreateInstance(type) is IWayfinderMod modInstance)
                        {
                            if (modInstance is WayfinderMod baseMod)
                                baseMod.ModDirectory = Path.GetDirectoryName(modAssembly.Location) ?? modsDirectory;
                            discoveredMods.Add(modInstance);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to read {Path.GetFileName(modFile)}: {ex.Message}");
                }
            }

            // sort by dependencies
            List<IWayfinderMod> sortedMods = SortDependencies(discoveredMods);

            // start them all
            foreach (var modInstance in sortedMods)
            {
                try
                {
                    string safeID = string.Join("_", modInstance.ID.Split(Path.GetInvalidFileNameChars()));
                    string modConfigPath = Path.Combine(configsDirectory, $"{safeID}.json");
                    ModConfig modConfig = ModConfig.Load(modConfigPath);

                    if (modInstance is WayfinderMod baseMod)
                        baseMod.Config = modConfig;
                    else if (modInstance is IConfigurableMod configMod) // fallback for older mods
                        configMod.InitializeConfig(modConfig);

                    // Saved state
                    bool shouldBeEnabled = true;
                    if (modStates.TryGetValue(modInstance.ID, out bool savedState))
                        shouldBeEnabled = savedState;
                    else
                    {
                        // First time seeing this mod
                        modStates[modInstance.ID] = true;
                        SaveMasterConfig();
                    }

                    if (shouldBeEnabled)
                    {
                        LogInfo($"Starting mod: {modInstance.Name} v{modInstance.Version} by {modInstance.Author}");
                        modInstance.Start();
                        LoadedMods.Add(new ModInstance(modInstance, modConfig, true));
                        LogSuccess($"Successfully loaded and enabled {modInstance.Name}.");
                    }
                    else
                    {
                        LogInfo($"Registered mod (disabled in config): {modInstance.Name} v{modInstance.Version}");
                        LoadedMods.Add(new ModInstance(modInstance, modConfig, false));
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error starting mod {modInstance.Name}: {ex.Message}");
                }
            }
        }

        private static List<IWayfinderMod> SortDependencies(List<IWayfinderMod> unsortedMods)
        {
            var sorted = new List<IWayfinderMod>();
            var visited = new HashSet<string>();
            var visiting = new HashSet<string>();

            void Visit(IWayfinderMod mod)
            {
                if (visited.Contains(mod.ID)) return; // already checked it

                if (visiting.Contains(mod.ID)) // same id, circular
                {
                    LogError($"Circular dependency detected involving {mod.ID}! Skipping loading, please contact the mod developer");
                    return;
                }

                visiting.Add(mod.ID);

                var dependencies = mod.GetType().GetCustomAttributes(typeof(WayfinderDependencyAttribute), true).Cast<WayfinderDependencyAttribute>();

                foreach (var dep in dependencies)
                {
                    var depMod = unsortedMods.FirstOrDefault(m => m.ID == dep.DependencyID);
                    if (depMod != null)
                        Visit(depMod);
                    else
                        LogWarning($"Mod '{mod.ID}' is missing dependency '{dep.DependencyID}'! It might crash or malfunction");
                }

                visiting.Remove(mod.ID);
                visited.Add(mod.ID);
                sorted.Add(mod);
            }

            foreach (var mod in unsortedMods)
                Visit(mod);

            return sorted;
        }

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

                modStates[mod.Mod.ID] = mod.IsEnabled;
                SaveMasterConfig();
            }
            catch (Exception ex)
            {
                LogError($"Failed to toggle {mod.Mod.Name}: {ex.Message}");
            }
        }

        private static void LoadMasterConfig()
        {
            if (File.Exists(masterConfigFilePath))
            {
                try
                {
                    string json = File.ReadAllText(masterConfigFilePath);
                    modStates = JsonSerializer.Deserialize<Dictionary<string, bool>>(json) ?? new Dictionary<string, bool>();
                }
                catch (Exception ex)
                {
                    LogWarning($"Failed to load master config, creating a new one. {ex.Message}");
                    modStates = new Dictionary<string, bool>();
                }
            }
        }

        private static void SaveMasterConfig()
        {
            try
            {
                string json = JsonSerializer.Serialize(modStates, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(masterConfigFilePath, json);
            }
            catch (Exception ex)
            {
                LogError($"Failed to save master config: {ex.Message}");
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
        }
        #endregion

        private class ConsoleToFileWriter : TextWriter
        {
            private readonly TextWriter _originalOut;
            private readonly string _logPath;

            public ConsoleToFileWriter(TextWriter originalOut, string logPath)
            {
                _originalOut = originalOut;
                _logPath = logPath;
            }

            public override Encoding Encoding => _originalOut.Encoding;

            public override void Write(string? value)
            {
                _originalOut.Write(value);
                AppendToFile(value);
            }

            public override void WriteLine(string? value)
            {
                _originalOut.WriteLine(value);
                AppendToFile(value + Environment.NewLine);
            }

            private void AppendToFile(string? text)
            {
                if (string.IsNullOrEmpty(text)) return;
                try
                {
                    File.AppendAllText(_logPath, text);
                }
                catch { }
            }
        }
    }
}