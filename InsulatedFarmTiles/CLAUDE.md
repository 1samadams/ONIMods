# Project: ONI "Insulated Farm Tiles Continued" Mod

## Goal
Two buildings: an **Insulated Farm Tile** and an **Insulated Hydroponic Farm
Tile**. Each is the vanilla farm tile with an Insulated Tile's thermal behaviour,
so a grow room can be sealed and temperature-controlled while still being fed
from outside.

**Scope rule: mirror vanilla, deviate only on purpose.** These configs are
line-for-line copies of `FarmTileConfig` and `HydroponicFarmConfig` apart from
three things — the anim, the insulation, and TIER3 raw minerals instead of TIER2
farmable materials. Every other historical difference turned out to be a 2019
config drifting behind six years of Klei changes, not a design decision. If a
future change makes one of these diverge from its vanilla counterpart, that
divergence needs a reason written down here.

## VERIFY BEFORE WRITING CODE — do not trust training memory
Same rule as the rest of the repo. Everything below was read out of the
**current** `Assembly-CSharp.dll`, not remembered.

- Game managed DLLs: `C:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed\`
- Element data is **not** in the assembly — `thermalConductivity`,
  `materialCategory` and the tag list live in
  `OxygenNotIncluded_Data\StreamingAssets\elements\solid.yaml`. That is the only
  place to check what a material list actually resolves to.
- **Use the PowerShell tool for `dotnet`**, not the Bash tool.
- Baseline game build: **`744825`**. Every claim in this file was verified
  against it.

## Lineage
Bokonon-ONI (2019, MIT) → erotel / Martin Zatloukal, "Fixed" (2026, MIT) → this.
Credits, licence position and the standing takedown offer are in
[`mod/NOTICE.md`](mod/NOTICE.md), which ships with the mod. The animation assets
are Bokonon's, unmodified.

Both upstreams are worth keeping to hand:
- https://github.com/Bokonon-ONI/ONI-Mods (`src/InsulatedFarmTile/`)
- https://github.com/erotel/InsulatedFarmTilesFixed

erotel's fork was a three-line change to Bokonon's code plus one new component.
Everything else in it — and therefore everything fixed below — was 2019 code.

## Insulation: the whole mechanism, and why the default changed

`SimMessages.SetInsulation(cell, value)` hands the sim a per-cell factor, and the
effective conductivity of that cell is `material.thermalConductivity * value`.
Vanilla `Insulator.OnSpawn` supplies `value` from `BuildingDef.ThermalConductivity`
and `Insulator.OnCleanUp` restores `1f`. `SimCellOccupier` with
`doReplaceElement = true` has already made the cell's element the build material,
which is why the material is in the product at all.

That multiplication is the bug the whole lineage is about:

| | `ThermalConductivity` | Sandstone result |
| --- | ---: | ---: |
| Vanilla Insulated Tile | 0.01 | 0.029 |
| Bokonon original | 1/32 = 0.031 | 0.091 |
| erotel fork | 1/32, then overridden per material | 0.010 |
| **This mod, default** | **0.01** | **0.029** |

Bokonon's `1/32` is the value insulated *pipes* use, not insulated tiles — a
player working this out from the code is what the workshop bug report was. erotel
fixed it by removing the material from the equation entirely; this mod fixes it
by using the number the vanilla Insulated Tile uses, and keeps erotel's mode
behind `config.json`. See the README for the player-facing version.

### Buildable materials resolve through a tag, not a category
`MATERIALS.RAW_MINERALS` is the single string `"BuildableRaw"`. Both a
`materialCategory` and a `tags` entry satisfy it, which is why **Insulation
(`SuperInsulator`) is buildable here** despite being `materialCategory:
ManufacturedMaterial` — it carries `BuildableRaw` as a tag. Abyssalite
(`Katairite`) is `materialCategory: Other` with no such tag and is **not**
buildable, in these tiles or in a vanilla Insulated Tile.

Full buildable set and conductivities: Insulation 0.00001, Ceramic 0.62, Mafic
Rock 1.0, Coquina 1.5, Shale 1.8, Fossil / Igneous / Obsidian / Sedimentary /
Siltstone 2.0, Basalt 2.5, Sandstone 2.9, Granite 3.39, Graphite 8.0.

### `FarmTileInsulation` replaces `Insulator`, it does not race it
erotel's fork put **both** components on the prefab and relied on a comment that
reads "added last, so its OnSpawn is last". `KMonoBehaviour.Spawn()` is called
from Unity's `Start()`, and Unity does not define `Start()` ordering across
components of one GameObject. Losing that race would leave the tile on the def's
own conductivity with no visible symptom at all.

`Settings` is loaded in `OnLoad`, long before `ConfigureBuildingTemplate` runs,
so the choice can be made at prefab-configuration time instead:
`TileInsulation.AddComponentTo` adds exactly one of the two. `FarmTileInsulation`
therefore owns the `OnCleanUp` reset to `1f` as well — without it a
material-independent tile would keep insulating its cell after deconstruction.

## Fixes that are just "catch up with vanilla"
Each of these was absent from the 2019 original, and each is a visible defect
rather than a balance choice. They are listed because the temptation to
"simplify" them away is real.

| What | Why it matters |
| --- | --- |
| `plantablePlot.AddAdditionalCriteria(FarmTileConfig.ForbiddenTags)` | Without it, seeds tagged `LargeSeed` (Wide Farm Tile crops) and `BackwallSeed` (Large Backwall Farm crops) can be planted in a 1×1 plot. Vanilla forbids this in **both** farm tiles. |
| The same call again in `prefabInitFn` | Vanilla does it twice on purpose — once on the template, once per instance. Only the instance copy gates what a Duplicant can plant. |
| `ModUtil.AddBuildingToPlanScreen(cat, id, "farming", anchor)` | The two-argument overload defaults the subcategory to `"uncategorized"`, which is what put the original's tiles in a stray group at the bottom of the Food tab. Subcategories postdate the original mod. |
| `initialBlendParameters = 4` (farm tile) | `4` is `KBatchedAnimInstanceData.BlendActiveOptions.WaterProof`. Stops a submerged tile being tinted by the liquid it sits in. Vanilla `FarmTileConfig` sets the same literal. |
| `SetBlendValue(LiquidVisibilityLayer, false)` + `(WaterProof, true)` (hydroponic) | Same idea; the hydroponic tile is full of piped water by design and opts out of the liquid layer. |
| `GameTags.CodexCategories.FarmBuilding` | Codex farm-building list. |
| `AddSearchTerms` | Build-menu search finds them. `FOOD` and `FARM` are what the vanilla farm tiles use; `TILE` is added on top because these are also insulated tiles and vanilla `InsulationTileConfig` uses it. That third term is the one deliberate deviation here. |
| `simCellOccupier.notifyOnMelt = true` (hydroponic) | Vanilla sets it; the original set it on one tile and not the other. |
| `construction_time = 30f` | erotel's fix, kept. `Deconstructable` derives its work time from `Def.ConstructionTime * 0.5f`, so the original's `300f` slowed teardown as well as building. |

Dropped rather than carried forward:
- `buildingDef.isSolidTile` — the field is `[Obsolete]` and the game reads it
  nowhere. The original set it inconsistently between the two tiles.
- The bundled BokLib (`BokModInfo`, `LogTools`, `BuildingTools`) — ~90 lines,
  most of it a commented-out dump of `TECH_GROUPING`, reachable only through the
  dead entry point below.
- `InsulatedFarmTilesPatches.OnModLoad.OnLoad()` — **nothing has ever called
  this.** `KMod.DLLLoader` finds one `UserMod2` per assembly and invokes
  `UserMod2.OnLoad(Harmony)`; there is no nested-class hook. The mod's version
  banner never printed.

## Registration — the ordering trap
`RegisterBuildingsPatch` is a **prefix on
`GeneratedBuildings.LoadGeneratedBuildings`**, and it must stay one. The original
used a postfix on `Db.Initialize`, which is a coin flip:

```
BuildingConfigManager.RegisterBuilding
  -> BuildingDef.PostProcess
       -> Db.Get().TechItems.AddTechItem(PrefabID, ...)
            -> returns NULL if no tech lists this ID yet
