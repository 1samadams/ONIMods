# CLAUDE.md

Guidance for Claude Code (or other AI agents) working in this repository.

## What this is

One mod within the **ONIMods** monorepo, for *Oxygen Not Included* (ONI). The
mod uses Harmony to patch the game's blueprint `rarity` getters so every
Printing Pod blueprint is treated as `PermitRarity.Universal` — i.e. always
unlocked. See `README.md` for the player-facing explanation and install
instructions.

Layout (normalized to match the other mods in the monorepo): sources in
`src/UnlockAllBlueprints/`, mod assets (`mod.yaml`, `mod_info.yaml`) in `mod/`.
The two are combined by the build's `StageMod` target into
`dist/UnlockAllBlueprints/`, which is the folder Klei's local-mods loader
expects — DLL and yamls side by side. This mod used to live flat at its own
repo root; if you move files again, update the layout table in `README.md`
and the `ModAssetsDir`/`StageDir` paths in the csproj.

## No game files in this repo

`Assembly-CSharp.dll`, `0Harmony.dll`, and the other Managed DLLs referenced
by this mod belong to Klei/Steam and are **not** checked into this repo, and
should never be committed here. They are resolved via `$(GameLibsDir)`, set in
the monorepo root's gitignored `GamePath.local.props` (see
`GamePath.local.props.example`), with the `ONI_INSTALL_DIR` environment
variable as a fallback. The references themselves live in the monorepo's
`Directory.Build.props`, not in this mod's csproj. This means:

- `dotnet build`/`dotnet restore` will fail in a sandbox or CI environment
  without a real ONI install available — that's expected, not a bug in the
  project file. Don't try to fix it by vendoring the DLLs.
- If a real install *is* present, build it. Where it isn't, reason about
  correctness by reading the patch code and cross-referencing known ONI
  class/member names (see below).

To check a type or member name against the installed game without a
decompiler, read `Assembly-CSharp.dll`'s metadata directly with
`System.Reflection.Metadata` (`PEReader` + `GetMetadataReader`). Do **not**
use `Assembly.ReflectionOnlyLoadFrom` — it silently drops types whose
dependencies can't be resolved, so members appear to be missing when they
are actually present.

## Class names are version-sensitive

ONI's internal class names occasionally change between game updates —
historically some blueprint-related types have carried an update-number
suffix (e.g. `Blueprints_U53`) that gets renamed each time Klei ships a
content update, which breaks patches targeting that specific class.

This mod deliberately avoids that fragility by patching the `rarity`
**property getter** on the stable info types instead. These getters are far
less likely to be renamed than one-off setup-routine classes.

The patched types are the implementations of the `IBlueprintInfo` interface:
`BuildingFacadeInfo`, `ArtableInfo`, `ClothingItemInfo`,
`BalloonArtistFacadeInfo`, `StickerBombFacadeInfo`, `EquippableFacadeInfo`,
and `MonumentPartInfo`. They live in the **global namespace**, not
`Database` — only the `PermitRarity` enum is `Database.PermitRarity`, which
is the sole reason `Patches.cs` has a `using Database;`.

Harmony cannot patch the `IBlueprintInfo.rarity` interface member, so each
implementation needs its own patch class. When checking coverage after a
game update, enumerate the types implementing `IBlueprintInfo` rather than
assuming the list above is still complete.

If a future game update does rename or restructure one of these info types:

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
- Leave `TargetFramework` at `net48`. The game's shipped `0Harmony.dll`
  targets 4.8; anything lower makes MSBuild silently *drop* every game
  reference (MSB3274/MSB3275) and the build then fails with a wall of
  misleading CS0246 "type or namespace not found" errors. The
  `Microsoft.NETFramework.ReferenceAssemblies` package is there so this
  builds without the 4.8 Developer Pack installed — don't remove it.
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

1. Build with a real `GameLibsDir` pointed at the game's Managed folder.
2. Copy `dist/UnlockAllBlueprints/` into the local mods directory (see `README.md`).
3. Start a new game and check the Printing Pod / base-select screen offers
   every blueprint rarity tier, not just previously-unlocked ones.
