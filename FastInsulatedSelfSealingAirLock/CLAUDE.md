# Project: ONI "Fast Insulated Self Sealing AirLock" Mod

## Goal
A 1x2 manual pressure door that is a **perfect seal in the simulation** while
still animating open for Duplicants. Gas, liquid and heat are blocked in Auto and
Locked; only the explicit `Opened` control state lets anything through. Door
speed is configurable 1x-20x.

Published as Workshop item **3755915137**, `staticID`
`zerotheabsolute.FastInsulatedSelfSealingAirLock`. A community continuation of
Neavo's mod — see `mod/NOTICE.md` for the full lineage and the takedown offer.

**The building prefab ID is `FastInsulatedSelfSealingAirLock` and must never
change.** It is what existing subscribers have in their saves.

## This source was reconstituted from the shipped DLL
The original source was lost. There is no upstream repo for *this* mod. The tree
was recovered by decompiling the shipped 0.4.1 DLL:

```
ilspycmd <mod>\FastInsulatedSelfSealingAirLock.dll -o <out> -p --nested-directories -r "<game>\Managed"
```

**The `-r` pointing at the game's `Managed` folder is the whole trick.** Without
it ILSpy cannot resolve game types and emits 37 `//IL_xxxx: Unknown result type`
comments plus integer casts (`(BuildLocationRule)6`, `(ObjectLayer)9`,
`(DoorType)1`, `door.CurrentState == 1`). With it, **zero** artifacts — every
enum resolves to its real name and the output compiles essentially as-is. If you
ever need to re-decompile, do not hand-clean; pass `-r`.

