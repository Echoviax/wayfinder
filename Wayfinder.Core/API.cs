using Wayfinder.API;

namespace Wayfinder.API
{
    public interface IModConfig
    {
        int GetInt(string key, int defaultValue = 0);
        void SetInt(string key, int value);

        float GetFloat(string key, float defaultValue = 0f);
        void SetFloat(string key, float value);

        bool GetBool(string key, bool defaultValue = false);
        void SetBool(string key, bool value);

        string GetString(string key, string defaultValue = "");
        void SetString(string key, string value);
    }

    // The 'I' apparently stands for 'Interface' (C# isn't my primary language)
    public interface IWayfinderMod
    {
        // Metadata
        string ID { get; }
        string Name { get; }
        string Description { get; }
        string Version { get; }
        string Author { get; }

        // Core Hooks
        void Start();

        void Stop(); 
    }

    // Modders should inherit this now!
    public abstract class WayfinderMod : IWayfinderMod
    {
        public abstract string ID { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract string Version { get; }
        public abstract string Author { get; }

        public string ModDirectory { get; internal set; } = "";
        public IModConfig Config { get; internal set; } = null!;

        public abstract void Start();
        public abstract void Stop();
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class WayfinderDependencyAttribute : Attribute
    {
        public string DependencyID { get; }

        public WayfinderDependencyAttribute(string dependencyID)
        {
            DependencyID = dependencyID;
        }
    }
}

public interface IConfigurableMod : IWayfinderMod
{
    void InitializeConfig(IModConfig config);
}