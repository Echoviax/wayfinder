using System.Numerics;
using HarmonyLib;
using Murder.Core.Graphics;
using Murder.Services;
using Road.Core;
using Road.StateMachines;
using Wayfinder.API;

namespace ConfigModTemplate
{
    public class Mod : WayfinderMod
    {
        public override string ID => "com.yourname.modname";
        public override string Name => "Template Mod";
        public override string Description => "A simple mod to test the Wayfinder API.";
        public override string Version => "1.0.0";
        public override string Author => "Your Name";

        // Create a static instance so our patches can easily reach the Config and ModDirectory
        // Read up on singletons if you aren't familiar
        public static Mod Instance { get; private set; }
        private Harmony _harmony;

        public override void Start()
        {
            Instance = this;

            // Seed the config values so the Wayfinder knows they exist and will be ised
            // If they aren't in the .json yet, this generates them
            Config.GetBool("ShowText", true);
            Config.GetInt("TextXPosition", 5);
            Config.GetInt("TextYPosition", 5);

            // Example of the ModDirectory property
            // string myFilePath = System.IO.Path.Combine(ModDirectory, "textures", "custom_ui.png");

            _harmony = new Harmony(ID);
            _harmony.PatchAll();
        }

        public override void Stop()
        {
            // Instantly disables all harmony patches made by this mod
            _harmony?.UnpatchAll(_harmony.Id);
        }
    }

    // A test patch!
    [HarmonyPatch(typeof(MainMenu), "DrawMainMenu")]
    public static class MainMenu_VisualTest_Patch
    {
        static void Postfix(RenderContext render)
        {
            // Since Wayfinder updates the dictionary in real-time, this updates instantly on screen.
            bool showText = Mod.Instance.Config.GetBool("ShowText");
            if (!showText) return;

            int textX = Mod.Instance.Config.GetInt("TextXPosition");
            int textY = Mod.Instance.Config.GetInt("TextYPosition");

            // Draw the text based on settings
            render.UiBatch.DrawText(
                11,
                "Template Mod is Active!",
                new Vector2(textX, textY),
                new DrawInfo(0.05f)
                {
                    Color = Palette.Colors[6]
                }
            );
        }
    }
}