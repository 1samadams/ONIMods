# Project: ONI "Auto Machines" Mod

## Goal
Build a Steam-local mod for Oxygen Not Included (Windows, Steam) that makes duplicant-operated fabricators run automatically once materials are delivered — no dupe standing at the machine cranking it.

## Target buildings
Patches target **config classes**, not UI names. All names below were verified present in `Assembly-CSharp.dll`. Each is individually toggleable via `config.json`.

All eleven `ComplexFabricator` buildings share one patch shape (`Patches/FabricatorPatches.cs`). Oil Refinery does not — see its own section below.

| Building (UI name) | Config class | Fabricator component | Status |
|---|---|---|---|
| Rock Crusher | `RockCrusherConfig` | `ComplexFabricator` | patched, **untested in-game** |
| Metal Refinery | `MetalRefineryConfig` | `LiquidCooledRefinery` | patched, untested in-game |
| Glass Forge | `GlassForgeConfig` | `GlassForge` | patched, untested in-game |
| Supermaterial Refinery | `SupermaterialRefineryConfig` | `ComplexFabricator` | patched, untested in-game |
| Microbe Musher | `MicrobeMusherConfig` | `MicrobeMusher` | patched, untested in-game |
| Cooking Station (Grill) | `CookingStationConfig` | `CookingStation` | patched, untested in-game |
| Gourmet Cooking Station (Gas Range) | `GourmetCookingStationConfig` | `GourmetCookingStation` | patched, untested in-game |
| Egg Cracker | `EggCrackerConfig` | `ComplexFabricator` | patched, untested in-game |
| Clothing Fabricator | `ClothingFabricatorConfig` | `ComplexFabricator` | patched, untested in-game |
| Suit Fabricator | `SuitFabricatorConfig` | `ComplexFabricator` | patched, untested in-game — one building, not two; Atmo/Lead suits are recipes on it |
| Apothecary | `ApothecaryConfig` | `Apothecary` | patched, untested in-game |
| Oil Refinery | `OilRefineryConfig` | `OilRefinery` (**not** a fabricator) | **experimental, disabled by default** |

`LiquidCooledRefinery`, `GlassForge`, `GourmetCookingStation`, `MicrobeMusher`, `CookingStation` and `Apothecary` all derive from `ComplexFabricator`, so `GetComponent<ComplexFabricator>()` finds them. **Use `GetComponent`, never `AddOrGet`** — `AddOrGet` would bolt a second, plain `ComplexFabricator` onto those buildings.

`EggCracker` (the component) is a bare `KMonoBehaviour`, but `EggCrackerConfig` also adds a plain `ComplexFabricator`, so it patches like the rest.

Reference implementation (do not patch): `KilnConfig`.

## Background — learn from the mod we're replacing
The abandoned Workshop mod "Configurable Automatic Machine" (2022) did this by *cloning* each building config into a new "automatic" building. That approach caused its known bugs: the cloned Oil Refinery output oxygen instead of natural gas and wrong oil masses, and the cloned Metal Refinery broke with MISSING/STRINGS errors after a game update. Do NOT clone buildings.

## Architecture — the surgical approach
The game already has a vanilla precedent: the **Kiln** is a `ComplexFabricator` that runs without duplicant operation. Duplicant-operated fabricators differ mainly by a `duplicantOperated = true` flag (and an operate-chore workable). Strategy:

1. Study `KilnConfig` in the decompiled game code as the reference implementation of an automatic fabricator.
2. Study `RockCrusherConfig` to see exactly what differs.
3. Write Harmony **postfix patches on each target building config's `DoPostConfigureComplete` (or `ConfigureBuildingTemplate` where appropriate)** that flip the existing building's `ComplexFabricator` to non-duplicant-operated, matching how the Kiln is set up.
4. Keep the patch surface as small as possible — this is what makes the mod survive game updates.

### VERIFIED: what the Rock Crusher patch must actually do
Decompiled from the current `Assembly-CSharp.dll` (`RockCrusherConfig`, `KilnConfig`, `ComplexFabricator`). Diff of the two configs:

| | Kiln (automatic) | Rock Crusher (dupe-operated) |
|---|---|---|
| `duplicantOperated` | `false` | `true` |
| `showProgressBar` | `true` | **never set** → defaults `false` |
| `ComplexFabricatorWorkable` | absent | **present**, with `overrideAnims` + `workingPstComplete` |

So the postfix must set **two** fields, not one:

```csharp
complexFabricator.duplicantOperated = false;
complexFabricator.showProgressBar   = true;   // required — see below
```

