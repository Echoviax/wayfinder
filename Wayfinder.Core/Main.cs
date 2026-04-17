using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using HarmonyLib;

namespace Wayfinder.Core
{
    public static class LoaderCore
    {
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
            LogInfo("Applying internal Wayfinder patches...");
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
            string modName = modAssembly.GetName().Name ?? "Unknown Mod";
            Type? entryType = modAssembly.GetType("ModEntry");

            if (entryType != null)
            {
                MethodInfo? startMethod = entryType.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
                if (startMethod != null)
                {
                    LogSuccess("Running Start() from " + modName);
                    startMethod.Invoke(null, null);
                }
                else
                {
                    LogWarning("Skipping loading " + modName + ", missing `public static void Start()`");
                }
            }
            else
            {
                LogWarning("Skipping loading " + modName + ", missing 'ModEntry' class");
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