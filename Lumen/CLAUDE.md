# Project: ONI "Lumen" Mod

## Goal
Five **new** motion-activated light buildings for Oxygen Not Included, unlocked
alongside the Duplicant Motion Sensor. Each draws 1 W and is dark (and drawing
zero watts) until a Duplicant is nearby, so Duplicants still collect the
lit-workspace work speed bonus without the base being lit around the clock.

**Hard scope rule: do not touch vanilla lights.** An earlier version of this idea
patched `CeilingLightConfig` / `FloorLampConfig` / `SunLampConfig` wattage in
place. That was dropped deliberately, at the user's direction. Patching existing
lights breaks Bristle Blossom farms (a motion-sensored light cannot grow plants,
which need continuous light) and changes the meaning of buildings already placed
in saves. New buildings have neither problem. If a future request sounds like
"make all lights cheaper", re-read this paragraph first.

## VERIFY BEFORE WRITING CODE — do not trust training memory
Same rule as AutoMachines. Everything below was read out of the **current**
`Assembly-CSharp.dll`, not remembered.

- Game managed DLLs: `C:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed\`
- `ilspycmd` 10.1.1.8388 at `%USERPROFILE%\.dotnet\tools\ilspycmd.exe` (not on PATH; call by full path).
  `ilspycmd -t <Type> <dll>`. It cannot resolve nested generics such as
  `Components.Cmps\`1` — decompile the outer type (`-t Components`) and read the
  nested class out of the output.
- **Use the PowerShell tool for `dotnet`**, not the Bash tool; Bash's `dotnet`
  resolution is unreliable here.
- Baseline game build: **`744825`**. Every claim in this file was verified
  against it.

## Architecture — how one Operational flag does all the work

This is the load-bearing insight of the mod. `LumenMotionSensor` never touches
`Light2D`, the animation, or the power draw directly. It sets **one**
`Operational.Flag` and the vanilla chain does the rest:

```
Operational.SetFlag(OccupiedFlag, false)
  -> UpdateOperational() finds a false flag, so IsOperational = false
  -> SetActive(false)
       -> EnergyConsumer.WattsUsed returns 0f when !operational.IsActive
  -> Trigger(-592767678 /* GameHashes.OperationalChanged */, false)
       -> Light2D.OnOperationalChangedDelegate sets Light2D.enabled = false
          (Light2D.autoRespondToOperational defaults true), which removes the
          emitter from the light grid
       -> LightController transitions on -> off and plays the "off" anim
```

Verified in `Operational.UpdateOperational`, `EnergyConsumer.WattsUsed`,
`Light2D.OnOperationalChangedDelegate` and `LightController.InitializeStates`.

Consequences worth knowing:

- The flag must be a **single static instance**. `Operational.Flags` is a
  `Dictionary<Flag, bool>` keyed by reference; a per-instance `Flag` would add a
  fresh entry on every call and never resolve to the same key.
- Zero watts while dark is real, not cosmetic — `WattsUsed` short-circuits on
  `IsActive`.
- `SetFlag` is only called on an actual edge. It is cheap in isolation, but it
  fires events that ripple into `LightGridManager`; calling it every 200 ms for
  every light would churn the light grid for nothing.

### The work speed bonus is per-tick, so a motion sensor is enough
`Workable` checks `Grid.LightIntensity[workerCell] > DUPLICANTSTATS.STANDARD.Light.NO_LIGHT`
on **every work update** and adds `LIGHT_WORK_EFFICIENCY_BONUS` when it passes.
There is no ramp-up or minimum lit duration, so a light that comes on when the
Duplicant arrives delivers the full bonus. This is why the design works at all —
confirm it still holds if Klei ever reworks `Workable`.

### Wattage
`BuildingDef.EnergyConsumptionWhenActive` is the only value to set.
`EnergyConsumer.OnPrefabInit` copies it into `BaseWattageRating`, and
`WattsNeededWhenActive` falls back to the def. So one assignment drives both the
build-menu number and the real draw.

**This is why `Settings.Load` must run before `base.OnLoad(harmony)`** — the def
is created later, but from config values that must already be in memory.

## Registration — the ordering trap
`LoadGeneratedBuildingsPatch` is a **prefix on
`GeneratedBuildings.LoadGeneratedBuildings`**, and it must stay one. It was
originally a postfix on `Db.Initialize`, which is a coin flip. The reason:

```
BuildingConfigManager.RegisterBuilding
  -> BuildingDef.PostProcess
       -> Db.Get().TechItems.AddTechItem(PrefabID, ...)
            -> GetTechFromItemID(id) scans every tech's unlockedItemIDs
               and AddTechItem RETURNS NULL if it finds nothing
```

A building whose ID is not on a tech *at the moment it registers* never gets a
`TechItem` and never appears in the research screen. `Db.Get()` is a lazy
singleton that `BuildingDef.PostProcess` can itself trigger, so a `Db.Initialize`
postfix can land in the middle of the registration sweep. The prefix is ordered
by construction.

The plan screen is safe in the same prefix: `LoadGeneratedBuildings` prunes plan
entries whose `BuildingDef` is null, but only *after* its `RegisterBuilding`
loop, by which point these defs exist.

Facts behind the constants in `LumenLights.cs`:

- Tech `"LogicControl"` unlocks `LogicDuplicantSensor` (the Duplicant Motion
  Sensor). Verified in `Database.Techs.Init`.
- Every vanilla light sits in plan category `Furniture`, subcategory `lights`
  (`TUNING.BUILDINGS.PLANORDER`).
- `Strings.Add(key, value)` hashes the **whole dotted key** as one string;
  `Strings.Get` does the same. So the flat
  `"STRINGS.BUILDINGS.PREFABS.<ID>.NAME"` form is correct.