- `ComplexFabricator.duplicantOperated` defaults to `true`; `showProgressBar` defaults to `false`.
- `ComplexFabricator.ShowProgressBar()` gates on `show && showProgressBar && !duplicantOperated`. Rock Crusher never sets `showProgressBar`, so flipping *only* `duplicantOperated` yields a machine that runs automatically **with no progress bar**. Set both.

### DO NOT remove `ComplexFabricatorWorkable`
This overrides the original plan's "adjust/remove the operate workable requirement as needed" — removing it will crash.

`RockCrusherConfig.DoPostConfigureComplete` registers a `KPrefabID.prefabSpawnFn` delegate that does:

```csharp
ComplexFabricatorWorkable component = game_object.GetComponent<ComplexFabricatorWorkable>();
component.WorkerStatusItem = Db.Get().DuplicantStatusItems.Processing;
component.AttributeConverter = Db.Get().AttributeConverters.MachinerySpeed;
```

with no null check. Removing the component NREs at prefab spawn. **Leave the workable component in place** — with `duplicantOperated = false`, `ComplexFabricator` simply stops routing work through it. Confirm in testing (checklist item 4) that the operate errand disappears on its own; only if it does not should removal be revisited, and then via the delegate, not the component.

Confirmed: `RockCrusher`, `MetalRefinery`, `GlassForge`, `SupermaterialRefinery`, `GourmetCookingStation`, `ClothingFabricator` and `SuitFabricator` all register such a delegate. Keeping the component is also *safe*: `ComplexFabricator.HasWorker` returns `true` without dereferencing `workable` once `duplicantOperated` is false.

### Oil Refinery is a special case
`OilRefinery` is **not** a `ComplexFabricator` — it is a `StateMachineComponent<OilRefinery.StatesInstance>`. Conversion is performed by an `ElementConverter` that runs while `Operational.IsActive`, and vanilla drives that flag from a duplicant working an infinite-duration `WorkableTarget` (`OnStartWork` → `SetActive(true)`, `OnStopWork`/`OnCompleteWork` → `SetActive(false)`).

`Patches/OilRefineryPatch.cs` instead drives `Operational` from the state machine's `ready` state. It is **disabled by default in `config.json`** with two unresolved caveats:

1. `ready.ToggleChore` still creates the Fabricate errand, so a dupe may still walk over. Harmless, but testing-checklist item 4 will not pass for this building.
2. A dupe finishing or aborting work fires `OnStopWork`, which would switch the machine off mid-conversion. The `WorkableTarget` postfixes re-activate it whenever the refinery is still in `ready` — this is the riskiest part of the mod and needs play-testing.

This also explains the predecessor mod's Oil Refinery bug: cloning a building whose outputs live in an `ElementConverter` (Petroleum 5 kg/s + Methane 0.09 kg/s) is exactly how you end up emitting the wrong gas.

### Hard constraints
- **No save-persisted state.** Patches must not add components that serialize into saves. A save made with the mod on must load with the mod off. If any approach would violate this, stop and flag it loudly before implementing.
- **One patch class per building**, isolated, so a game update breaking one doesn't take down the rest.

### Conditional patching — use Harmony `Prepare()`
`base.OnLoad(harmony)` calls `PatchAll()`, which applies every `[HarmonyPatch]` class unconditionally. To honor per-building toggles, each patch class gets a static `Prepare()` that returns `false` when its building is disabled in `config.json`. Do **not** switch to manual `harmony.Patch()` calls; `Prepare()` is the intended mechanism and keeps the patch surface small.

### Secondary effects to verify in testing
Work time vs. machine order progress, skill/attribute speed bonuses (will no longer apply — acceptable), animations, and whether the "operate" errand disappears cleanly from the errands list.

## VERIFY BEFORE WRITING CODE — do not trust training memory
Game APIs change. Before writing any patch, inspect the **actual current assembly**:

