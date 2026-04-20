# Wayfinder

A  mod loader for **Neverway**. (Tested for prologue)

Wayfinder is a drag-and-drop program to make modding as accessible as possible.
*Neverway* is packaged as a .NET single-file application, and so standard tools like BepInEx don't work as easily with it. Wayfinder *found a way* (I'll stop) around this using .NET startup hooks.

## Installation

**Step 1: Extract the files**
Download the latest release for your architecture and extract its contents directly into your `Neverway` installation folder (the same folder that contains `Neverway.exe`).  
If you're not sure where this is, then right-click **Neverway** in your Steam Library and click **Installed Files** then **Browse...**   

Your folder structure should look like this:
```text
Neverway/
 ├── Neverway.exe
 ├── Wayfinder.Launcher.exe
 ├── ModdedCoreFiles/ (Generated on first boot)
 ├── Wayfinder/
 │   ├── 0Harmony.dll
 │   ├── Wayfinder.Core.dll
 │   └── Wayfinder.Patches.dll
 └── Mods/
     └── Configs/ (Generated on first boot)
       └── Wayfinder_Config.json (Generated on first boot)
```

**Step 2: Launching the Game**
Because the mod loader relies on environment variables to hook the game, you cannot launch `Neverway.exe` normally if you want to play with mods.  
*Note: the first time you launch Wayfinder, it will extract your game to a moddable state. Don't panic if it takes a minute to boot!*

### Windows
Navigate to your game folder and double-click **`Wayfinder.Launcher.exe`**. This will inject the loader and start the game for you. 

### Linux / Steam Deck
If you are playing via Steam / Proton, launch **`Wayfinder.Launcher`** through your console.  

---

## How to Install Mods
1. Download a compatible mod
2. Place the mod's files inside the `Mods` folder located in your game directory
3. Launch the game as above. Wayfinder will automatically detect and execute the mods

---

## Community / Support
Looking for mods, need help troubleshooting, or want to learn how to make your own? Join the modding community on **[Discord](https://discord.gg/BzgPQgw2nD)**!

Enjoy the work we do on **[Wayfinder](https://github.com/Echoviax/wayfinder)** and **Neverway** mods? 
Consider supporting the project on **[Ko-Fi](https://ko-fi.com//Echoviax)**. There is no obligation to donate, but it would be greatly appreciated!

---

## Logs
Yes these should be in a logs folder. Don't think about it too hard.

* **`Wayfinder_Log.txt`**: Records startup and lists every mod that loaded (or failed). Also records everything that prints to the console during gameplay

---

## Creating a Mod
All Wayfinder mods must implement the `WayfinderMod` abstract class. This ensures the mod loader knows exactly how to identify your mod, display it to the player, and safely turn it on or off.

### Getting Started

To create a mod for Wayfinder, you will need:
1. **.NET SDK** (.NET 8.0)
2. **Wayfinder.Core.dll** (Provided in the Wayfinder folder of the latest release)
3. **HarmonyLib** (Provided in the Wayfinder folder of the latest release)

Create a new C# Class Library project, reference the required DLLs, and create a class that implements `WayfinderMod` (`using Wayfinder.API`).

### The Mod Interfaces

Your main mod class must implement `WayfinderMod`. Wayfinder will automatically scan your `.dll` for any class using this abstract class and load it.  
If you don't implement `WayfinderMod`, then your mod **will not load**.

### Metadata Properties
These properties are read by the Wayfinder UI to display your mod to the player.
* `string ID { get; }`: The unique identifier for your mod. General format is `com.yourname.modname`. **DO NOT** change this after your first publish, or users will lose all saved settings!
* `string Name { get; }`: The display name of your mod. Keep it relatively short so it fits in the menu
* `string Description { get; }`: A brief explanation of what your mod does. This is displayed as a tooltip when the player hovers over your mod in the menu. Consider a sentence or 2
* `string Version { get; }`: Your mod's current version (ex., `"1.0.0"`)
* `string Author { get; }`: Your name or handle or literally anything to identify you

### Core Methods
* `void Start()`: Called when the mod is enabled. This is where you should instantiate Harmony, apply your patches, register custom events, or spawn persistent entities.
* `void Stop()`: Called when the player disables your mod in the menu. **This must completely undo everything your mod did.** You must unpatch your Harmony instances, destroy any custom UI/entities you spawned, and unhook any C# events to prevent memory leaks and game crashes.

### Example Mod Templates

Included below, as well as in the repo is a copy-pasteable template for a Wayfinder mod using Harmony

```csharp
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
```

### Installing and Testing Your Mod

1. Build your project (`Ctrl+Shift+B` or `Build` -> `Build Solution`)
2. Locate the output `.dll` file (e.g., `bin/Debug/net8.0/WayfinderMod.dll`)
3. Navigate to the game's root directory and open the `Mods` folder (Wayfinder will create this folder automatically on its first run)
4. Drop your `.dll` into the `Mods` folder
5. Launch the game!