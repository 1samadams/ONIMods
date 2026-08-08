# ONIMods

A monorepo of my mods for [*Oxygen Not Included*](https://www.klei.com/games/oxygen-not-included).

Each mod lives in its own top-level folder, keeping the full commit history of
the standalone repository it came from (merged with `git subtree`). One
solution, `ONIMods.sln`, builds them all.

## Mods

| Mod | Folder | What it does | Workshop |
| --- | --- | --- | --- |
| **Auto Machines** | [`AutoMachines/`](AutoMachines/) | Makes duplicant-operated fabricators run on their own once materials are delivered. Patches the existing buildings in place instead of cloning them, so recipes, outputs and masses stay exactly vanilla. Every building is individually toggleable in `config.json`. | [Subscribe](https://steamcommunity.com/sharedfiles/filedetails/?id=3779252781) |
| **Unlock All Blueprints** | [`UnlockAllBlueprints/`](UnlockAllBlueprints/) | Unlocks all Printing Pod blueprints — building facades, artables, clothing items, balloon artist facades, sticker bombs, equippable facades and monument parts — regardless of Colony Achievement or Klei account unlock status. | _local only — not published_ |
| **Lumen** | [`Lumen/`](Lumen/) | Adds five motion-activated light fixtures unlocked alongside the Duplicant Motion Sensor. Each draws 1 W and stays dark — and genuinely unpowered — until a Duplicant walks into range, so Duplicants still get the lit-workspace work speed bonus without lighting an empty base. The four cone fixtures aim in all four directions when *Rotate Everything* is installed. Vanilla lights are left untouched. Every fixture is tunable in `config.json`. | _local only — not published_ |

All three target `supportedContent: ALL` and mod `APIVersion: 2`, minimum game
build `744825`.

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

> On Windows, Auto Machines and Lumen also try to copy themselves straight into
> the Local mods folder after each build. If Controlled Folder Access blocks
> that, the build still succeeds and prints a warning — copy the staged `dist/`
> folder across by hand.

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
│   ├── mod/                         mod.yaml, mod_info.yaml, config.json
│   └── src/AutoMachines/            sources + mod-specific build settings
├── UnlockAllBlueprints/
│   ├── mod/                         mod.yaml, mod_info.yaml
│   └── src/UnlockAllBlueprints/     sources + mod-specific build settings
└── Lumen/
    ├── mod/                         mod.yaml, mod_info.yaml, config.json
    └── src/Lumen/                   sources + mod-specific build settings
```

`Directory.Build.props` is imported automatically into every project, so the
target framework, the shared game references and the
`Microsoft.NETFramework.ReferenceAssemblies` version are declared exactly once.
A mod needing an extra assembly (Auto Machines and Lumen both use
`Newtonsoft.Json` to read their `config.json`) declares that in its own
`.csproj`.

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
| [ILSpy / `ilspycmd`](https://github.com/icsharpcode/ILSpy) | How every claim in these mods was verified against the real assemblies. |
| [Harmony](https://github.com/pardeike/Harmony) | The patching library the game ships (`0Harmony.dll`, built against .NET 4.8 — the reason these mods target net48). |
| [ONI Wiki](https://oxygennotincluded.wiki.gg/) | Vanilla numbers, for sanity-checking balance against stock buildings. |

Klei's assemblies are their property. They are referenced from a local install
and are never committed to or redistributed by this repository.

## Licence

Auto Machines carries an MIT licence (`AutoMachines/LICENSE`).