- Game managed DLLs: `C:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed\` — confirmed present at this path on this machine. Note this is only the *decompiling* path; the **build** never hardcodes it, resolving the DLLs through `$(GameLibsDir)` from the monorepo root's gitignored `GamePath.local.props` instead.
- Install the decompiler as a dotnet tool: `dotnet tool install -g ilspycmd`, then decompile targeted types, e.g. `ilspycmd -t RockCrusherConfig <path>\Assembly-CSharp.dll`, and likewise `KilnConfig`, `ComplexFabricator`, `ComplexFabricatorWorkable`.
- Confirm exact field/property names (`duplicantOperated` or whatever it is now called), method names, and signatures from the decompiled output. If something doesn't match this brief, the decompiled code wins.

Verified present as identifiers in `Assembly-CSharp.dll` (string-level check only — confirm shape by decompiling): `duplicantOperated`, `ComplexFabricator`, `ComplexFabricatorWorkable`, `UserMod2`, and every config class in the target table.

## Toolchain & project setup
- C# class library, **.NET Framework 4.8** (`<TargetFramework>net48</TargetFramework>`). **Not net471**, despite the original brief: the game ships `0Harmony.dll` built against 4.8, so targeting 4.7.1 makes MSBuild refuse to resolve it (MSB3274) and, through the indirect dependency, `Assembly-CSharp` as well (MSB3275) — the build then fails with every game type "not found". The decompiled assembly wins over the brief.
- The targeting pack comes from the `Microsoft.NETFramework.ReferenceAssemblies` NuGet package rather than a machine-wide dev pack, so the build does not depend on what happens to be installed. SDK in use: **10.0.302**.
- **NuGet is repo-scoped by design.** The machine-wide `%APPDATA%\NuGet\NuGet.Config` has an intentionally empty `<packageSources>` block — **leave it that way.** This repo's `NuGet.config` restores nuget.org locally. Because NuGet resolves config by walking up from the working directory, **any `dotnet tool install` / `restore` must be run from inside this repo.** Running it elsewhere fails with "No NuGet sources are defined or enabled" — that error means wrong working directory, not a broken machine.
- `ilspycmd` **10.1.1.8388 is installed** at `%USERPROFILE%\.dotnet\tools\ilspycmd.exe`. It is not on PATH in this session's shells; invoke it by full path.
- **Verify tooling with PowerShell, not the Bash tool.** The Bash tool reported "No .NET SDKs were found" for a machine that has the SDK installed; its `dotnet` resolution is unreliable here. Use the PowerShell tool for `dotnet`, and re-check any negative result before recording it.
- References (`<Reference>` with `<HintPath>`, `Private=false` so game DLLs aren't copied to output): `Assembly-CSharp.dll`, `Assembly-CSharp-firstpass.dll`, `0Harmony.dll`, `UnityEngine.dll`, `UnityEngine.CoreModule.dll` — all present in the Managed folder above. These five are shared by every mod and are declared **once**, in the monorepo root's `Directory.Build.props`; don't re-declare them here or they'll resolve twice. `Newtonsoft.Json.dll` (needed for `config.json`) is specific to this mod and so stays in `AutoMachines.csproj`.
- Mod entry point: `AutoMachinesMod : KMod.UserMod2`. Verified shape — `UserMod2` exposes `assembly`, `path` and `mod` properties, and `OnLoad(Harmony)` calls `harmony.PatchAll(assembly)`. `path` is the deployed mod folder, which is where `config.json` is read from.
- **Load config before `base.OnLoad(harmony)`.** `PatchAll` evaluates every patch class's `Prepare()` during that call, so settings must already be in memory or every toggle reads as its default.
- Use `UnityEngine.Debug.Log`, fully qualified — ONI declares its own global `Debug` class, so an unqualified call is ambiguous.

## Repo layout
```
NuGet.config                                  # repo-scoped nuget.org source
src/AutoMachines/AutoMachines.csproj
src/AutoMachines/AutoMachinesMod.cs           # UserMod2 entry point
src/AutoMachines/Settings.cs                  # BuildingIds + config.json load
src/AutoMachines/Patches/FabricatorPatches.cs # the 11 ComplexFabricator buildings
src/AutoMachines/Patches/OilRefineryPatch.cs  # Oil Refinery only (different mechanism)
mod/mod.yaml                                  # source-controlled, copied on build
mod/mod_info.yaml                             # source-controlled, copied on build
mod/config.json                               # default config, copied on build
```

The eleven fabricator patches share one file because they are one pattern; each still gets its own `[HarmonyPatch]` class and its own `Prepare()`, so a game update that breaks one building's config class does not stop the other ten from patching.

## Build & deploy
Build command (verified working, clean):

```
dotnet build src/AutoMachines/AutoMachines.csproj -c Release
```

The build does two things after compiling:

1. **`StageMod`** assembles the complete, ready-to-install mod folder at `dist/AutoMachines/` (gitignored). This always succeeds.
2. **`DeployToLocalMods`** tries to copy that into the game's Local mods folder. This is `ContinueOnError` on purpose — see below.

`config.json` is copied **only if absent** in the deploy folder, so a player's edits survive rebuilds. Delete it there to regenerate defaults.

### The deploy path is NOT `%USERPROFILE%\Documents`
Two Windows behaviours bite here, and both were hit on this machine:

- **OneDrive Known Folder Move.** The real Documents folder is `%USERPROFILE%\OneDrive\Documents`, and that is where the game reads and writes (save files live there). `$(USERPROFILE)\Documents` still resolves to an unredirected, *game-invisible* folder — deploying there looks like it worked and does nothing. The csproj therefore resolves `$([System.Environment]::GetFolderPath(SpecialFolder.MyDocuments))`, which follows the redirect. Never hardcode `$(USERPROFILE)\Documents`.
- **Windows Controlled Folder Access** (Defender ransomware protection, `Get-MpPreference | Select EnableControlledFolderAccess`) is **enabled on this machine**. It guards the real Documents folder and blocks MSBuild from writing there, failing with a *misleading* `Could not find file` on create — it reads like the path is missing when the write was actually denied. Reads are unaffected, which is why directory listings look fine.

So the automatic deploy currently fails by design-of-the-OS, not by bug. The build prints what to do and still succeeds. To install:

**Copy `dist\AutoMachines` into `%USERPROFILE%\OneDrive\Documents\Klei\OxygenNotIncluded\mods\Local\` using File Explorer** — Explorer is a trusted app, so Controlled Folder Access allows it.

To make auto-deploy work instead, either allow `dotnet.exe`/MSBuild through Controlled Folder Access (needs admin) or turn it off. Do not do either without asking the user; it is a security setting.

Diagnostic tell: if a write to Documents fails with `Could not find file` while reads work, suspect Controlled Folder Access before suspecting the path.

`mod_info.yaml` needs `supportedContent: ALL`, `APIVersion: 2` (Harmony 2.x), and `minimumSupportedBuild`.
**Baseline game build: `744825`** (`U59-744825-SCRPN`, read from `Player.log`). This is the version every claim in this file was verified against; when an update breaks the mod, diff against this.

`mod.yaml` needs title, description, and staticID (e.g. `AutoMachines`).

## Debugging
Game log: `%USERPROFILE%\AppData\LocalLow\Klei\Oxygen Not Included\Player.log`

Iteration is log-driven — read this after every in-game test. Harmony patch failures, MISSING/STRINGS errors, and mod load errors all surface here.

## Configuration
**v1 (done):** JSON config in the mod folder (`config.json`), one boolean per building, read at load; patches gated via `Prepare()` as described above. Defaults: all eleven fabricator buildings enabled, `OilRefinery` disabled. Unknown keys are ignored with a log line, and a malformed file falls back to defaults rather than stopping the mod from loading.
**v2 (later, only after v1 works):** in-game options menu via Peter Han's PLib (NuGet package `PLib`; note PLib must be ILRepack-merged into the mod DLL per its docs). Don't start here — it adds build complexity.

## Testing checklist (user runs the game; ask them to report)
1. Game launches with mod enabled, no crash, mod listed in Mods menu.
2. Build a Rock Crusher, queue a recipe, deliver materials via dupe or sweeper → it runs with **no dupe operating it**, and shows a progress bar. Then repeat for the other ten fabricator buildings.
3. Output element and mass are IDENTICAL to vanilla (this is where the old mod failed). Check the Metal Refinery's coolant output and, if `OilRefinery` is ever enabled, that it still emits Methane at 0.09 kg/s and Petroleum at 5 kg/s — not Oxygen.
4. Errands overlay shows no orphaned "operate" chore.
5. Save/load cycle works; disable mod → save still loads. (See the no-save-persisted-state constraint under Architecture — this test confirms it, it does not discover it.)
6. `Player.log` clean of Harmony and mod-load errors.

## Working style
- All twelve buildings were implemented in one pass at the user's explicit direction, superseding the original "Rock Crusher first, then generalize" rule. The risk that rule guarded against was handled instead by decompiling and diffing **all twelve** configs before writing any code — which is what surfaced the Oil Refinery and `ComplexFabricator`-subclass differences. If that verification step is ever skipped, go back to one-building-at-a-time.
- **Nothing is verified in-game yet.** Everything above is verified against decompiled source and a clean compile only. Update the Status column as buildings are actually play-tested.
- When a game update later breaks the mod, the fix procedure is: re-decompile the affected config, diff against build `744825` assumptions, adjust the patch.
