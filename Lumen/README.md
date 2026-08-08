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

These reuse Klei's `ceilinglight` and `floorlamp` animations with a per-building
colour tint, rather than shipping custom `.kanim` files. Swapping in real custom
art later is a one-line change per entry in `LumenLights.cs`.

Only **base-game** anims are used. DLC-only anims are absent on installs without
that DLC, and the game does not degrade gracefully — it throws while registering
the building, and you lose the building entirely. The four ceiling fixtures
therefore share one silhouette and are told apart by colour.

## Building

See the [monorepo README](../README.md) for one-time setup. Then:

```sh
dotnet build src/Lumen/Lumen.csproj -c Release
```

The build stages a ready-to-install folder at `dist/Lumen/` and tries to copy it
into the game's Local mods folder.
