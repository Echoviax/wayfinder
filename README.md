# Wayfinder

A  mod loader for **Neverway**. (Tested for prologue)

*Neverway* is packaged as a .NET single-file application, and so standard tools like BepInEx don't work as easily with it. Wayfinder *found a way* around this using .NET startup hooks.
Wayfinder is packaged as a drag-and-drop program to make modding as accessible as possible.

## Installation

**Step 1: Extract the files**
Download the repo as a `.zip` and extract its contents directly into your `Neverway` installation folder (the same folder that contains `Neverway.exe`). 

Your folder structure should look like this:
```text
Neverway/
 ├── Neverway.exe
 ├── _run.bat
 ├── RuntimePatcher.dll
 ├── NeverwayModLoader.dll
 ├── 0Harmony.dll
 └── Mods/
```

**Step 2: Launching the Game**
Because the mod loader relies on environment variables to hook the game, you cannot launch `Neverway.exe` normally if you want to play with mods.

### Windows
Navigate to your game folder and double-click **`_start.bat`**. This will inject the loader and start the game for you. 

### Linux / Steam Deck
If you are playing via Steam / Proton, you do not need to use the `.bat` file. You can inject the loader directly through Steam's launch options.
1. Right-click **Neverway** in your Steam Library and select **Properties**.
2. In the **General** tab, scroll down to **Launch Options**.
3. Copy and paste the following:
   ```text
   WINEDLLOVERRIDES="winhttp=n,b" DOTNET_STARTUP_HOOKS="%command_dir%/RuntimePatcher.dll" %command%
   ```
4. Close the properties window and launch the game normally through Steam.

---

## How to Install Mods
1. Download a compatible mod. (none exist yet, don't think about it too hard)
2. Place the mod's files inside the `Mods` folder located in your game directory.
3. Launch the game as above. Wayfinder will automatically detect and execute the mods.

---

## Troubleshooting & Logs
Yes these should be in a logs folder. Again, don't think about it too hard.

* **`ModLoader_Log.txt`**: Records startup and lists every mod that loaded (or failed).
* **`Patcher_CrashLog.txt`**: If the bootstrapper fails to hook the game, the error will be here.

---

## Creating Mods
Creating a mod through *Wayfinder* is incredibly simple. 
1. Create a new C# Class Library project targeting `.NET 8.0`.
2. Reference the game's core assemblies and `0Harmony.dll`.
3. Create a public class named exactly `ModEntry` in your root namespace.
4. Add a `public static void Start()` method to that class. 

The Mod Loader will automatically find and invoke your `Start()` method when the game boots. You can initialize your Harmony patches from there.