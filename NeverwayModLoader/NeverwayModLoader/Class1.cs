using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace NeverwayModLoader
{
    public static class LoaderCore
    {
        private static string logFilePath = "";

        public static void Initialize()
        {
            string exePath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)!;
            string modsDirectory = Path.Combine(exePath, "Mods");

            logFilePath = Path.Combine(exePath, "ModLoader_Log.txt");
            File.WriteAllText(logFilePath, $"--- Mod Loader Started at {DateTime.Now} ---\n");

            if (!Directory.Exists(modsDirectory))
            {
                Directory.CreateDirectory(modsDirectory);
                Log("Created Mods directory. Drop mod DLLs here!");
                return;
            }

            string[] modFiles = Directory.GetFiles(modsDirectory, "*.dll");
            Log($"Found {modFiles.Length} potential mod(s).");

            foreach (string modFile in modFiles)
            {
                try
                {
                    Assembly modAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(modFile);
                    StartMod(modAssembly);
                }
                catch (Exception ex)
                {
                    Log($"Failed to load {Path.GetFileName(modFile)}: {ex.Message}");
                }
            }
        }

        private static void StartMod(Assembly modAssembly)
        {
            Type? entryType = modAssembly.GetType("ModEntry");
            if (entryType != null)
            {
                MethodInfo? startMethod = entryType.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
                if (startMethod != null)
                {
                    startMethod.Invoke(null, null);
                    Log($"Successfully started mod: {modAssembly.GetName().Name}");
                }
                else
                {
                    Log($"Warning: {modAssembly.GetName().Name} has a ModEntry class, but no public static Start() method.");
                }
            }
            else
            {
                Log($"Warning: {modAssembly.GetName().Name} does not have a root ModEntry class.");
            }
        }

        private static void Log(string message)
        {
            try
            {
                File.AppendAllText(logFilePath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
            }
            catch {  }
        }
    }
}