The result was cross-checked against
[mrcyclo/ONIInsulatedSelfSealingAirLock](https://github.com/mrcyclo/ONIInsulatedSelfSealingAirLock)
— the upstream this lineage descends from, a *different* mod with prefab ID
`InsulatedSelfSealingAirLock`. The seal logic is semantically identical, which is
what confirms the decompile is faithful. That repo has **no LICENSE file**;
neither did Neavo's original.

## VERIFY BEFORE WRITING CODE — do not trust training memory
Everything below was read out of the **current** `Assembly-CSharp.dll`.

- Game managed DLLs: `C:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed\`
- `ilspycmd` 10.1.1.8388, `ilspycmd -t <Type> <dll>`.
- **Use the PowerShell tool for `dotnet`**, not Bash.
- Baseline game build: **`744825`**. `mod_info.yaml` declares
  `minimumSupportedBuild: 737790` (inherited from 0.4.1; deliberately not raised).

Two enums decide everything, both verified rather than remembered:

```csharp
Door.ControlState      { Auto = 0, Opened = 1, Locked = 2, NumStates = 3 }
Sim.Cell.Properties    { GasImpermeable = 1, LiquidImpermeable = 2,
                         SolidImpermeable = 4, Unbreakable = 8, ... }
```

So the mod's mask `7` is gas+liquid+solid impermeable, and a decompiled
`CurrentState == 1` is `ControlState.Opened`. Get either wrong and the seal
inverts.

## How the seal actually works — the door IS a hole
`Door.OnSpawn` unconditionally bypasses structure temperature for any
non-`Internal` door:

```csharp
if (DisplacesGas(doorType))              // doorType != Internal -> true for ManualPressure
    structureTemperatures.Bypass(handle);
```

That means the early-return in `Door.SetSimState` never fires on open, so vanilla
reaches `SimMessages.Dig(cell)` and **physically removes the door from its own
cells** every time it opens. The seal is therefore *not* a side effect of the
building being solid. It is held entirely by re-applying, on every transition:

```
sealed :  SetInsulation(cell, 0f) + SetCellProperties(cell, 7)
open   :  SetInsulation(cell, 1f) + ClearCellProperties(cell, 7)
```

Cell properties are **sticky**. Any transition that fails to re-apply them leaves
the door wide open until the next one that does. That is the entire bug class
this mod exists to manage.

The seal keys on `ControlState`, **not** on `IsOpen()`. In Auto the door animates
open for a Duplicant and still holds a perfect seal — that is the "self-sealing"
behaviour, and it is confirmed in-game (see Play-tests).

## The state machine gap — why `SetWorldState` is patched
Only four SM states call `SetWorldState(updateSim: true)`, and therefore
`SetSimState`: `open`, `closed`, `locked`, `Sealed`. These do **not**:
`opening`, `closing`, `closedelay`, **`closeblocked`**, `locking`, `unlocking`.

And `Door.RefreshControlState()` ends with `SetWorldState(updateSim: false)` —
deliberately skipping `SetSimState`. **Every** control-state change routes through
it: the UI toggle, the logic wire, `ApplyRequestedControlState`, `OnSpawn`, and
both `Sealed` enter/exit.

So hooking `SetSimState` alone (which is all upstream and every version through
0.4.1 do) misses all of them, and the door keeps whatever the last `SetSimState`
left:

- **Auto/Locked -> Opened**: stays sealed until the SM reaches `open`. Harmless.
- **Opened -> Auto/Locked**: stays **unsealed** until it reaches `closed`/`locked`. Leak.

`closeblocked` has no `SetWorldState` entry at all, so a Duplicant standing in the
doorway parks the door there indefinitely.

`DoorSetWorldStatePatch` closes this — it runs on both `updateSim` paths. It
early-returns on `updateSim: true` because `DoorSetSimStatePatch.Postfix` already
sealed there, and doubling the sim messages buys nothing.

**Measured window: 21 seconds** (log 21:05:04 toggle -> 21:05:25 `SetSimState`),
not the sub-second originally estimated — because `hasComplexUserControls` means a
Duplicant has to walk over and operate the door before it physically opens. Do
not remove this patch on the grounds that the window looks small.

## `Opened` is a deliberate vent — decided, do not "fix"
`Opened` drops gas, liquid *and* solid impermeability. A door left in Opened is a
genuine hole and equalises both atmospheres at roughly **a full room swap per
90 s** (measured: a chlorine room fully displaced by oxygen in ~90 s). This is
intended and matches upstream — it is how you vent a room on purpose.

This was explicitly considered and **left alone**. Two rejected alternatives:
making `Opened` seal too (removes the vent use case), and adding a
`SealWhenOpened` option (unmotivated — no evidence of a real defect). If a future
report says "the door leaks", check the control state *first*.

One consequence worth knowing: vanilla `Door.OnLogicValueChanged` does

```csharp
requestedState = flag ? ControlState.Opened : ControlState.Locked;
```

so an **automation-wired door is driven to `Opened` on green, never to Auto** —
it vents while the wire is green. Documented in the README. This is vanilla
behaviour, not something the mod can sensibly override.

## Diagnostics
`DebugLogging` writes `FISSAC.log` to `Application.persistentDataPath`
(`%USERPROFILE%\AppData\LocalLow\Klei\Oxygen Not Included\`). Opened with
`append: false`, so **each launch overwrites it**.

`LeakSummary` emits one `[Leak]` line per door per 5 s. `grep verdict=BREACH` is
the whole triage step.

**`doorGas` is dispositive.** Gas cannot cross without occupying the door's own
cells, so while sealed it must read exactly `0`. Everything else is room-level
movement that happens to be near a door.

`VerboseCellLogging` (default **off**) adds the old per-cell dump. It was 29,350
of 37,991 lines in the first captured log — 12.7 MB. Only turn it on after a
`[Leak]` line has named a door.

### Reading element labels — the artefact that wasted a round
ONI stores **one element and one mass per cell**; there are no mixed cells. At a
gas interface a cell flips its label between Oxygen and CarbonDioxide as the
dominant gas changes, and the reported mass jumps because you are looking at a
*different gas packet*. Observed directly, with no door state change:

```
21:05:26  Oxygen:3.889
21:05:31  CarbonDioxide:2.067    <- flips
21:05:42  Oxygen:3.742           <- flips back
```

A label that oscillates and returns while `dA`/`dB` stay flat is this artefact.
A real leak is sustained, directional, and shows in `doorGas` first. This is what
"CO2 is being created at the boundary" turned out to be.

## Repo layout
```
mod/mod.yaml, mod_info.yaml, NOTICE.md, preview.gif, anim/, translations/ (10 locales)
src/.../FastInsulatedSelfSealingAirLockMod.cs   UserMod2 entry; patch registration
src/.../Options.cs, ModStrings.cs               PLib options + strings
src/.../AirlockConstants.cs                     ids, mask 7
src/.../DoorPatchHelpers.cs                     ApplySelfSealingState — the core
src/.../TranslationFileResolver.cs              .po lookup
src/.../Buildings/FastInsulatedDoorConfig.cs    the BuildingDef
src/.../Diagnostics/FISSACLog.cs                the log file
src/.../Diagnostics/DoorDiagnostics.cs          per-cell snapshots (verbose)
src/.../Diagnostics/LeakSummary.cs              the one-line [Leak] verdict
src/.../Patches/*.cs                            10 Harmony patches
```

`DatabasePatches` and `TranslationPatch` install in `OnLoad`; everything else in
`OnAllModsLoaded`, each through `PatchClass` so a failure names itself.

## Build & deploy
```
dotnet build ONIMods.sln
```
PLib arrives via NuGet (`PLib` 4.25.0) and is **ILRepacked into the mod
assembly** — PLib is designed to be merged, not shipped alongside, so each mod
carries its own version and they arbitrate through `PRegistry`. This needs
`CopyLocalLockFileAssemblies=true` to override the monorepo-wide `false`, or
`PLib.dll` never lands in the output for ILRepack to find.

Stages to `dist/FastInsulatedSelfSealingAirLock/`, then deploys to the game's
`mods\Local\`. Upload `dist/`, never `mod/` — the latter has no DLL and scans as
empty (see the AutoMachines CLAUDE.md for the full Workshop packaging rules).

> **Testing note:** the Workshop copy declares the same `staticID`. Keep it
> disabled while testing the local build, or two mods claim one ID.

MSBuild rejects `--` inside XML comments (`MSB4025`). Do not write em-dashes as
`--` in the csproj.

## Status
Compiles clean, zero warnings, PLib merged, deployed.

**Play-test 1 (log 19:04, 49 doors, 12.7 MB):** doors seal correctly. CO2 never
occupied a door cell in 37,991 lines. One door held CO2 0.657 kg on one face and
O2 1.319 kg on the other, **unchanged to three decimals for five minutes**. The
`insulation=255` readings at load are a non-issue — 196 of 198 fall in the first
~19 s, before the sim has processed queued messages (ONI loads paused, the sampler
runs on `Time.unscaledTime`). No door was ever set to `Opened`, so
`DoorSetWorldStatePatch` was installed but its scenario untested.

**Play-test 2 (log 21:01):** the decisive one.
- **Auto + Duplicant traffic = perfect seal.** 8 `SetSimState` events with
  `isOpen=True, allowTx=False` over 30 s; door cells read `Vacuum:0.000`
  throughout. Dug open by vanilla, zero mass, nothing crossed.
- **Opened = real hole.** Two doors deliberately set to Opened in debug and left
  there. Door cells immediately filled with gas and a chlorine room was fully
  displaced by oxygen in ~90 s. Working as designed.
- `DoorSetWorldStatePatch` fired on both toggles, in the harmless direction. The
  `Opened -> Auto` direction it exists for is **still unverified in game.**

### Open items
1. **`Opened -> Auto` re-seal is untested.** The one thing that would close out
   `DoorSetWorldStatePatch`. Toggle a door with a live gas gradient from Opened
   back to Auto and confirm `[Leak]` reports `verdict=ok` immediately.
2. **`LeakSummary.xfer` baselines on a door's first sample**, so a door built or
   rebuilt mid-session starts fresh. It is a heuristic pointer; `doorGas` is the
   field to trust.
3. `mod_info.yaml` declares build `737790` while the dev machine runs `744825`.
   Untested below 744825.

## Testing checklist (user runs the game; ask them to report)
1. Game launches with the mod enabled; the airlock appears under **Base -> Doors**
   after `PressureDoor`, correctly named (not `MISSING.STRINGS...`).
2. It appears on the **Temperature Modulation** research node.
3. Build one between two rooms with different gases, leave it in **Auto**, walk
   Duplicants through repeatedly → the gases stay separated.
4. Same door set to **Opened** → the atmospheres mix. This is correct.
5. Toggle it **back to Auto** → mixing stops immediately. *(Open item 1.)*
6. Door opens visibly faster than a vanilla Manual Airlock; changing `Multiplier`
   and restarting changes the speed.
7. Options screen shows all three settings and they round-trip to
   `mods\config\FastInsulatedSelfSealingAirLock\`.
8. With `DebugLogging` on, `FISSAC.log` appears and `[Leak]` lines show
   `verdict=ok` for sealed doors. Log should be a few hundred KB, not megabytes —
   if it is huge, `VerboseCellLogging` got left on.
9. Save/load cycle: doors come back sealed, no gas jump at load.
10. Deconstruct one → the cells clear properly, no invisible wall left behind.
11. `Player.log` clean of Harmony errors.
