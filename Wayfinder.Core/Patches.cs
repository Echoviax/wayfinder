using HarmonyLib;
using Murder.Core.Graphics;
using Murder.Core.Input;
using Murder.Services;
using Murder.Utilities;
using Murder.Core.Geometry;
using Road.Core;
using Road.StateMachines;
using System.Numerics;
using System.Reflection;
using Wayfinder.UI;

namespace Wayfinder.Patches
{
    [HarmonyPatch(typeof(MainMenu))]
    [HarmonyPatch("DrawFootnotes")]
    internal static class MainMenu_DrawFootnotes_Patch
    {
        static void Postfix(MainMenu __instance, RenderContext render, float ____menuFadeDelta)
        {
            Point point = render.Camera.Size / 2f;
            Vector2 customTextPosition = new Vector2(point.X, render.Camera.Size.Y - 25);

            render.UiBatch.DrawText(
                11,
                "Wayfinder v0.3.2",
                customTextPosition,
                new DrawInfo(Palette.Colors[2] * ____menuFadeDelta, 0.6f)
                {
                    Origin = Vector2Helper.Center,
                    CultureInvariant = true
                }
            );
        }
    }

    [HarmonyPatch(typeof(MainMenu), "GetMainMenuOptions")]
    public static class MainMenu_AddModsOption_Patch
    {
        public static MenuOption[] TrackedMainMenuOptions = null;

        static void Postfix(MainMenu __instance, ref MenuInfo __result)
        {
            var oldOptions = __result.Options;
            var newOptions = new MenuOption[oldOptions.Length + 1];

            newOptions[0] = oldOptions[0];
            newOptions[1] = oldOptions[1];
            newOptions[2] = new MenuOption("Mods");
            newOptions[3] = oldOptions[2];

            __result = new MenuInfo(newOptions) { Sounds = __result.Sounds };

            TrackedMainMenuOptions = newOptions;

            Traverse.Create(__instance).Field("_connectTimes").SetValue(new float[newOptions.Length]);
            Traverse.Create(__instance).Field("_disconnectTimes").SetValue(new float[newOptions.Length]);
        }
    }

    [HarmonyPatch(typeof(Bang.World), "Update")]
    public static class World_Capture_Patch
    {
        static void Prefix(Bang.World __instance)
        {
            WayfinderMenuManager.ActiveWorld = __instance;
        }
    }

    [HarmonyPatch(typeof(MainMenu), "DrawMainMenu")]
    public static class MainMenu_CaptureMainMenu_Patch
    {
        static void Prefix(MainMenu __instance)
        {
            WayfinderMenuManager.ActiveMainMenu = __instance;
        }
    }

    [HarmonyPatch]
    public static class PlayerInput_VerticalMenu_Patch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.GetDeclaredMethods(typeof(PlayerInput))
                .First(m => m.Name == nameof(PlayerInput.VerticalMenu) && !m.IsGenericMethod
                && m.GetParameters().FirstOrDefault()?.ParameterType == typeof(MenuInfo).MakeByRefType());
        }

        static bool Prefix([HarmonyArgument(0)] ref MenuInfo info)
        {
            bool isMainMenu = info.Options != null && info.Options == MainMenu_AddModsOption_Patch.TrackedMainMenuOptions;

            if (WayfinderMenuManager.IsWayfinderMenuOpen && isMainMenu)
                return false;

            return true;
        }

        static void Postfix([HarmonyArgument(0)] ref MenuInfo info, ref bool __result)
        {
            bool isMainMenu = info.Options != null && info.Options == MainMenu_AddModsOption_Patch.TrackedMainMenuOptions;

            if (isMainMenu)
            {
                if (__result)
                {
                    if (info.Selection == 2)
                    {
                        if (WayfinderMenuManager.ActiveWorld != null && !WayfinderMenuManager.IsWayfinderMenuOpen)
                        {
                            Core.LoaderCore.LogInfo("Spawning Mod Menu State Machine!");
                            var uiEntity = WayfinderMenuManager.ActiveWorld.AddEntity();
                            uiEntity.AddComponent(new Bang.StateMachines.StateMachineComponent<WayfinderModMenuStateMachine>());
                        }
                        else
                            Core.LoaderCore.LogError("ActiveWorld is NULL! UI aborted.");

                        __result = false;
                    }
                    else if (info.Selection == 3)
                        info.Select(2);
                }
            }
        }
    }
}