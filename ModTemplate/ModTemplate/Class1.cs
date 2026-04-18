using System.Numerics;
using HarmonyLib;
using Murder.Core.Graphics;
using Murder.Services;
using Road.Core;
using Road.StateMachines;
using Wayfinder.API;

namespace ModTemplate
{
    public class ModEntry : IWayfinderMod
    {
        public string Name => "Template Mod";
        public string Description => "A simple mod to test the Wayfinder API.";
        public string Version => "1.0.0";
        public string Author => "Your Name";

        private Harmony _harmony;

        public void Start()
        {
            _harmony = new Harmony("com.yourname.templatemod");
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
            // Draw text in the top-left corner of the screen
            render.UiBatch.DrawText(
                11, // The engine's standard Pixel Font ID
                "Template Mod is Active!",
                new Vector2(5, 5),
                new DrawInfo(0.05f) // Draw at 0.05f depth so it sits cleanly on top of the UI
                {
                    Color = Palette.Colors[6]
                }
            );
        }
    }
}