```

A building whose ID is not on a tech *at the moment it registers* never gets a
`TechItem` and never appears in the research screen. `Db.Get()` is a lazy
singleton that `BuildingDef.PostProcess` can itself trigger, so a `Db.Initialize`
postfix can land in the middle of the registration sweep. The prefix is ordered
by construction. (This is the same trap documented in `Lumen/CLAUDE.md`; it bites
every mod that adds a building.)

`Techs.PostProcess` — which resolves `unlockedItemIDs` into `unlockedItems` — runs
from `Db.PostProcess()`, separately and later, so adding IDs during the prefix is
in time.

The plan screen is safe in the same prefix: `LoadGeneratedBuildings` prunes plan
entries whose `BuildingDef` is null, but only *after* its `RegisterBuilding`
loop, by which point these defs exist.

Constants behind it, all verified in `TUNING.BUILDINGS`:
- Vanilla `FarmTile` and `HydroponicFarm` are plan category `Food`, subcategory
  `farming` (`PLANSUBCATEGORYSORTING`). Passing each as the anchor puts the
  insulated version directly after its vanilla twin.
- Tech `FinerDining` displays as **Gourmet Meal Preparation**.
- `CONSTRUCTION_MASS_KG.TIER3` is 200 kg.

## No save-persisted state
No component this mod adds has `[Serialize]` fields. Cell insulation is a sim
value re-applied on every spawn and reset on every cleanup, so nothing survives
removing the mod except the buildings themselves — which is normal for any mod
that adds buildings.

## Repo layout
```
mod/mod.yaml, mod_info.yaml, config.json, NOTICE.md
mod/anim/assets/...                                Bokonon's kanims, unmodified
src/InsulatedFarmTiles/InsulatedFarmTilesMod.cs    UserMod2 entry; load order matters
src/InsulatedFarmTiles/Settings.cs                 config.json
src/InsulatedFarmTiles/ModStrings.cs               string table registration
src/InsulatedFarmTiles/FarmTileInsulation.cs       material-independent mode only
src/InsulatedFarmTiles/Buildings/TileInsulation.cs the one place that picks a mode
src/InsulatedFarmTiles/Buildings/InsulatedFarmTileConfig.cs
src/InsulatedFarmTiles/Buildings/InsulatedHydroponicFarmConfig.cs
src/InsulatedFarmTiles/Patches/RegisterBuildingsPatch.cs   tech + build menu
```

`using STRINGS` and `using TUNING` both define a `BUILDINGS`, so
`TUNING.BUILDINGS.*` has to be written out in the configs. Vanilla does the same.

## Build & deploy
```
dotnet build src/InsulatedFarmTiles/InsulatedFarmTiles.csproj -c Release
```
Stages to `dist/InsulatedFarmTiles/`, then tries to copy into the game's Local
mods folder. The deploy is `ContinueOnError` because Windows Controlled Folder
Access guards the real Documents folder; see `AutoMachines/CLAUDE.md` for the
full explanation. `config.json` is copied only when absent, so player edits
survive a rebuild.

On this machine the deploy currently **succeeds**, to
`C:\Users\1sama\OneDrive\Documents\Klei\OxygenNotIncluded\mods\Local\InsulatedFarmTiles`.

### Workshop packaging
`anim/` is scanned content, so `StageMod` copies the whole `mod/` tree
recursively rather than naming files. A package without it registers buildings
whose kanim is missing, and `BuildingTemplates.CreateBuildingDef` does an
unchecked `Assets.GetAnim` — `AnimFiles` becomes `[null]`, which passes
`BuildingLoader.Add2DComponents`' `Length != 0` check and then throws inside
`KAnimControllerBase.set_AnimFiles`, aborting `RegisterBuilding` for both tiles.

Upload `dist/InsulatedFarmTiles/`, never `mod/`. Full loader rules in
`AutoMachines/CLAUDE.md`.

## Status
**Compiles clean, zero warnings, staged and deployed. NOT yet play-tested.**
Every claim above is verified against the assembly or the element data; none of
it is verified in game. The checklist below is what would establish that.

## Testing checklist (user runs the game; ask them to report)
1. Game launches with the mod enabled, no crash, listed in the Mods menu.
2. Both tiles appear under **Food → Farming**, directly after Farm Tile and
   Hydroponic Farm, correctly named — not `MISSING.STRINGS...` and **not** in a
   separate "Uncategorized" group. A stray group means the subcategory argument
   regressed.
3. Both appear on the **Gourmet Meal Preparation** research node. Present in the
   build menu but absent from research means the registration ordering trap has
   regressed.
4. Build one of each from Sandstone. The info panel's thermal conductivity should
   read the same as a vanilla Insulated Tile built from Sandstone.
5. Seal a room with them, run a heat source outside, confirm the inside holds —
   and that a Sandstone tile and a Ceramic tile visibly differ. Identical results
   from both materials means the default mode is not in effect.
6. Plant something ordinary (Mealwood, Bristle Blossom) — works. Try a crop that
   needs a Wide Farm Tile or Large Backwall Farm — must be **refused**. Being
   allowed means the `ForbiddenTags` criteria regressed.
7. Flood a tile of each type; neither should be tinted as though submerged.
8. Deconstruct one and confirm the cell stops insulating — heat crosses it again.
   This is the `OnCleanUp` reset.
9. Build and deconstruct timing feels like a vanilla farm tile, not 10× slower.
10. Set `"MaterialIndependentInsulation": true`, restart, rebuild from Sandstone
    and confirm it now insulates *better* than a vanilla Insulated Tile.
11. Save/load cycle; tiles come back with insulation intact and plants alive.
12. `Player.log` clean of Harmony errors and MISSING.STRINGS errors.
13. If any save used Bokonon's or erotel's version: enable this one with that one
    disabled and confirm existing tiles load rather than vanishing.
