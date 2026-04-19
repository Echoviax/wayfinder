using System.Text.Json;
using System.Text.Json.Serialization;
using Wayfinder.API;

namespace Wayfinder.Core
{
    public class ModConfig : IModConfig
    {
        public Dictionary<string, int> IntSettings { get; set; } = new();
        public Dictionary<string, float> FloatSettings { get; set; } = new();
        public Dictionary<string, bool> BoolSettings { get; set; } = new();
        public Dictionary<string, string> StringSettings { get; set; } = new();

        [JsonIgnore]
        public string FilePath { get; set; } = "";

        public ModConfig() { }

        public int GetInt(string key, int defaultValue = 0)
        {
            if (IntSettings.TryGetValue(key, out int val)) return val;
            SetInt(key, defaultValue);
            return defaultValue;
        }
        public void SetInt(string key, int value) { IntSettings[key] = value; Save(); }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            if (FloatSettings.TryGetValue(key, out float val)) return val;
            SetFloat(key, defaultValue);
            return defaultValue;
        }
        public void SetFloat(string key, float value) { FloatSettings[key] = value; Save(); }

        public bool GetBool(string key, bool defaultValue = false)
        {
            if (BoolSettings.TryGetValue(key, out bool val)) return val;
            SetBool(key, defaultValue);
            return defaultValue;
        }
        public void SetBool(string key, bool value) { BoolSettings[key] = value; Save(); }

        public string GetString(string key, string defaultValue = "")
        {
            if (StringSettings.TryGetValue(key, out string val)) return val;
            SetString(key, defaultValue);
            return defaultValue;
        }
        public void SetString(string key, string value) { StringSettings[key] = value; Save(); }

        public void Save()
        {
            if (string.IsNullOrEmpty(FilePath)) return;
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                LoaderCore.LogError($"Failed to save config at {FilePath}: {ex.Message}");
            }
        }

        public static ModConfig Load(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    var config = JsonSerializer.Deserialize<ModConfig>(json) ?? new ModConfig();
                    config.FilePath = filePath;
                    return config;
                }
                catch (Exception ex)
                {
                    LoaderCore.LogWarning($"Failed to load config {filePath}, creating new. {ex.Message}");
                }
            }
            return new ModConfig { FilePath = filePath };
        }
    }
}