- `UI.FormatAsLink` does **not** resolve from a mod assembly here. Plain names.

## Art — reused kanims, tinted
No custom `.kanim` pipeline. `LumenTint` applies
`KBatchedAnimController.TintColour` so five buildings sharing three Klei anims
read as five fixtures.

- The tint must be applied **at spawn, not at prefab-configure time**.
  `TintColour` writes through to `batchInstanceData`, which does not exist until
  the controller is batched, so setting it on the inactive prefab template is
  silently dropped.
- Only kanims driven by `LightController` are usable, because `LightController`
  plays the literal anim names `"on"` and `"off"`. Safe: `ceilinglight_kanim`,
  `glassceilinglight_jelly_green_kanim`, `floorlamp_kanim`, `sun_lamp_kanim`.
  **Not** `mercurylight_kanim` — the Mercury Ceiling Light has its own
  `MercuryLight` state machine with different anim names.

## No save-persisted state
Same constraint as AutoMachines. `LumenMotionSensor` and `LumenTint` have no
`[Serialize]` fields and are not `ISaveLoadable`, so they write nothing.
`Operational.Flags` is not serialised either, so no stale flag survives removing
the mod. The buildings themselves are of course new prefabs — a save containing
them will lose those buildings if the mod is removed, which is normal for any
mod that adds buildings and is not what this constraint is about.

## Repo layout
```
mod/mod.yaml, mod_info.yaml, config.json
src/Lumen/LumenMod.cs                        UserMod2 entry point; load order matters
src/Lumen/Settings.cs                        config.json
src/Lumen/LumenLight.cs                      the per-building record type
src/Lumen/LumenLights.cs                     the five lights, as data
src/Lumen/LumenStrings.cs                    string table registration
src/Lumen/Buildings/LumenLightConfig.cs      one shared IBuildingConfig
src/Lumen/Buildings/LumenBuildingConfigs.cs  the five concrete configs
src/Lumen/Components/LumenMotionSensor.cs    the sensor
src/Lumen/Components/LumenTint.cs            reused-kanim tinting
src/Lumen/Components/MinionPositions.cs      shared per-frame duplicant snapshot
src/Lumen/Patches/RegisterLightsPatch.cs     tech + build menu
```

`LumenLightConfig` is abstract on purpose: `LoadGeneratedBuildings` enumerates
every **non-abstract** `IBuildingConfig` and calls `Activator.CreateInstance`, so
the base is skipped and only the five concrete subclasses register. Adding a
sixth light means one entry in `LumenLights.All` plus a four-line subclass.

## Known limitations / deliberate trade-offs
1. **No line of sight.** Detection is straight-line distance, so a Duplicant on
   the far side of a wall trips the sensor. A real visibility test would need the
   lit-cell set, which does not exist while the light is off — the state we are
   trying to leave.
2. **No status item** explaining *why* an unlit fixture is non-operational. The
   dark animation is the only feedback. A custom `StatusItem` would need its own
   string registration; deferred.
3. **Disabling a light in `config.json` still registers the prefab**, it just
   never reaches the build menu or a tech. `LoadGeneratedBuildings` sweeps the
   assembly for config types and there is no supported opt-out.
4. **No logic port.** The Sentry was sketched with an automation output that goes
   green on detection. Dropped from v1 to keep the API surface small; it would be
   `def.LogicOutputPorts` plus a `LogicPorts.SendSignal` call in the sensor.
5. `SelfHeatKilowattsWhenActive` is **derived**, not chosen: `watts * 0.05`, which
   is vanilla's own ratio (a Ceiling Light burns 10 W and emits 0.5 kW). Deriving
   it means raising `Watts` in `config.json` also makes the fixture hotter,
   instead of quietly handing out free cooling. Do not replace this with a
   literal.

## Build & deploy
```
dotnet build src/Lumen/Lumen.csproj -c Release
```
Stages to `dist/Lumen/`, then tries to copy into the game's Local mods folder.
The deploy is `ContinueOnError` because Windows Controlled Folder Access guards
the real Documents folder and fails with a misleading `Could not find file`. See
the AutoMachines CLAUDE.md for the full explanation — it applies verbatim.

On this machine the deploy currently **succeeds**, to
`C:\Users\1sama\OneDrive\Documents\Klei\OxygenNotIncluded\mods\Local\Lumen`.

## Status
**Compiles clean, zero warnings, deployed. Nothing verified in-game yet.**
Everything above is verified against decompiled source and a clean build only.

## Testing checklist (user runs the game; ask them to report)
1. Game launches with the mod enabled, no crash, "Lumen" listed in the Mods menu.
2. All five lights appear under **Furniture → Lights**, correctly named — not
   `MISSING.STRINGS...`. A missing name means the string key is wrong.
3. All five appear on the **Logic Control** research node. Absent from research
   but present in the menu means the tech ordering trap above has regressed.
4. Build one, power it, stand a Duplicant under it → it lights up. Walk the
   Duplicant away → it goes dark after the linger time.
5. Power overlay shows **0 W** while dark and the configured wattage while lit.
6. A Duplicant working at a machine under a lit fixture shows the light work
   speed bonus status item.
7. The five look visually distinct (tint applied) rather than three identical
   pairs. If they all look untinted, `LumenTint` is running too early.
8. Save/load cycle works; the lights come back dark and re-trigger correctly.
9. Set `"Enabled": false` for one light → it disappears from the build menu and
   the research node, and the rest still work.
10. `Player.log` clean of Harmony errors and MISSING/STRINGS errors.
11. Performance sanity: build ~50 of them and confirm no frame-time regression.
    If there is one, the suspect is light grid churn from `SetFlag` edges, not
    the distance scan — raise `LingerSeconds`.
