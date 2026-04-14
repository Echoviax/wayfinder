# Wayfinder

A  mod loader for **Neverway**. (Tested for prologue)

*Neverway* is packaged as a .NET single-file application, and so standard tools like BepInEx don't work as easily with it. Wayfinder *found a way* around this using .NET startup hooks.
Wayfinder is a drag-and-drop program to make modding as accessible as possible.

## Installation

**Step 1: Extract the files**
Download the latest release for your architecture and extract its contents directly into your `Neverway` installation folder (the same folder that contains `Neverway.exe`).  
If you're not sure where this is, then right-click **Neverway** in your Steam Library and click **Installed Files** then **Browse...**   

Your folder structure should look like this:
```text
Neverway/
 ├── Neverway.exe
 ├── WayfinderLauncher(.exe)
 ├── RuntimePatcher.dll
 ├── NeverwayModLoader.dll
 ├── 0Harmony.dll
 └── Mods/
```

**Step 2: Launching the Game**
Because the mod loader relies on environment variables to hook the game, you cannot launch `Neverway.exe` normally if you want to play with mods.

### Windows
Navigate to your game folder and double-click **`WayfinderLauncher.exe`**. This will inject the loader and start the game for you. 

### Linux / Steam Deck
If you are playing via Steam / Proton, launch **`WayfinderLauncher`** through your console.  

---

## How to Install Mods
1. Download a compatible mod.  
2. Place the mod's files inside the `Mods` folder located in your game directory.
3. Launch the game as above. Wayfinder will automatically detect and execute the mods.

---

## Troubleshooting & Logs
Yes these should be in a logs folder. Don't think about it too hard.

* **`ModLoader_Log.txt`**: Records startup and lists every mod that loaded (or failed).

---

## Creating Mods
Creating a mod through *Wayfinder* is simple. 
1. Create a new C# Class Library project targeting `.NET 8.0`.
2. Reference the game's core assemblies and `0Harmony.dll`. (Optional)
3. Create a public class named exactly `ModEntry` in your root namespace.
4. Add a `public static void Start()` method to that class. 

The Mod Loader will automatically find and invoke your `Start()` method when the game boots. You can initialize your Harmony patches or other code from there.