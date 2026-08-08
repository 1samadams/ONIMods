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

## Art (1 of 2) — choosing a kanim, and the landmines
No custom `.kanim` pipeline; the fixtures reuse Klei's. This section is about
*which* anim is safe to reuse. For how five buildings sharing two anims are told
apart, see **Art (2 of 2)** further down.

- Only kanims driven by `LightController` are usable, because `LightController`
  plays the literal anim names `"on"` and `"off"`. **Not** `mercurylight_kanim` —
  the Mercury Ceiling Light has its own `MercuryLight` state machine with
  different anim names.
- **Only base-game anims.** Verified safe: `ceilinglight_kanim`,
  `floorlamp_kanim`, `sun_lamp_kanim` — their configs have no
  `GetRequiredDlcIds` override. **Never** `glassceilinglight_jelly_green_kanim`:
  `GlassCeilingLightConfig.GetRequiredDlcIds()` returns `DlcManager.DLC5`, so on
  an install without DLC5 the anim is simply not loaded. This shipped as a bug
  and killed the Panel Light outright — see below.

### The missing-anim failure mode (hit in play-testing, now guarded)
`BuildingTemplates.CreateBuildingDef` does an unchecked
`AnimFiles = new KAnimFile[1] { Assets.GetAnim(anim) }`. When the anim is absent
`GetAnim` returns null, so `AnimFiles` is `[null]` — which is **not** empty, so
`BuildingLoader.Add2DComponents` passes its `Length != 0` check and then throws
inside `KAnimControllerBase.set_AnimFiles`. That aborts `RegisterBuilding` for
the whole building, and the log reads:

```
First anim file needs to be non-null. LumenPanelLightComplete
Exception in RegisterBuilding for type Lumen.LumenPanelLightConfig from Lumen
  System.NullReferenceException at KAnimControllerBase.set_AnimFiles
```

`LumenLightConfig.ResolveAnim` now checks `Assets.GetAnim` first and falls back
to `LumenLights.FallbackAnim`, so a bad anim name costs one wrong-looking fixture
instead of a missing building. Keep that guard.

