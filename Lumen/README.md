# Lumen

Five new motion-activated light fixtures for *Oxygen Not Included*, unlocked
alongside the Duplicant Motion Sensor.

Each one draws **1 W** and stays completely dark — and completely unpowered —
until a Duplicant walks into range. Duplicants still get the lit-workspace work
speed bonus, because the game grants that bonus per work tick based on the light
level of the tile the worker is standing in, and the light is on whenever
somebody is there to work.

**Vanilla lights are not touched.** No existing building is patched, so saves
made without this mod behave identically and the Ceiling Light you already built
still costs 10 W.

## The lights

| Building | Size | Mount | Light | Range | Extra sensing | Stays lit | Materials |
| --- | --- | --- | --- | --- | --- | --- | --- |
| **Lumen Spotlight** | 1×1 | Ceiling | Cone, 1800 lux | 4 | — | 5 s | 25 kg metal |
| **Lumen Panel Light** | 1×1 | Ceiling | Cone, 1800 lux | 8 | — | 5 s | 50 kg glass |
| **Lumen Floodlight** | 1×1 | Ceiling | Cone, 2400 lux | 12 | — | 8 s | 100 kg refined metal |
| **Lumen Floor Lamp** | 1×2 | Floor | Circle, 1400 lux | 5 | — | 5 s | 50 kg metal |
| **Lumen Sentry Light** | 1×1 | Ceiling | Cone, 1800 lux | 8 | **+12 tiles** | 10 s | 100 kg refined metal |

**A fixture lights up when a Duplicant stands somewhere it would actually
illuminate** — no separate radius to tune, and no way for the sensor to drift out
of step with the beam. The trigger area is computed with the same shadow caster
the light grid uses, so it follows the cone's real shape and respects walls.

That is also exactly the condition the game uses to grant the work speed bonus,
which reads the light level of the tile the worker is standing in. If a fixture
lights a Duplicant, it was already on.

The Sentry is the one exception: it additionally senses in a plain 12-tile radius
beyond its beam, ignoring walls, so a corridor is bright before anyone arrives.

## Rotation

