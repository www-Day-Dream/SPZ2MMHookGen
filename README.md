
# Shapez 2 MonoMod HookGen

Implements the `MonoMod.RuntimeDetour.HookGen` as a Shapez 2 built-in `IPatcher`.

Also adds MonoMod/Mono.Cecil/HarmonyX to a dynamically resolved path so you can use them. I recommend referencing those libraries by checking the version under [Third-Party Libraries](#third-party-libraries) and using Nuget to add them as references.

This allows mod developers to create `NET Standard 2.1` or `NET Framework 4.7.1` libraries with a class that implements Shapez 2's `IMod` and perform event based hooks into Shapez 2's `SPZGameAssembly.dll` & `Core.dll`.


## Installation
Download the latest release's `Shapez2AppDataExtract.zip` and extract it into the `/tobspr Games/shapez 2/` folder located wherever Unity's [Application.persistentDataPath](https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html) would put the game's application data. On Windows this is `(...)/AppData/LocalLow/`.

Note: The `/tobspr Games/shapez 2/` directory contains a few sub-directories, notably `blueprints`, `savegames`, and a file `Player.log`; However, until you extract the directories `mods` and `patchers` from your zip, and therefore making **both 'mods' and 'patchers' exist**, the ModLoader won't run Mods or Patchers. The fact that both directories must be present is why there is an empty `mods` folder included in the `Shapez2AppDataExtract.zip`.

#### Run the game!
---
After you've run the game, with both of the aforementioned directories present, two files should appear in the game's managed directory `(Where 'shapez 2.exe' is located)/shapez 2_data/Managed`, next to the originals, whose filenames are prefixed with `MMHook.`

#### Create your project and reference the generated dlls.
---
Once you've got a project setup and references to the generated dlls (and the base games version of those dlls ofc). We can create a mod that hooks existing methods!

The below pseudo-code should skip the starting scenes as a way to test your process (You'll need to use BepInEx.AssemblyPublicizer.MSBuild to publicize the game dll's or use System.Reflection to access the below private fields and you should reference Unity stuff too ofc):

**Note**: The way you bind to events `On.X.Y.Z +=` shown below is how you use HookGen to hook the type 'Preloader' that is not namespaced in one of the DLLs. But the patch might not work w/ your version of SPZ2.
```csharp
public class Mod : IMod
{
    public void Init(string path)
    {
        Debug.Log("Hello World from my custom Mod using HookGen!");

        On.Preloader.MoveToNextState += (orig, self) =>
        {
            self.StateIndex = self.StatePrefabs.Length + 1;
            orig(self);
        };
    }

    public ModMetadata Metadata { get; set; } =
        new ModMetadata("Test Mod", "www.Day.Dream", "0.1.0");
        // You should fill in your own information here ofc
}
```

#### Build your project
---
Copy only your output dll to your `/tobspr Games/shapez 2/mods/` folder.

## Third-Party Libraries
[MonoMod](https://github.com/MonoMod/MonoMod) - v22.5.1.1

[Mono.Cecil](https://github.com/jbevain/cecil) - v0.11.4

[HarmonyX](https://github.com/BepInEx/HarmonyX) - v2.10.2