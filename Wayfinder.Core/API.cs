namespace Wayfinder.API
{
    // The 'I' apparently stands for 'Interface' (C# isn't my primary language)
    public interface IWayfinderMod
    {
        // Metadata
        string Name { get; }
        string Description { get; }
        string Version { get; }
        string Author { get; }

        // Core Hooks
        void Start();

        void Stop(); 
    }
}