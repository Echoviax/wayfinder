using System.Numerics;
using HarmonyLib;
using Murder.Core.Graphics;
using Murder.Services;
using Road.Core;
using Road.StateMachines;
using Wayfinder.API;

namespace ConfigModTemplate
{
    public class ModEntry : IConfigurableMod
    {
        public string ID => "com.yourname.configmod";
        public string Name => "Config Template Mod";
        public string Description => "A simple mod to test the Wayfinder API.";
        public string Version => "1.0.0";
        public string Author => "Your Name";

        private Harmony _harmony;

        // Static so the patch below can easily access them
        public static bool ShowText = true;
        public static int TextX = 5;
        public static int TextY = 5;

        public void InitializeConfig(IModConfig config)
        {
            // Pull the values from the JSON, or auto-generate them if they don't exist
            ShowText = config.GetBool("ShowText", true);
            TextX = config.GetInt("TextXPosition", 5);
            TextY = config.GetInt("TextYPosition", 5);
        }

        public void Start()
        {
            _harmony = new Harmony(ID);
            _harmony.PatchAll();
        }

        public void Stop()
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
            // 1. Check the bool setting
            if (!ModEntry.ShowText) return;

            // 2. Use the int settings for the position
            render.UiBatch.DrawText(
                11,
                "Template Config Mod is Active!",
                new Vector2(ModEntry.TextX, ModEntry.TextY),
                new DrawInfo(0.05f)
                {
                    Color = Palette.Colors[6]
                }
            );
        }
    }
}