# Working in this repo

Repo-wide standing instructions. Each mod folder has its own `CLAUDE.md` with the
engineering notes specific to it; this file is what applies everywhere. The
README's "Working on these mods" section covers the same ground for humans.

## Stay in the mod you were asked to change

Each top-level folder is an independently published mod with its own history, its
own `CLAUDE.md`, and often its own in-flight work in another session. **Do not
edit files under another mod's folder unless asked.** That includes build
plumbing, and it includes fixes that look obviously correct.

If something looks wrong in a neighbouring mod, **report it and stop there.**
Before reporting it at all, check it against how that mod is actually built and
shipped:

- A failure that only appears under a configuration the mod never uses is not a
  defect in that mod. If a mod's documented build command is
  `dotnet build ONIMods.sln` — which defaults to **Debug** — then something that
  only breaks under `-c Release` says nothing about the mod's health.
- Do not present fallout from your own verification method as a pre-existing
  defect in the user's code. Check which one it is before describing it.

This happened once already: a Release-only ILRepack failure in Fast Insulated
Self Sealing AirLock was reported as a real break and "fixed", when that mod is
only ever built in Debug and was working correctly the whole time.

## Configuration belongs in the in-game options screen

**Any mod here that has settings should expose them through the Mods-menu options
dialog, not a `config.json` the player has to find and hand-edit.** New mods start
that way; existing ones convert as they are touched. Converted so far: Auto
Machines, Insulated Farm Tiles. Not yet: Lumen.

Two reasons, one of them concrete rather than aesthetic:

1. Hand-editing JSON is the wrong ask of someone who just subscribed on the
   Workshop.
2. A config file inside the mod folder is **overwritten by Steam on every mod
   update**, silently resetting settings. Converted mods keep the file in the
   game's shared `mods\config\` directory instead — PLib's `[ConfigFile]`
   attribute takes a `SharedConfigLocation` flag for exactly this.

When converting a mod that already shipped a `config.json`, migrate it once on
first run rather than resetting the player's choices.

### PLib provides it, and is merged, not shipped alongside

Peter Han's PLib (NuGet `PLib`) supplies `POptions` / `SingletonOptions`. Points
that are easy to get wrong:

- **ILRepack PLib into the mod assembly.** ONI does not probe the mod folder for
  sibling assemblies, so an unmerged build loads and then throws
  `FileNotFoundException` on first use. Needs `CopyLocalLockFileAssemblies=true`
  to override the repo-wide `false`, or `PLib.dll` never reaches the output.
- **`ILRepack.Lib.MSBuild.Task` injects a merge target of its own**, Release-only,
  suppressed only by the existence of `$(ProjectDir)ILRepack.targets`. The
  injected one passes no `LibraryPath` and so cannot resolve the game's
  `Newtonsoft.Json`. Where a mod is built in Release, that file's existence is
  load-bearing regardless of its contents. `PrivateAssets="all"` does not prevent
  the injection.
- **Read settings before `base.OnLoad(harmony)`** when patch classes gate
  themselves through Harmony's `Prepare()` — `PatchAll` evaluates `Prepare()`
  during that call, so settings must already be in memory.
- **`[RestartRequired]` when the mod acts on a setting at load.** Building prefabs
  are constructed once per process, so a toggle deciding whether a patch exists
  cannot apply mid-session. An options screen that looks live but is not is worse
  than one that says so.

## Verify against the assembly, never from memory

Klei changes APIs between updates. Decompile and read the real thing rather than
recalling a signature — `ilspycmd -t <Type> "<Managed>\Assembly-CSharp.dll"`,
passing `-r "<Managed>"` so game types resolve. Some types live in
`Assembly-CSharp-firstpass.dll`.

**Enumerate; do not hand-write lists of game types.** Auto Machines shipped
missing 14 buildings because its list of `ComplexFabricator` subclasses was
written from memory instead of grepped out of a decompile. If a task involves
"all the X in the game", derive X from the assembly.

**NuGet is repo-scoped.** The machine-wide config has an intentionally empty
`<packageSources>`; this repo's `NuGet.config` supplies nuget.org. Run any
`dotnet tool install` or `restore` from inside this repo, or it fails with "No
NuGet sources are defined or enabled" — that error means wrong working directory,
not a broken machine.

**Use the PowerShell tool for `dotnet`,** not the Bash tool, which has reported
"No .NET SDKs were found" on this machine when the SDK is installed.

## Packaging

Upload `<Mod>/dist/<ModName>/`, never `<Mod>/mod/`. The loader scans only the top
level of the package and counts just `*.dll`, `*.po`, and the directories
`strings`, `codex`, `elements`, `templates`, `worldgen`, `buildingfacades`,
`anim`. `mod.yaml`, `mod_info.yaml` and `config.json` count for nothing, so a
package of metadata alone is rejected with "No compatible mod found". Full rules
in `AutoMachines/CLAUDE.md`.

**Never change a published mod's `staticID`** to fix a packaging problem — it is
the mod's identity in save files and orphans existing subscribers' settings.
