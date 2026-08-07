# CLAUDE.md

Guidance for Claude Code (or other AI agents) working in this repository.

## What this is

A single-mod repo for *Oxygen Not Included* (ONI). The mod uses Harmony to
patch the game's blueprint `rarity` getters so every Printing Pod blueprint
(building facades, artables, clothing items, balloon artist facades) is
treated as `PermitRarity.Universal` — i.e. always unlocked. See `README.md`
for the player-facing explanation and install instructions.

The mod folder *is* the repo root: `mod.yaml`, `mod_info.yaml`, and the
built DLL sit side by side, matching how Klei's local-mods loader expects a
mod to be laid out. Don't move mod files into a subdirectory without also
updating install instructions in `README.md`.

## No game files in this repo

`Assembly-CSharp.dll`, `0Harmony.dll`, and the other Managed DLLs referenced
by `UnlockAllBlueprints.csproj` belong to Klei/Steam and are **not**
checked into this repo, and should never be committed here. The csproj
resolves them via `$(ONIInstallDir)` (an MSBuild property) or the
`ONI_INSTALL_DIR` environment variable, pointed at a local game install.
This means:

- `dotnet build`/`dotnet restore` will fail in a sandbox or CI environment
  without a real ONI install available — that's expected, not a bug in the
  project file. Don't try to fix it by vendoring the DLLs.
- You cannot fully compile or test changes in this environment. Reason
  about correctness by reading the patch code and cross-referencing known
  ONI class/member names (see below) rather than trusting a local build to
  catch mistakes.

## Class names are version-sensitive

ONI's internal class names occasionally change between game updates —
historically some blueprint-related types have carried an update-number
suffix (e.g. `Blueprints_U53`) that gets renamed each time Klei ships a
content update, which breaks patches targeting that specific class.

This mod deliberately avoids that fragility by patching the `rarity`
**property getter** on the stable info types instead
(`BuildingFacadeInfo.rarity`, `ArtableInfo.rarity`, `ClothingItemInfo.rarity`,
`BalloonArtistFacadeInfo.rarity`), all in the `Database` namespace. These
getters are far less likely to be renamed than one-off setup-routine
classes. If a future game update does rename or restructure one of these
info types:

1. Decompile the new `Assembly-CSharp.dll` (dotPeek/ILSpy/dnSpy) to find the
   new type/member name.
2. Update the corresponding `[HarmonyPatch(...)]` attribute in `Patches.cs`.
3. Bump `mod_info.yaml`'s `minimumSupportedBuild` and `version`.

## Making changes

- Keep patches as small, targeted Prefix/Postfix methods — this mod should
  stay a minimal, single-purpose override, not grow into a general-purpose
  blueprint editor.
- If you add a new blueprint category to unlock, follow the existing
  pattern in `Patches.cs`: a `[HarmonyPatch(typeof(XInfo), nameof(XInfo.rarity), MethodType.Getter)]`
  class with a `Prefix` that sets `__result = PermitRarity.Universal;` and
  returns `false`.
- `UnlockAllBlueprintsMod.cs` is the `UserMod2` entry point Klei's mod
  loader calls; it should stay a thin `harmony.PatchAll()` call rather than
  accumulating logic.
- After editing `mod_info.yaml`, keep `version` in sync with any change
  that affects behavior, so players/Steam Workshop can tell builds apart.

## Testing

There's no automated test suite (Harmony patches against a closed-source
Unity game aren't practically unit-testable). Verification is manual, in a
running copy of Oxygen Not Included, by an environment with the real game
installed:

1. Build with a real `ONIInstallDir` pointed at the game.
2. Copy the mod folder into the local mods directory (see `README.md`).
3. Start a new game and check the Printing Pod / base-select screen offers
   every blueprint rarity tier, not just previously-unlocked ones.
