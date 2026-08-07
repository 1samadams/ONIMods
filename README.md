# ONIMods

A monorepo of my mods for [*Oxygen Not Included*](https://www.klei.com/games/oxygen-not-included).

Each mod lives in its own top-level folder, keeping the full commit history of
the standalone repository it came from (merged with `git subtree`). One
solution, `ONIMods.sln`, builds them all.

## Mods

| Mod | Folder | What it does | Workshop |
| --- | --- | --- | --- |
| **Auto Machines** | [`AutoMachines/`](AutoMachines/) | Makes duplicant-operated fabricators run on their own once materials are delivered. Patches the existing buildings in place instead of cloning them, so recipes, outputs and masses stay exactly vanilla. Every building is individually toggleable in `config.json`. | <!-- TODO: Workshop link --> _not yet published_ |
| **Unlock All Blueprints** | [`UnlockAllBlueprints/`](UnlockAllBlueprints/) | Unlocks all Printing Pod blueprints — building facades, artables, clothing items, balloon artist facades, sticker bombs, equippable facades and monument parts — regardless of Colony Achievement or Klei account unlock status. | <!-- TODO: Workshop link --> _not yet published_ |

Both target `supportedContent: ALL` and mod `APIVersion: 2`, minimum game build
`744825`.

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

> On Windows, Auto Machines also tries to copy itself straight into the Local
> mods folder after each build. If Controlled Folder Access blocks that, the
> build still succeeds and prints a warning — copy the staged `dist/` folder
> across by hand.

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
└── UnlockAllBlueprints/
    ├── mod/                         mod.yaml, mod_info.yaml
    └── src/UnlockAllBlueprints/     sources + mod-specific build settings
```

`Directory.Build.props` is imported automatically into every project, so the
target framework, the shared game references and the
`Microsoft.NETFramework.ReferenceAssemblies` version are declared exactly once.
A mod needing an extra assembly (Auto Machines uses `Newtonsoft.Json` to read
its `config.json`) declares that in its own `.csproj`.

Mods target **.NET Framework 4.8**, not 4.7.1: the game ships `0Harmony.dll`
built against 4.8, and targeting lower makes MSBuild refuse to resolve it
(MSB3274) and, through the indirect dependency, `Assembly-CSharp` too (MSB3275).

## Upstream repositories

These folders were merged in with `git subtree`, so their original history is
intact and changes can still be pushed back:

| Folder | Origin |
| --- | --- |
| `AutoMachines/` | [1samadams/ONI_AM](https://github.com/1samadams/ONI_AM) |
| `UnlockAllBlueprints/` | [1samadams/ONI_ALLBP](https://github.com/1samadams/ONI_ALLBP) |

## Licence

Auto Machines carries an MIT licence (`AutoMachines/LICENSE`).
