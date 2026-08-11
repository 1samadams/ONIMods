# Insulated Farm Tiles Continued

Adds an **Insulated Farm Tile** and an **Insulated Hydroponic Farm Tile** to
[*Oxygen Not Included*](https://www.klei.com/games/oxygen-not-included) — a Farm
Tile and a Hydroponic Farm Tile that insulate exactly like an Insulated Tile.
Plants can still be watered and fertilised from outside, so a
temperature-controlled grow room can be sealed off and fed by pipe.

Both cost 200 kg of raw minerals, build at vanilla farm-tile speed, and unlock
with **Gourmet Meal Preparation**, next to their vanilla counterparts under
**Food → Farming**.

A community continuation of Bokonon's *Insulated Farm Tiles* by way of erotel's
*Insulated Farm Tiles (Fixed)*. Full lineage and credits in
[`mod/NOTICE.md`](mod/NOTICE.md).

> **Do not run this alongside either earlier version.** All three register the
> same building IDs and would clash. Tiles placed by either earlier mod load into
> this one, so switching is a matter of disabling the old and enabling this.

## Insulation

The sim's effective conductivity for a tile's cell is
`material.thermalConductivity × insulation`, and the game's `Insulator` component
supplies `insulation` from `BuildingDef.ThermalConductivity`. Vanilla
`InsulationTileConfig` puts `0.01` there; the 2019 original put `1/32` (`0.031`),
which is why players reported these tiles leaking.

By default this mod uses `0.01` — a like-for-like copy of the Insulated Tile, so
the familiar "build it from something better" progression still applies:

| Build material | Conductivity | Effective, in this tile |
| --- | ---: | ---: |
| Insulation | 0.00001 | 0.0000001 |
| Ceramic | 0.62 | 0.0062 |
| Mafic Rock | 1.0 | 0.010 |
| Igneous Rock, Sedimentary, Obsidian | 2.0 | 0.020 |
| Sandstone | 2.9 | 0.029 |
| Granite | 3.39 | 0.034 |
| Graphite | 8.0 | 0.080 |

### config.json

```json
{
  "MaterialIndependentInsulation": false,
  "TargetConductivity": 0.01
}
```

`MaterialIndependentInsulation: true` selects erotel's design instead: the build
material is divided back out, so the effective conductivity is
`TargetConductivity` whatever the tile is made of. That is stronger than a
vanilla Insulated Tile on every raw mineral — and, worth knowing before turning
it on, *weaker* on Ceramic (0.01 instead of 0.0062), because the mode can only
raise a poor material up to the target, never leave a good one alone. Insulation
is the exception: below the target already, it is left untouched at 0.00001.

`TargetConductivity` means the multiplier in the default mode and the resulting
effective conductivity in material-independent mode. At `0.01` both readings
coincide with the vanilla Insulated Tile, which is why it is the default for
both.

The file is read once at mod load. Edit it and restart the game.

## Building

Requires the repo-level setup described in the [monorepo
README](../README.md#building) — a `GamePath.local.props` pointing at your game's
`Managed` folder.

```sh
dotnet build InsulatedFarmTiles/src/InsulatedFarmTiles/InsulatedFarmTiles.csproj -c Release
```

Stages a ready-to-install folder at `dist/InsulatedFarmTiles/` and then tries to
copy it into the game's Local mods folder. If Controlled Folder Access blocks
that, the build still succeeds — copy the staged folder across by hand.

Upload `dist/InsulatedFarmTiles/` to the Workshop, never `mod/`: the latter has
no DLL and the loader would scan it as empty.

## Licence

MIT, inherited from Bokonon's original and erotel's fork. See
[`LICENSE`](LICENSE).