## No save-persisted state
Same constraint as AutoMachines. No component this mod adds has any
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
src/Lumen/LumenOrientation.cs                Orientation -> the 3 Light2D aim fields
src/Lumen/LumenCompat.cs                     is a rotation mod making cones aimable?
src/Lumen/Buildings/LumenLightConfig.cs      one shared IBuildingConfig
src/Lumen/Buildings/LumenBuildingConfigs.cs  the five concrete configs
src/Lumen/Components/LumenMotionSensor.cs    the sensor
src/Lumen/Components/LumenAimedLight.cs      applies aim on spawn and on rotate
src/Lumen/Components/LumenAppearance.cs      tint, lens tint, size
src/Lumen/Components/MinionPositions.cs      shared per-frame duplicant snapshot
src/Lumen/Patches/RegisterLightsPatch.cs     tech + build menu
src/Lumen/Patches/RotationPatches.cs         keep beam + preview aimed with sprite
```

`LumenLightConfig` is abstract on purpose: `LoadGeneratedBuildings` enumerates
every **non-abstract** `IBuildingConfig` and calls `Activator.CreateInstance`, so
the base is skipped and only the five concrete subclasses register. Adding a
sixth light means one entry in `LumenLights.All` plus a four-line subclass.

### Detection = the lit-cell set, NOT a radius (regression guard)
The sensor asks `DiscreteShadowCaster.GetVisibleCells` which cells the fixture
would light, caches them, and triggers when a duplicant's cell is in that set.
Do not "simplify" this back to a distance check.

v1 did use a distance check, with `SensorRadius` defaulted to the light's
`Range`. That is wrong in **both** directions, because the sensor described a
sphere while the light describes a downward cone. Play-testing showed exactly the
predicted split:

| Fixture | v1 radius | v1 behaviour |
|---|---|---|
| Spotlight | 4 | never triggered — sphere never reached the floor below a ceiling mount |
| Panel | 8 | triggered constantly — caught duplicants sideways and through walls that it never lit |
| Floodlight | 12 | correct by luck |
| Sentry | 16 | correct by luck |

Using the shadow caster makes the trigger condition exactly "this duplicant would
be lit", which needs no per-building tuning and, usefully, is the *same* test the
work speed bonus uses: `Workable` reads `Grid.LightIntensity` at
`Grid.PosToCell(worker.gameObject)`, the duplicant's feet cell — the cell this
checks. So a lit worker was necessarily already switched on.

There is no chicken-and-egg: `GetVisibleCells` is a pure query and works fine
while the light is off, which is the state the sensor has to decide out of.

The cache refreshes every `LitCellRefreshSeconds` (5 s) rather than hooking the
solid-change partitioner. Only building or digging a wall can invalidate it, so
the lazy refresh is a deliberate simplicity/latency trade. If a wall change needs
to register instantly, subscribe to `GameScenePartitioner.Instance.solidChangedLayer`
the way `Light2D.AddToScenePartitioner` does.

`ExtraSensorRadius` is the *only* remaining distance test, is 0 for everything
but the Sentry, and is additive on top of the lit-cell set. It was renamed from
`SensorRadius` on purpose so old config files fall back to defaults instead of
silently reinstating the broken behaviour — Newtonsoft drops unknown fields.

## Known limitations / deliberate trade-offs
1. **`ExtraSensorRadius` ignores walls.** Only the Sentry uses it, and that is the
   point: it is an early warning for someone approaching from outside the beam.
   The baseline lit-cell test *is* wall-aware.
2. **No status item** explaining *why* an unlit fixture is non-operational. The
   dark animation is the only feedback. A custom `StatusItem` would need its own
   string registration; deferred.
3. **Disabling a light in `config.json` still registers the prefab.**
   `LoadGeneratedBuildings` sweeps the assembly for config types and there is no
   supported opt-out. It is made unreachable three ways instead, the first being
   the game's own mechanism for this: `BuildingDef.Deprecated = true` (which tags
   the prefab `GameTags.DeprecatedContent` and makes `BuildingDef.PostProcess`
   skip `AddTechItem`), plus no plan category and no tech entry.
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

### Workshop packaging — verified correct
Checked against `KMod.Mod` at build `744825` after Auto Machines shipped a
package the loader rejected with "No compatible mod found". **The full rules live
in the AutoMachines CLAUDE.md** under *Packaging for the Workshop*; they apply
verbatim. Lumen's result:

| check | value | verdict |
|---|---|---|
| DLL at the archive's top level | `dist/Lumen/Lumen.dll` | correct — the only thing that makes the mod scan as non-empty |
| `APIVersion` | `2` | correct; anything else with a DLL present is rejected as `OldAPI` |
| `supportedContent` | `ALL` | loads on vanilla **and** Spaced Out (maps to no DLC restriction either way) |
| `mod.yaml` | title, description, `staticID: ONI.Lumen` | correct |

**Upload `Lumen/dist/Lumen/`, never `Lumen/mod/`.** The latter is source metadata
with no DLL, and scans as empty — that is exactly how Auto Machines broke.

One wart: `dist/Lumen/` also contains `Lumen Thumbnail-selection.png` (886 KB),
which is not produced by `StageMod` and was dropped there by hand. The loader
ignores it — PNG is not scanned content — but it ships to every subscriber. It is
not needed in the mod folder; the Workshop thumbnail is supplied to the uploader
separately.

## Status
Compiles clean, zero warnings, deployed.

**Play-test 1 (build 744825, no DLC5):** Panel Light failed to register — DLC-gated
anim. Fixed and guarded.

**Play-test 2:** Floodlight, Sentry and Floor Lamp working. Spotlight never
triggered and Panel Light stayed lit too long — both the sphere-vs-cone mismatch.

**Play-test 3: all five confirmed working**, lighting the correct downward cone.
The lit-cell rewrite is verified in-game.

**Play-test 4:** confirmed the inverted preview disappears when Rotate Everything
is removed, which is what identified it as a mod interaction rather than stock
behaviour. Also confirmed the power overlay reads the fixture's *rating* while
dark -- that is `WattsNeededWhenActive`, not live draw; `WattsUsed` short-circuits
to `0f` when inactive, and the zero heat while dark corroborates it. Not a bug.

**Play-test 5: everything verified in-game.** Lens tinting, size differentiation,
4-direction rotation, the placement preview and the light itself all confirmed
correct (checklist 7, 11 and 12). The whole feature set is play-tested; nothing
currently ships unverified.

**Play-test 6:** every aimable fixture lit one tile low with its own cell dark,
while the preview was correct. Root-caused to `Light2D.Offset` moving the emitter
into the cell below — fixed and made unrepresentable; see the section below. Not
yet re-tested in game.

Remaining open items are choices, not defects: the four ceiling fixtures still
share a silhouette (limited by `ceilinglight_kanim` having one body part), and
building facades are assessed but deliberately not implemented -- both below.

### SOLVED: inverted placement preview, and how aiming works
Root cause, confirmed by decompiling `rotate_everything.dll` (Rotate Everything,
Jarodamus Prime, Workshop 1715709940 -- no public source; decompile the installed
binary under `mods/Steam/1715709940/`). It does two things:

1. Prefixes **`DiscreteShadowCaster.GetVisibleCells`** and, for `LightShape.Cone`,
   picks the octant pair from the `direction` argument instead of the stock
   hardcoded `S_SE`/`S_SW`. **This is global** -- it makes cones aimable for every
   light in the game, Lumen's included.
2. Only updates `LightShapePreview.direction` for prefabs whose name starts with
   **`"CeilingLight"`** (a `LightShapePreview.Update` prefix), and likewise only
   re-aims `Light2D` for those, via `Light2D.OnSpawn` and
   `Rotatable.SetOrientation` postfixes.

`LightShapePreview.direction` defaults to `Direction.North` -- enum value 0 -- and
**no vanilla config ever sets it**, because stock cones ignore direction. So once
(1) makes direction meaningful, every cone light that (2) does not whitelist
previews as a cone aimed straight up.

That explains the whole observed pattern: vanilla Ceiling Light fine
(whitelisted), vanilla Sun Lamp broken, Lumen cones broken, Lumen **Floor Lamp
fine because it is a `Circle`** -- the patch returns early for non-cone shapes.

**Fix:** `DoPostConfigurePreview` sets `preview.direction = South` explicitly.
One line, no vanilla patching. Harmless when direction is ignored, correct when
it is not. Do not delete it as redundant -- it is only redundant in stock ONI.

### Aiming a light needs THREE fields to agree
`LumenOrientation` maps `Orientation` onto all three. Setting one alone gives a
light that looks right and lights the wrong tiles, or the reverse:

| Field | Type | Controls |
|---|---|---|
| `Light2D.LightDirection` | `DiscreteShadowCaster.Direction` | which **cells** are lit |
| `Light2D.Direction` | `Vector2` | which way the **glow** is drawn (`LightBuffer`) |
| `Light2D.Offset` | `Vector2` | the **origin cell** the beam is cast from, *and* where the glow is drawn from |

`Light2D.FullRefresh()` must follow, or the change sits unapplied in
`pending_emitter_state`.

### SOLVED: beam one tile low and a dark bulb — `Offset` is not cosmetic
Play-test 6 reported it on every aimable fixture: the placement preview started
the cone at the bulb, the built fixture started it one square lower and left its
own cell at zero lumens.

`Light2D.Offset`'s **setter** recomputes
`origin = Grid.PosToCell(transform.position + Offset)`, and `origin` is the cell
`DiscreteShadowCaster` casts the whole cone from. `LightBuffer.LateUpdate` also
adds `Offset` to the glow position, so the field does both jobs — an earlier
comment in `LumenOrientation` called it "purely cosmetic" and that false claim is
exactly what caused this.

Two facts make a downward offset fatal:

- A building's transform sits at the **bottom** of its footprint, not the centre.
  Every vanilla offset is positive-Y and proves it: `CEILINGLIGHT_OFFSET` +0.65 on
  a 1x1, `FLOORLAMP_OFFSET` +1.5 on a 1x2, `SUNLAMP_OFFSET` +3.5 on a 2x4.
- `Grid.PosToCell` is `(int)(pos.y + 0.05f)`, not a round-to-nearest.

`LumenOrientation.ToOffset` returned `(0.05, -0.35)` for South, so a fixture at
cell `(X, Y)` — transform `(X+0.5, Y)` — resolved to `(int)(Y - 0.30) = Y-1`.

**Fix:** `ToOffset` now takes the fixture's configured mounting point
(`LumenLight.Offset`, passed in via `LumenAimedLight.baseOffset`) plus the
building's world position, **leans** `AimOffset` along the glow vector from there,
and clamps the result into the box `PosToCell` maps to the mounting point's own
cell. Aiming can no longer move the origin cell at all, whatever `AimOffset` is
set to. Resulting offsets for a ceiling fixture, all resolving to cell `(X, Y)`:

| Aim | Offset | `PosToCell` y |
|---|---|---|
| South | `(0.05, 0.30)` | `(int)(Y+0.35)` = Y |
| North | `(0.05, 0.90)` | `(int)(Y+0.95)` = Y |
| East | `(0.40, 0.65)` | `(int)(Y+0.70)` = Y |
| West | `(-0.30, 0.65)` | `(int)(Y+0.70)` = Y |

The Floor Lamp was never affected: it is a `Circle`, so it is not aimable, never
gets a `LumenAimedLight`, and keeps vanilla's `(0.05, 1.5)`.

The sensor needed no change — `RebuildLitCells` derives its origin from
`light2D.Offset` the same way `Light2D` does, so the trigger area was wrong in
exactly the same way and is now right for the same reason.

**Lesson:** the preview and the fixture reach the origin cell by different
routes — `LightShapePreview` does `Grid.OffsetCell(PosToCell(pos), offset)` in
whole cells, `Light2D` does `Grid.PosToCell(pos + Offset)` in metres. They agreed
on the preview path and disagreed on the built path, which is what made this look
like a rendering bug. `DoPostConfigurePreview` now derives its `CellOffset` with
`PosToCell`'s own `(int)(y + 0.05f)` so the two agree by construction.

### Rotation is gated on a third-party mod, deliberately
`LumenCompat.ConesAreDirectional` checks whether the `rotate_everything` assembly
is loaded. Only then do aimable fixtures get `PermittedRotations.R360` and a
`LumenAimedLight`. Without it a rotatable cone would spin its sprite while the
beam stayed on the floor -- worse than not offering rotation at all. Detection is
lazy and cached: mod load order is not guaranteed, and this is first read during
building registration, long after every mod assembly has loaded.

If that mod is ever abandoned, the self-contained alternative is
`LightShape.Quad`, which honours `LightDirection` in **stock** ONI via `ScanQuad`
(the Mercury Ceiling Light is the vanilla reference). Quad throws a rectangle
rather than a cone.

Both rotation patches are scoped so they cannot affect other mods' lights:
`Rotatable.SetOrientation` by **component presence** (`LumenAimedLight`), and
`LightShapePreview.Update` by prefab-name prefix, since the preview prefab is
named `<ID>Preview` and carries none of our components.

### Lesson: a vanilla building is not a control when mods patch globally
An earlier conclusion in this file -- that the flipped preview was stock Klei
behaviour -- was wrong, and cost several rounds of hunting through Klei's
renderer for a bug that was never there. The Sun Lamp reproducing it proved only
that the flip was not *Lumen-specific*. Rule out third-party patches, by
decompiling them, before concluding anything is stock. The installed DLLs are
right there under `mods/Steam/`.

## Art (2 of 2) — differentiating fixtures within one kanim
`LumenAppearance` (was `LumenTint`) applies three things at spawn:

- `KBatchedAnimController.TintColour` — the housing. All four ceiling fixtures
  share one neutral steel value, so they read as a single product line.
- `SetSymbolTint` over `LensSymbols` — the lens, which is what actually
  distinguishes them. Symbol tints live on the controller instance rather than on
  an animation, so they survive the on/off anim switch and apply once. Naming a
  symbol a build lacks is a silent no-op.
- `animScale` multiplier — size. Deliberately tracks `Range` (4 → 0.85, 8 → 1.0,
  12 → 1.2) so size is information, not decoration. `animScale` is read inside
  `GetTransformMatrix()` every render, so assigning at spawn is enough.

All must be applied at **spawn**: `TintColour` writes through to
`batchInstanceData`, which does not exist on the inactive prefab template.

Still colour-only between **Panel and Sentry** — same kanim, same range, so same
size. That is honest rather than fixed with an invented size difference; they
differ by lens colour, sensor reach and linger.

**Symbol names (dumped at runtime; NOT readable by decompiling).** These are the
only two kanims the mod uses:

| kanim | symbols |
|---|---|
| `ceilinglight_kanim` (5) | `generator_light_bloom`, `light_off`, `place`, `temp_base`, `ui` |
| `floorlamp_kanim` (9) | `ui`, `beam`, `cord`, `place`, `pole`, `shade`, `feet`, `handle`, `light` |

Read those carefully before planning art work, because they set hard limits:

- **`ceilinglight_kanim` has exactly ONE body part**, `temp_base` — and the name
  says what it is, placeholder art Klei never replaced. The rest is a bloom, an
  off-state sprite, the placement ghost and the build-menu icon. So
  `SetSymbolVisiblity` cannot produce silhouette variety on the ceiling
  fixtures: hiding `temp_base` leaves a floating glow, hiding the bloom kills the
  light. **Do not plan a "hide parts" scheme for the ceiling lights.**
- **`floorlamp_kanim` is genuinely composable** — `pole`, `shade`, `feet`,
  `cord`, `handle`, `beam`, `light` are seven separable parts. Any future
  part-hiding or part-swapping work should be built on this kanim, not the
  ceiling one.

What was done with them: `LumenAppearance` tints the housing via `TintColour` and
the lens via `SetSymbolTint` on `LensSymbols`. All four ceiling fixtures now share
one neutral steel housing and differ by lens colour, so they read as one product
line rather than as the same lamp dyed four ways. Symbol tints live on the
controller instance, not on an animation, so they survive the on/off anim switch
and only need applying once. Naming a symbol a build lacks is a silent no-op.

### Open: silhouette variety, and the facade question
Four of five fixtures share `ceilinglight_kanim`. They are now differentiated by
housing colour, lens colour and `animScale`, which is as far as that kanim goes —
see the symbol table above for why (**one** body part). Panel and Sentry share a
size and differ only by lens colour. This is a look-and-feel limit, not a defect.

Remaining options, in increasing effort:

1. `SetSymbolOverride(...)` — graft a symbol from a *different* build onto this
   one. The most powerful reuse option and the fiddliest. Untried.
2. Build a variant on `floorlamp_kanim`, which has seven separable parts
   (`pole`, `shade`, `feet`, `cord`, `handle`, `beam`, `light`). Possibly flipped
   to read as a hanging pendant; would need offset tuning. Untried.
3. `sun_lamp_kanim` — a genuinely different base-game silhouette, but 2x4 and
   floor-mounted, so it changes that building's identity.
4. Real custom kanims: Spriter + a kanim packer + `ModUtil.AddKAnim`. The only
   route to art that is actually new.

#### Building facades — assessed, deliberately NOT done
Asked whether Lumen fixtures should accept blueprint facades like vanilla lights.
The mechanism is real and public: `Db.Get().BuildingFacades.Add(id, name, desc,
rarity, prefabId, animFile, ...)`, and a facade is literally *a prefab ID plus an
anim file*. `BuildingFacade` is already added to preview objects by
`BuildingLoader.CreateBuildingPreview`. Facades are data-driven from
`Blueprints.Get().all.buildingFacades`, so they cannot be enumerated by
decompiling — it needs a runtime dump, same trick as the symbol names.

Not started, for three reasons:

1. **Facades do not solve the art problem.** They are a chooser UI for art you
   already have. Worth doing only if Klei authored several *light* facades whose
   anim files we could point at — that would be the best art option available,
   better than custom kanims, since it is Klei-drawn and already fits. Unknown
   until someone dumps the facade list. **That check gates everything else.**
2. **Permits are probably fatal.** Facades unlock through the Printing Pod and
   achievements. Registering our own facade IDs creates permits that are in no
   drop table, so on a normal save they may be permanently unobtainable — while
   appearing to work fine for anyone running Unlock All Blueprints. A feature
   that works only for the author is worse than no feature.
3. **It would partly break the lens tinting.** A facade swaps the anim file, and
   `generator_light_bloom` / `temp_base` almost certainly do not exist in it.
   Missing symbols are a silent no-op, so fixtures would quietly lose their lens
   colour and fall back to the housing tint.

Also worth weighing: facades and permits are likelier to shift between game
updates than anything this mod currently touches.

Both temporary diagnostics -- the `[Lumen] geometry` spawn logging and the
`[Lumen] symbols` kanim dump -- have been removed now that their results are
recorded above. The mod logs only on load, on a detected rotation mod, and on
genuine problems.

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
7. The five look visually distinct: neutral steel housings, differently coloured
   lenses, and the Spotlight visibly smaller / Floodlight visibly larger. All
   untinted means `LumenAppearance` is running too early; correct housings but
   no lens colour means the symbol names in `LensSymbols` are wrong.
8. Save/load cycle works; the lights come back dark and re-trigger correctly.
9. Set `"Enabled": false` for one light → it disappears from the build menu and
   the research node, and the rest still work.
10. `Player.log` clean of Harmony errors and MISSING/STRINGS errors.
11. With a rotation mod present: rotate a cone fixture sideways and confirm the
    beam, the glow and the *trigger area* all follow -- a Duplicant standing in
    the new beam must switch it on. That last part is the sensor-follows-beam
    path and the least certain.
12. The placement preview cone points the same way the fixture will. A vanilla
    Sun Lamp previewing upward is expected and is not this mod.
13. Performance sanity: build ~50 of them and confirm no frame-time regression.
    If there is one, the suspect is light grid churn from `SetFlag` edges, not
    the distance scan — raise `LingerSeconds`.
