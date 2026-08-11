# ONIMods

A monorepo of my mods for [*Oxygen Not Included*](https://www.klei.com/games/oxygen-not-included).

Each mod lives in its own top-level folder, keeping the full commit history of
the standalone repository it came from (merged with `git subtree`). One
solution, `ONIMods.sln`, builds them all.

## Mods

| Mod | Folder | What it does | Workshop |
| --- | --- | --- | --- |
| **Auto Machines** | [`AutoMachines/`](AutoMachines/) | Makes duplicant-operated fabricators run on their own once materials are delivered — 25 buildings across the base game and every DLC. Patches the existing buildings in place instead of cloning them, so recipes, outputs and masses stay exactly vanilla. Every building is individually toggleable from the in-game mod options screen. | [Subscribe](https://steamcommunity.com/sharedfiles/filedetails/?id=3779252781) |
| **Unlock All Blueprints** | [`UnlockAllBlueprints/`](UnlockAllBlueprints/) | Unlocks all Printing Pod blueprints — building facades, artables, clothing items, balloon artist facades, sticker bombs, equippable facades and monument parts — regardless of Colony Achievement or Klei account unlock status. | _local only — not published_ |
| **Lumen** | [`Lumen/`](Lumen/) | Adds five motion-activated light fixtures unlocked alongside the Duplicant Motion Sensor. Each draws 1 W and stays dark — and genuinely unpowered — until a Duplicant walks into range, so Duplicants still get the lit-workspace work speed bonus without lighting an empty base. The four cone fixtures aim in all four directions when *Rotate Everything* is installed. Vanilla lights are left untouched. Every fixture is tunable in `config.json`. | _local only — not published_ |
| **Fast Insulated Self Sealing AirLock** | [`FastInsulatedSelfSealingAirLock/`](FastInsulatedSelfSealingAirLock/) | A 1×2 manual pressure door that stays a perfect seal in the simulation while still animating open for Duplicants — gas, liquid and heat are all blocked unless the door is explicitly set to Opened. Door speed is configurable from 1× to 20×. A community continuation of Neavo's mod; see [`NOTICE.md`](FastInsulatedSelfSealingAirLock/mod/NOTICE.md) for the full lineage. | [Subscribe](https://steamcommunity.com/sharedfiles/filedetails/?id=3755915137) |
| **Insulated Farm Tiles Continued** | [`InsulatedFarmTiles/`](InsulatedFarmTiles/) | A Farm Tile and a Hydroponic Farm Tile that insulate exactly like a vanilla Insulated Tile, so a grow room can be sealed and temperature-controlled while still being watered and fertilised from outside. Insulation strength, and whether it scales with the build material, are set from the in-game mod options screen. A community continuation of Bokonon's mod by way of erotel's fork; see [`NOTICE.md`](InsulatedFarmTiles/mod/NOTICE.md) for the full lineage. | _local only — not published_ |

All five target `supportedContent: ALL` and mod `APIVersion: 2`. All require
minimum game build `744825` except Fast Insulated Self Sealing AirLock, which
declares `737790`.

## Building

You need the [.NET SDK](https://dotnet.microsoft.com/download) and a local
Oxygen Not Included install. The game's assemblies (`Assembly-CSharp.dll`,
`0Harmony.dll`, the UnityEngine DLLs) are Klei's property — they are referenced
from your install and are **never** committed to or redistributed by this repo.

**One-time setup.** Tell the build where the game is:

```sh
cp GamePath.local.props.example GamePath.local.props
```

Then edit `GamePath.local.props` and set `GameLibsDir` to your
`OxygenNotIncluded_Data\Managed` folder. That file is gitignored, because the
path is specific to your machine.

**Build:**

```sh
dotnet build ONIMods.sln
```

Instead of the local props file you may set the `ONI_INSTALL_DIR` environment
variable to the game root, or pass the path directly:

```sh
dotnet build ONIMods.sln -p:GameLibsDir="/path/to/OxygenNotIncluded_Data/Managed"
```

If `GameLibsDir` is unset or wrong, the build stops with a message telling you
so, rather than emitting hundreds of "type not found" errors.

## Installing a built mod

Each mod's build stages a complete, ready-to-install folder at
`<Mod>/dist/<ModName>/` — the DLL alongside `mod.yaml` and `mod_info.yaml`.
Copy that folder into the game's local mods directory:

- **Windows** — `%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\Local\`
- **Linux** — `~/.config/unity3d/Klei/Oxygen Not Included/mods/Local/`
- **macOS** — `~/Documents/Klei/OxygenNotIncluded/mods/Local/`

Then enable the mod from the in-game **Mods** menu and restart.

> On Windows, Auto Machines, Lumen, Fast Insulated Self Sealing AirLock and
> Insulated Farm Tiles also try to copy themselves straight into the Local mods
> folder after each build. If Controlled Folder Access blocks that, the build
> still succeeds and prints a warning — copy the staged `dist/` folder across by
> hand.

> Fast Insulated Self Sealing AirLock is also published on the Workshop under the
> same `staticID`. Keep the Workshop copy disabled while testing a local build,
> or two mods claim one ID.

## Publishing to the Steam Workshop

Upload `<Mod>/dist/<ModName>/` — the same staged folder you would install
locally. **Not** `<Mod>/mod/`, which holds only source metadata.

The game's mod loader scans the **top level** of the uploaded folder and counts
just two things as content: files ending in `.dll` or `.po`, and directories
named `strings`, `codex`, `elements`, `templates`, `worldgen`, `buildingfacades`
or `anim`. `mod.yaml`, `mod_info.yaml` and `config.json` count for nothing, and
the scan does not recurse — so a package missing its DLL, or one that wraps the
mod in an extra parent folder, loads as empty and shows

```
<Mod Title> - No compatible mod found
```

in the Mods menu, with no way to enable it. That message always means "nothing
recognisable in the folder"; a DLC or game-version mismatch reports
*"Incompatible DLC configuration"* instead. Auto Machines shipped exactly this
way once — see `AutoMachines/CLAUDE.md` for the full loader rules.

## Repository layout

```
ONIMods/
├── Directory.Build.props            shared build config: net48, game refs, package versions
├── GamePath.local.props.example     copy to GamePath.local.props (gitignored) and edit
├── NuGet.config                     repo-scoped nuget.org source
├── ONIMods.sln
├── AutoMachines/
│   ├── mod/                         mod.yaml, mod_info.yaml
│   └── src/AutoMachines/            sources + mod-specific build settings
├── UnlockAllBlueprints/
│   ├── mod/                         mod.yaml, mod_info.yaml
│   └── src/UnlockAllBlueprints/     sources + mod-specific build settings
├── Lumen/
│   ├── mod/                         mod.yaml, mod_info.yaml, config.json
│   └── src/Lumen/                   sources + mod-specific build settings
└── InsulatedFarmTiles/
    ├── mod/                         mod.yaml, mod_info.yaml, NOTICE.md, anim/
    └── src/InsulatedFarmTiles/      sources + mod-specific build settings
```

`Directory.Build.props` is imported automatically into every project, so the
target framework, the shared game references and the
`Microsoft.NETFramework.ReferenceAssemblies` version are declared exactly once.
A mod needing an extra assembly (Auto Machines, Lumen and Insulated Farm Tiles
all use `Newtonsoft.Json` for their settings) declares that in its own `.csproj`.

Auto Machines and Insulated Farm Tiles carry an `ILRepack.targets` next to their
`.csproj`, holding the target that merges PLib into the mod assembly. **That
filename is load-bearing**: `ILRepack.Lib.MSBuild.Task` injects a merge target of
its own in Release builds unless a file of exactly that name exists, and the
injected one passes no `LibraryPath`, so it cannot resolve the game's
`Newtonsoft.Json` (referenced `Private=false` and therefore never copied to the
output). Deleting either file breaks `dotnet build ONIMods.sln -c Release` — and
only Release, which is what makes it hard to spot.

The injected target is Release-only, so a mod that is only ever built with the
Debug-defaulting command above never reaches it and does not need the file — Fast
Insulated Self Sealing AirLock merges PLib without one.

Mods target **.NET Framework 4.8**, not 4.7.1: the game ships `0Harmony.dll`
built against 4.8, and targeting lower makes MSBuild refuse to resolve it
(MSB3274) and, through the indirect dependency, `Assembly-CSharp` too (MSB3275).

## Working on these mods

Practices that apply to every mod here. Each mod's `CLAUDE.md` carries its own
engineering notes; this is what generalises.

### Verify against the assembly, never from memory

Klei changes APIs between updates, and plausible-but-wrong recollections of a
method signature cost more time than looking. Install the decompiler once:

```sh
dotnet tool install -g ilspycmd     # run from inside this repo; see NuGet note below
```

Then read the real thing, e.g.:

```sh
ilspycmd -t CeilingLightConfig "<install>/OxygenNotIncluded_Data/Managed/Assembly-CSharp.dll"
```

Two quirks: `ilspycmd` cannot resolve nested generics such as
`Components.Cmps\`1` — decompile the outer type and read the nested class out of
the output — and some types live in `Assembly-CSharp-firstpass.dll` instead.

**NuGet is repo-scoped by design.** The machine-wide config has an intentionally
empty `<packageSources>`; this repo's `NuGet.config` restores nuget.org locally.
Any `dotnet tool install` or `restore` must therefore be run from inside this
repo, or it fails with "No NuGet sources are defined or enabled".

### Decompile other mods too, not just the game

When behaviour looks wrong and a *vanilla* building reproduces it, that proves
the bug is not yours — it does **not** prove the behaviour is stock. Any mod that
patches a system globally makes every vanilla building in that system a
compromised control.

Lumen lost several rounds to exactly this before the real cause turned out to be
a third-party mod patching `DiscreteShadowCaster` for the whole game. Installed
mod DLLs sit in `mods/Steam/<workshop id>/` and decompile the same way the game
does — usually faster than reasoning about what a mod *might* be doing, and it
gives the version actually running rather than whatever is on GitHub.

### Dump what you cannot decompile

Some data only exists at runtime — kanim symbol names live in binary build files,
not in `Assembly-CSharp`. A temporary `Debug.Log` behind a one-shot guard, fired
from a `Game.OnSpawn` postfix so nothing has to be built in-game, answers those
questions in one launch. Record the results in the mod's `CLAUDE.md` and delete
the diagnostic; the log is the throwaway, the notes are the artefact.

### Scope Harmony patches so they cannot reach other mods' content

Prefer matching on a component only your mod adds. Where that is impossible — the
building *preview* prefab carries none of your components — match on your prefab
ID prefix. Lumen's two patches use one of each.

### Read the log after every in-game test

`%USERPROFILE%\AppData\LocalLow\Klei\Oxygen Not Included\Player.log`

Harmony failures, `MISSING.STRINGS` errors and mod load order all surface there,
and mod load order is visible near the top — useful when a compatibility question
turns out to be a sequencing question.

### Configuration belongs in the in-game options screen

**Any mod here that has settings should expose them through the Mods-menu options
dialog, not a `config.json` a player has to find and hand-edit.** New mods start
that way; existing ones move across as they are touched. Auto Machines and
Insulated Farm Tiles are converted; Lumen still uses a JSON file.

Editing JSON by hand is the wrong ask of a player who just subscribed on the
Workshop, and there is a concrete failure mode behind the preference: a config
file inside the mod folder is **overwritten by Steam on every mod update**, so
settings silently reset. Both converted mods therefore keep their file in the
game's shared `mods\config\` directory instead.

#### PLib is what provides it

Peter Han's [PLib](https://github.com/peterhaneve/ONIMods) (NuGet package `PLib`)
supplies the options dialog — `POptions` and `SingletonOptions`. A mod declares
one `[Option]`-attributed property per setting on a class implementing `IOptions`,
registers it in `OnLoad` with `new POptions().RegisterOptions(this, typeof(Options))`,
and the gear icon appears next to the mod in the Mods menu.

Three things about it are not obvious:

- **PLib is merged into the mod assembly with ILRepack, not shipped beside it.**
  That is how PLib is designed to be consumed: every mod carries its own copy and
  they arbitrate at runtime through `PRegistry`. ONI does not probe the mod folder
  for sibling assemblies, so an unmerged build loads and then throws
  `FileNotFoundException` on first use. Merging needs
  `CopyLocalLockFileAssemblies=true` to override the repo-wide `false`, or
  `PLib.dll` never reaches the output for ILRepack to find.
- **Settings must be read before `PatchAll`** if patch classes gate themselves on
  them via Harmony's `Prepare()`, because `PatchAll` evaluates `Prepare()` during
  the call.
- **Mark options `[RestartRequired]` when the mod acts on them at load.** Building
  prefabs are constructed once per process, so a toggle that decides whether a
  patch exists cannot take effect mid-session, and an options screen that appears
  to apply live when it does not is worse than one that says so.

### Stay in the mod you were asked to change

Each folder is an independently published mod with its own history, its own
`CLAUDE.md`, and often its own work in progress. A problem noticed in a
neighbouring mod is worth *reporting*, not fixing in passing — and worth checking
against how that mod is actually built before calling it a problem at all. A
failure that only appears under a configuration a mod never uses is not a defect
in that mod.

## Upstream repositories

These folders were merged in with `git subtree`, so their original history is
intact and changes can still be pushed back:

| Folder | Origin |
| --- | --- |
| `AutoMachines/` | [1samadams/ONI_AM](https://github.com/1samadams/ONI_AM) |
| `UnlockAllBlueprints/` | [1samadams/ONI_ALLBP](https://github.com/1samadams/ONI_ALLBP) |

## References

Other people's work that these mods build on, interoperate with, or that was
read while working out how the game behaves.

| Reference | Why it matters here |
| --- | --- |
| [peterhaneve/ONIMods](https://github.com/peterhaneve/ONIMods) — Peter Han | The reference body of ONI mod source. `PLibLighting` in particular shows which lighting methods can safely be patched. Not a dependency of anything here. |
| [Rotate Everything](https://steamcommunity.com/sharedfiles/filedetails/?id=1715709940) — Jarodamus Prime | Globally makes cone lights honour their direction, which is what Lumen's rotation is built on. Also the source of the inverted light *preview* seen on vanilla Sun Lamps. No public source; decompile the installed DLL. |
| [mrcyclo/ONIInsulatedSelfSealingAirLock](https://github.com/mrcyclo/ONIInsulatedSelfSealingAirLock) — Tuna / mrcyclo | The upstream the Fast Insulated Self Sealing AirLock lineage descends from. A separate mod with its own prefab ID, not a dependency; used as the reference that confirmed this mod's recovered source is faithful. No licence file. |
| [Bokonon-ONI/ONI-Mods](https://github.com/Bokonon-ONI/ONI-Mods) — Bokonon | The original Insulated Farm Tiles, and the source of its animation assets. MIT. |
| [erotel/InsulatedFarmTilesFixed](https://github.com/erotel/InsulatedFarmTilesFixed) — Martin Zatloukal | The intermediate fork that diagnosed the material-scaling bug and restored vanilla build speed. MIT. Its material-independent insulation survives here as a `config.json` option. |
| [PLib](https://github.com/peterhaneve/ONIMods) — Peter Han | Options screen and `.po` localisation for the airlock. Pulled from NuGet and ILRepacked into the mod assembly, which is how PLib is meant to ship. |
| [ILSpy / `ilspycmd`](https://github.com/icsharpcode/ILSpy) | How every claim in these mods was verified against the real assemblies — and how the airlock's lost source was recovered. Pass `-r <game>\Managed` or every game enum decompiles as a bare integer cast. |
| [Harmony](https://github.com/pardeike/Harmony) | The patching library the game ships (`0Harmony.dll`, built against .NET 4.8 — the reason these mods target net48). |
| [ONI Wiki](https://oxygennotincluded.wiki.gg/) | Vanilla numbers, for sanity-checking balance against stock buildings. |

Klei's assemblies are their property. They are referenced from a local install
and are never committed to or redistributed by this repository.

## Licence

Auto Machines carries an MIT licence (`AutoMachines/LICENSE`).

Insulated Farm Tiles is MIT too (`InsulatedFarmTiles/LICENSE`), inherited from
Bokonon's original and erotel's fork. Credits and a standing takedown offer are
in [`InsulatedFarmTiles/mod/NOTICE.md`](InsulatedFarmTiles/mod/NOTICE.md), which
ships with the mod.

Fast Insulated Self Sealing AirLock is a community continuation of prior work
that carries no licence of its own: neither Neavo's original nor Tuna / mrcyclo's
repository has a licence file. Credit, provenance and a standing takedown offer
to any prior author are recorded in
[`FastInsulatedSelfSealingAirLock/mod/NOTICE.md`](FastInsulatedSelfSealingAirLock/mod/NOTICE.md),
which ships with the mod.
