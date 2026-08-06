# Project: ONI "Auto Machines" Mod

## Goal
Build a Steam-local mod for Oxygen Not Included (Windows, Steam) that makes duplicant-operated fabricators run automatically once materials are delivered — no dupe standing at the machine cranking it. Target buildings (configurable, each individually toggleable):

- Rock Crusher (primary target — get this working first)
- Metal Refinery
- Glass Forge
- Supermaterial Refinery
- Oil Refinery
- Microbe Musher
- Cooking Station (Grill)
- Gourmet Cooking Station (Gas Range)
- Egg Cracker
- Clothing Fabricator
- Suit Fabricators
- Apothecary

## Background — learn from the mod we're replacing
The abandoned Workshop mod "Configurable Automatic Machine" (2022) did this by *cloning* each building config into a new "automatic" building. That approach caused its known bugs: the cloned Oil Refinery output oxygen instead of natural gas and wrong oil masses, and the cloned Metal Refinery broke with MISSING/STRINGS errors after a game update. Do NOT clone buildings.

## Architecture — the surgical approach
The game already has a vanilla precedent: the **Kiln** is a `ComplexFabricator` that runs without duplicant operation. Duplicant-operated fabricators differ mainly by a `duplicantOperated = true` flag (and an operate-chore workable). Strategy:

1. Study `KilnConfig` in the decompiled game code as the reference implementation of an automatic fabricator.
2. Study `RockCrusherConfig` to see exactly what differs.
3. Write Harmony **postfix patches on each target building config's `DoPostConfigureComplete` (or `ConfigureBuildingTemplate` where appropriate)** that flip the existing building's `ComplexFabricator` to non-duplicant-operated, matching how the Kiln is set up. Adjust/remove the operate workable requirement as needed.
4. Keep the patch surface as small as possible — this is what makes the mod survive game updates.

Watch for secondary effects to verify in testing: work time vs. machine order progress, skill/attribute speed bonuses (will no longer apply — acceptable), animations, and whether the "operate" errand disappears cleanly from the errands list.

## VERIFY BEFORE WRITING CODE — do not trust training memory
Game APIs change. Before writing any patch, inspect the **actual current assembly**:

- Game managed DLLs: `C:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed\` (ask the user if Steam is installed elsewhere; check both `Assembly-CSharp.dll` and, if types aren't found there, the other assemblies in that folder).
- Install the decompiler as a dotnet tool: `dotnet tool install -g ilspycmd`, then decompile targeted types, e.g. `ilspycmd -t RockCrusherConfig <path>\Assembly-CSharp.dll`, and likewise `KilnConfig`, `ComplexFabricator`, `ComplexFabricatorWorkable`.
- Confirm exact field/property names (`duplicantOperated` or whatever it is now called), method names, and signatures from the decompiled output. If something doesn't match this brief, the decompiled code wins.

## Toolchain & project setup
- C# class library, **.NET Framework 4.7.1** (`<TargetFramework>net471</TargetFramework>`). Requires .NET SDK plus .NET Framework 4.7.1 targeting pack — check they're installed; help the user install if missing.
- References (`<Reference>` with `<HintPath>`, set `Private=false` so game DLLs aren't copied to output): `Assembly-CSharp.dll`, `Assembly-CSharp-firstpass.dll`, `0Harmony.dll`, `UnityEngine.dll`, `UnityEngine.CoreModule.dll` — all from the Managed folder above.
- Mod entry point: a class inheriting `KMod.UserMod2`, override `OnLoad(Harmony harmony)`, call `base.OnLoad(harmony)` (which runs `PatchAll`). Verify `UserMod2`'s current shape in the decompiled code.

## Mod packaging
Output goes to: `%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\Local\AutoMachines\`

That folder needs:
- The compiled `AutoMachines.dll`
- `mod.yaml` — title, description, staticID (e.g. `Sam.AutoMachines`)
- `mod_info.yaml` — `supportedContent: ALL`, `APIVersion: 2` (Harmony 2.x), and `minimumSupportedBuild` set to a recent game build number (find the current build number from the game's logs or title screen; ask the user if needed).

Add a post-build step (csproj `Target` or a small script) that copies the DLL + yaml files into that Local mods folder so build → test is one command.

## Configuration
**v1:** simple JSON config file in the mod folder (`config.json`) with a boolean per building, read at load; patches only apply for enabled buildings. Default: all enabled.
**v2 (later, only after v1 works):** in-game options menu via Peter Han's PLib (NuGet package `PLib`; note PLib must be ILRepack-merged into the mod DLL per its docs). Don't start here — it adds build complexity.

## Testing checklist (user runs the game; ask them to report)
1. Game launches with mod enabled, no crash, mod listed in Mods menu.
2. Build a Rock Crusher, queue a recipe, deliver materials via dupe or sweeper → it runs with **no dupe operating it**.
3. Output element and mass are IDENTICAL to vanilla (this is where the old mod failed — especially verify Oil Refinery natural gas output if/when that building is enabled).
4. Errands overlay shows no orphaned "operate" chore.
5. Save/load cycle works; disable mod → save still loads (patches shouldn't add components that persist in saves — flag it loudly if any approach would).

## Working style
- Get the Rock Crusher working end-to-end before touching any other building. One building, compiled, tested in-game, verified output masses — then generalize.
- When a game update later breaks the mod, the fix procedure is: re-decompile the affected config, diff against assumptions, adjust the patch. Keep patches isolated per-building so one breakage doesn't take down the rest.