With the [Rotate Everything](https://steamcommunity.com/sharedfiles/filedetails/?id=1715709940)
mod installed, the four cone fixtures can be **rotated in all four directions** —
mount them to throw light sideways or upward. The beam, the glow and the motion
sensor's trigger area all follow wherever you aim it.

Without that mod they stay fixed, on purpose. Stock *Oxygen Not Included* cannot
aim a cone at all: `DiscreteShadowCaster` scans a hardcoded downward octant pair
and never reads the direction it was given. Offering rotation anyway would spin
the fixture's sprite while the light kept pointing at the floor. The Floor Lamp
is never rotatable — it casts a circle, which looks identical from every angle.

All five appear under **Furniture → Lights** and are unlocked by the
**Logic Control** research node — the same one that gives you the Duplicant
Motion Sensor.

## Why 1 W is not overpowered

A conventional 1 W light would trivialise the two things light is expensive for.
These do not, because they are dark whenever nobody is standing under them:

- **Farms.** Bristle Blossoms need *continuous* light. A fixture that switches
  off seconds after a Duplicant leaves is useless for growing anything, so
  Sun Lamps keep their job.
- **Ambient base lighting.** Lighting a whole base for a rounding error only
  works if the lights are on, and they are not.

The sensor is what pays for the wattage. If you raise `Watts` in `config.json`
without also widening `LingerSeconds`, you are turning the wrong dial.

## Configuration

`config.json` sits next to the DLL in the mod folder. Every field is optional;
omit one and the building's default is used.

```json
{
  "Lights": {
    "LumenSpotlight": { "Enabled": true, "Watts": 1.0, "ExtraSensorRadius": 0.0, "LingerSeconds": 5.0 }
  }
}
```

- `Enabled` — `false` hides the building from the build menu and the research
  tree. Copies already placed in a save keep working.
- `Watts` — power draw while lit. Zero while dark, regardless of this value.
  Self-heat scales with it, so raising this does not buy free cooling.
- `ExtraSensorRadius` — detection reach *beyond* the lit beam, in tiles. `0` means
  the fixture triggers strictly on what it lights, which is what you usually
  want. Raise it to make a fixture anticipate arrivals, as the Sentry does.
- `LingerSeconds` — how long it stays lit after the last Duplicant leaves.

> Earlier versions had a `SensorRadius` field that described a plain sphere around
> the fixture. It is gone, and files still using it fall back to the new defaults.

Unknown building names are ignored with a warning in `Player.log`. A malformed
file falls back to defaults rather than stopping the mod from loading.

Your edits survive rebuilds: the build only copies `config.json` into the game's
mod folder if it is not already there. Delete it to regenerate the defaults.

## Art

No custom `.kanim` files. The fixtures reuse Klei's `ceilinglight` and `floorlamp`
animations and are told apart three ways:

- **Housing colour** — all four ceiling fixtures share one neutral steel body, so
  they read as a single product line.
- **Lens colour** — each fixture tints only its own lens symbols (amber, cool
  white, cyan, green), which is what actually distinguishes them.
- **Size** — an `animScale` multiplier that deliberately tracks the light's
  range, so a fixture that reaches further looks bigger. Size is information, not
  decoration.

Only **base-game** anims are used. A DLC-only anim is absent on installs without
that DLC and the game does not degrade gracefully — it throws while registering
the building and you lose it entirely. That shipped as a bug once, when the Panel
Light borrowed the Glass Ceiling Light's DLC5 anim.

The honest limit: `ceilinglight_kanim` contains exactly **one** body part, so the
four ceiling fixtures cannot be given different silhouettes without new art. The
Panel and Sentry share a size and differ only by lens colour. `floorlamp_kanim`
is far richer — seven separable parts — so any future part-swapping work belongs
there.

## Compatibility

Vanilla lights are never patched. The two Harmony patches this mod does apply are
scoped so they cannot touch anything else: one matches on a Lumen-only component,
the other on the `Lumen` prefab-name prefix. Both exist solely to keep a rotated
Lumen fixture's beam and placement preview pointing the same way as its sprite.

## Building

See the [monorepo README](../README.md) for one-time setup. Then:

```sh
dotnet build src/Lumen/Lumen.csproj -c Release
```

The build stages a ready-to-install folder at `dist/Lumen/` and tries to copy it
into the game's Local mods folder.

## How it works, and how that was established

Every claim here was read out of the game's own assemblies with
[ILSpy](https://github.com/icsharpcode/ILSpy) (`ilspycmd`) against build
`744825`, not recalled. `Lumen/CLAUDE.md` carries the full engineering notes;
this is the short version.

**One flag does everything.** The sensor never touches `Light2D`, the animation
or the power draw. It sets a single `Operational.Flag`, and Klei's own chain does
the rest: a false flag makes `IsOperational` false → `SetActive(false)` →
`EnergyConsumer.WattsUsed` returns literal `0f` → an `OperationalChanged` event
that `Light2D` and `LightController` already react to. That is why "dark" means
genuinely zero watts rather than a cosmetic off state.

**The trigger area is the light itself.** The sensor asks
`DiscreteShadowCaster.GetVisibleCells` which cells the fixture *would* light —
a pure query that works while the light is off — and fires when a Duplicant
stands in one. This replaced a plain radius, which was wrong in both directions:
a sphere small enough not to catch people through walls never reached the floor
below a ceiling mount, and one large enough to reach the floor fired constantly.
It also happens to be the exact test `Workable` uses to grant the work speed
bonus, so a lit Duplicant was necessarily already switched on.

**Cones cannot aim in stock ONI.** `GetVisibleCells` scans a hardcoded downward
octant pair for `LightShape.Cone` and never reads the direction it is given —
only `ScanQuad` honours it. Rotation is therefore gated on a mod that changes
that; see below.

### Compatibility notes worth knowing

**[Rotate Everything](https://steamcommunity.com/sharedfiles/filedetails/?id=1715709940)**
(Jarodamus Prime) globally replaces `GetVisibleCells` so cones *do* honour their
direction — which is what Lumen's rotation is built on. It also updates
`LightShapePreview.direction` only for prefabs named `CeilingLight*`. Since that
field defaults to `Direction.North` and no vanilla config ever sets it, every
other cone light in the game — including the stock Sun Lamp — starts previewing
as a cone aimed straight up. Lumen sets it explicitly, so its own previews are
correct either way. If you see an upward preview cone on a *vanilla* light, that
is where it comes from, and it is cosmetic.

**[PLib](https://github.com/peterhaneve/ONIMods/tree/main/PLibLighting)**
(Peter Han) is not a dependency, but its lighting module patches the same
methods and was a useful reference for what is safe to touch.

A diagnosis lesson recorded here because it cost real time: **a vanilla building
is not a control when a mod patches lighting globally.** The inverted preview was
initially written off as stock Klei behaviour because the vanilla Sun Lamp
reproduced it. That only proved it was not Lumen-specific. Decompiling the
installed third-party DLL took minutes and gave the real answer.
