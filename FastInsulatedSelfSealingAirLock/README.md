# Fast Insulated Self Sealing AirLock

A community continuation of Neavo's *Fast Insulated Self Sealing AirLock* for
*Oxygen Not Included*. Published on the Workshop as
[3755915137](https://steamcommunity.com/sharedfiles/filedetails/?id=3755915137).

A 1×2 manual pressure door that stays a **perfect seal in the simulation** while
still animating open for Duplicants. Gas, liquid and heat are all blocked while
the door is in Auto or Locked; only the explicit **Opened** control state lets
anything through. Door speed is configurable from 1× to 20×.

Credit and provenance for the whole lineage — Neavo, Tuna / mrcyclo, EnemyArea,
Davkas, Triggernometry, chromiumboy, gbdickinson and 情深旧忆往昔 — is recorded
in [`mod/NOTICE.md`](mod/NOTICE.md), which ships with the mod.

## How the seal works

Vanilla `Door.OnSpawn` unconditionally bypasses structure temperature for any
non-`Internal` door. That lets vanilla `Door.SetSimState` fall through to
`SimMessages.Dig()`, which physically removes the door from its own cells
whenever it opens — the door really does become a hole in the sim.

So the seal is not a side effect of the building being solid. It is held by
explicitly re-applying, on every relevant transition:

```
sealed :  SetInsulation(cell, 0f) + SetCellProperties(cell, 7)
open   :  SetInsulation(cell, 1f) + ClearCellProperties(cell, 7)
```

Mask `7` is `GasImpermeable | LiquidImpermeable | SolidImpermeable`
(`Sim.Cell.Properties`). Cell properties are sticky, so any transition that
fails to re-apply them leaves the door wide open until the next one that does.

`Door.ControlState` is `{ Auto = 0, Opened = 1, Locked = 2 }` — the seal keys on
`Opened`, not on whether the door is physically animating open. That is the
"self-sealing" behaviour: in Auto the door animates open for a Duplicant and
still holds a perfect seal, verified with Duplicant traffic across a live
gas gradient.

### Opened is a deliberate vent

**`Opened` drops gas, liquid *and* solid impermeability.** A door left in Opened
is a genuine hole, and will equalise the atmospheres on either side — measured
at roughly a full room swap per 90 s. This is intended, and matches upstream: it
is how you vent a room on purpose.

One consequence worth knowing: vanilla `Door.OnLogicValueChanged` sets
`requestedState = flag ? ControlState.Opened : ControlState.Locked`. An
automation-wired door is therefore driven to **Opened** on a green signal, never
to Auto — so it vents while the wire is green and seals when it goes red. If you
want an automated door that stays sealed, wire it so green means *closed*, or
leave it on Auto and let Duplicant pathing drive it.

## Patches

| Patch | Target | Why |
| --- | --- | --- |
| `DoorSetSimStatePatch` | `Door.SetSimState` | Re-applies the seal after vanilla's `Dig` / `ReplaceAndDisplaceElement`. Prefix also repairs an invalid door temperature. |
| `DoorSetWorldStatePatch` | `Door.SetWorldState` | Covers the `updateSim: false` path that `RefreshControlState` uses. See below. |
| `DoorOnSpawnPatch` | `Door.OnSpawn` | Re-seals after load, so a saved door does not leak before its first state change. |
| `DoorCleanUpPatch` | `Door.OnCleanUp` | Clears mask 7 on deconstruct; vanilla only clears 12. |
| `StructureTemperatureGetPatch` | `StructureTemperatureComponents.OnGetTemperature` | Repairs a non-positive temperature read. |
| `DoorAnimPatch` / `DoorWorkableGetAnimPatch` | `Door` / `Workable.GetAnim` | Keeps the Duplicant work animation from going null. Scoped to a known prefab list rather than patching every door in the game. |
| `GameLeakSamplerPatch` | `Game.UnsafeSim200ms` | Drives the diagnostic leak sampler. No-op unless `DebugLogging` is on. |
| `DatabasePatches` / `TranslationPatch` | `Db.Initialize` | Strings, tech tree, build menu, `.po` localisation. |

### Why `SetWorldState` is patched

`Door.RefreshControlState()` ends with `SetWorldState(updateSim: false)`, which
deliberately skips `SetSimState`. Every control-state change goes through it —
the UI toggle, the logic wire, `ApplyRequestedControlState`, and both `Sealed`
enter/exit. Hooking `SetSimState` alone misses all of them, so the door keeps
whatever the *last* `SetSimState` left:

- **Auto/Locked → Opened**: stays sealed until the state machine reaches `open`. Harmless.
- **Opened → Auto/Locked**: stays **unsealed** until it reaches `closed`/`locked`. Gas leak.

That window is normally sub-second (`closeblocked` → `closedelay` 0.5 s →
`closing` → `closed`), but `closeblocked` has no `SetWorldState` entry at all —
so a Duplicant or critter standing in the doorway parks the door there and holds
it open to gas indefinitely.

Patching `SetWorldState` closes this, because it runs on both the `updateSim`
paths. Added in 0.4.2; the bug is present in every earlier version and in
mrcyclo's upstream mod, which shares the same three patch points.

## Options

Configured in-game through the mod options screen (PLib), stored at
`Documents\Klei\OxygenNotIncluded\mods\config\FastInsulatedSelfSealingAirLock\`.

| Option | Default | Notes |
| --- | --- | --- |
| `Multiplier` | 5 (1–20) | Door animation speed, powered and unpowered. Requires a restart. |
| `DebugLogging` | `false` | Writes `FISSAC.log` next to the game's `Player.log`. |
| `VerboseCellLogging` | `false` | Adds a full per-cell dump to every sample. Thousands of lines a minute. Requires `DebugLogging`. |

The log is opened with `append: false`, so **each launch overwrites it** — copy
it off before relaunching.

## Diagnosing a suspected leak

With `DebugLogging` on, every door emits one `[Leak]` line every 5 s:

```
[Leak] id=-553570 prefab=... state=Auto isOpen=True pos=(65.5, 119.0, -30.5)
       axis=LR sealed=Y doorGas=0.000 doorLiq=0.000
       A=Oxygen:0.775 B=Oxygen:1.421 dA=+0.068 dB=-0.128 xfer=none verdict=ok
```

`grep verdict=BREACH` is the whole triage step.

- **`doorGas` / `doorLiq`** are dispositive. Gas cannot cross without occupying
  the door's own cells, so while sealed these must read exactly `0`. A sealed
  door with `doorGas > 0` is a real breach; anything else is room-level gas
  movement that happens to be next to a door.
- **`axis`** is `LR` for an upright door, `UD` for one rotated flat — `A`/`B`
  are the two open faces.
- **`dA` / `dB`** are cumulative gas-mass change per face since the door's first
  sample.
- **`xfer`** names any species that started out exclusive to one face and has
  since appeared on the other.
- **`verdict`** is `ok`, `BREACH`, or `vent(Opened)`.

**Reading the element labels.** ONI stores one element and one mass per cell,
with no mixed cells. At any gas interface a cell flips its label between (say)
Oxygen and CarbonDioxide as the dominant gas changes, and the reported mass
jumps because you are looking at a different gas packet — not the same one
gaining or losing mass. A label that oscillates and returns while `dA`/`dB` stay
flat is that artefact. A real leak is sustained and directional, and shows up in
`doorGas` first.

Only turn on `VerboseCellLogging` after a `[Leak]` line has pointed at a
specific door.

## Source provenance

The original source for this mod was lost. This project was **reconstituted by
decompiling the shipped 0.4.1 DLL** with `ilspycmd`, referencing the game's
`Managed` folder so enums and types resolved to real names rather than integer
casts. The recovered source was verified against
[mrcyclo/ONIInsulatedSelfSealingAirLock](https://github.com/mrcyclo/ONIInsulatedSelfSealingAirLock)
(the upstream this lineage descends from) — the seal logic is semantically
identical, confirming the decompile is faithful.

`Multiplier` and the option file format are unchanged from 0.4.1, so existing
player configs keep working. The building prefab ID remains
`FastInsulatedSelfSealingAirLock` for save compatibility.

## Building

From the monorepo root, after the one-time `GamePath.local.props` setup
described in the [root README](../README.md):

```sh
dotnet build ONIMods.sln
```

The build merges PLib into the mod assembly with ILRepack (PLib is designed to
be merged, not shipped alongside), stages a ready-to-install folder at
`dist/FastInsulatedSelfSealingAirLock/`, and deploys to the game's
`mods\Local\` folder.

> **Testing note:** the Workshop copy (Steam item `3755915137`) declares the same
> `staticID`. Keep it unsubscribed or disabled while testing the local build, or
> the loader has two mods claiming one ID.
