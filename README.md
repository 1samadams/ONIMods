# Unlock All Blueprints (Oxygen Not Included)

A [Harmony](https://github.com/pardeike/Harmony)-based mod for *Oxygen Not
Included* that unlocks every Printing Pod blueprint — building facades,
artables, clothing items, balloon artist facades, sticker bombs, equippable
facades, and monument parts — regardless of Colony Achievement progress or
Klei account unlock status.

## How it works

Klei's blueprint system gates most blueprints behind a `rarity` tier.
Anything below `Universal` rarity is only offered if the game believes your
account (or local save) has unlocked it via Colony Achievements. This mod
patches the `rarity` getter on every implementation of `IBlueprintInfo`
(`BuildingFacadeInfo`, `ArtableInfo`, `ClothingItemInfo`,
`BalloonArtistFacadeInfo`, `StickerBombFacadeInfo`, `EquippableFacadeInfo`,
`MonumentPartInfo`) to always return `Database.PermitRarity.Universal`, the
tier the game always treats as unlocked. From the game's perspective every
blueprint is now a default, always-available one.

Patching the property getters (rather than one-time setup code) means the
override applies no matter which internal setup routine populates the
blueprint collection, and it survives across game updates that only touch
that setup routine.

## Caveats

- The effect is client-side and cosmetic to blueprint *availability* — it
  does not touch your actual Klei account unlock state. Uninstalling the
  mod reverts you to whatever you'd legitimately unlocked.
- If a Duplicant/base printed with an otherwise-locked blueprint is lost
  after uninstalling, it won't be offered again without the mod.
- Intended for single-player use. Behavior in multiplayer/shared saves with
  players who don't have the mod installed is untested.

## Installation

1. Build the mod (see below) or download a release build.
2. Copy the folder (containing `mod.yaml`, `mod_info.yaml`, and
   `UnlockAllBlueprints.dll`) into your local mods directory:
   - Windows: `%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\Local\UnlockAllBlueprints`
   - Linux: `~/.config/unity3d/Klei/Oxygen Not Included/mods/Local/UnlockAllBlueprints`
   - macOS: `~/Documents/Klei/OxygenNotIncluded/mods/Local/UnlockAllBlueprints`
3. Enable it from the in-game **Mods** menu and restart.

## Building

Requires the .NET SDK and a local Oxygen Not Included install (its
`Assembly-CSharp.dll` and `0Harmony.dll` are needed to compile against, and
are not distributed in this repo). The project targets .NET Framework 4.8 to
match the game's `0Harmony.dll`; the 4.8 reference assemblies come from a
NuGet package, so no Developer Pack install is required.

```bash
dotnet build -p:ONIInstallDir="/path/to/OxygenNotIncluded"
```

or set the `ONI_INSTALL_DIR` environment variable to your game install
directory before running `dotnet build`. The build copies
`UnlockAllBlueprints.dll` next to `mod.yaml` so the project root can be used
directly as the mod folder.

## Project layout

| File | Purpose |
| --- | --- |
| `mod.yaml` | Mod title/description shown in the in-game Mods menu |
| `mod_info.yaml` | API version and supported build metadata |
| `UnlockAllBlueprintsMod.cs` | `UserMod2` entry point that applies the Harmony patches |
| `Patches.cs` | The Harmony patches themselves |
| `UnlockAllBlueprints.csproj` | Build configuration |

See `CLAUDE.md` for notes aimed at AI coding agents working in this repo.
