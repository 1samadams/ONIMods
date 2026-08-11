# Insulated Farm Tiles Continued

A community continuation of Bokonon's "Insulated Farm Tiles" for Oxygen Not Included, by way of erotel's "Insulated Farm Tiles (Fixed)".

## Credits And Provenance

- **Bokonon-ONI**: the original "Insulated Farm Tiles" ([Workshop 1850356486](https://steamcommunity.com/sharedfiles/filedetails/?id=1850356486), source at https://github.com/Bokonon-ONI/ONI-Mods). All animation assets shipped here are Bokonon's work, unchanged. So are both buildings' identity, cost and research placement.
- **Martin Zatloukal / erotel**: "Insulated Farm Tiles (Fixed)" ([Workshop 3768907966](https://steamcommunity.com/sharedfiles/filedetails/?id=3768907966), source at https://github.com/erotel/InsulatedFarmTilesFixed). Identified that the original's insulation still scaled with the build material, restored vanilla build and deconstruct speed, and rebuilt for U59. The material-independent insulation mode here is their design, kept as an option.

Both are MIT-licensed; this continuation is too. See [LICENSE](../LICENSE).

## What This Continuation Changes

- Insulation is now vanilla Insulated Tile parity by default (`ThermalConductivity = 0.01`, the value `InsulationTileConfig` itself uses), with erotel's material-independent mode available in `config.json`.
- The material-independent component replaces the game's `Insulator` rather than racing it. Previously both were on the prefab and the outcome depended on Unity's undefined `Start()` ordering.
- Brought both buildings back to parity with vanilla `FarmTileConfig` / `HydroponicFarmConfig` on everything the 2019 original predates: plot seed criteria, build-menu subcategory, codex category, anim blend values, search terms, and melt notification.
- Tech and build-menu registration moved off a `Db.Initialize` postfix, which can land mid-registration and silently cost a building its research entry.

## Save Compatibility

The building prefab IDs remain `InsulatedFarmTile` and `InsulatedHydroponicFarm`, unchanged since 2019, so tiles placed by either earlier mod load as these. The mod static ID is distinct (`zerotheabsolute.InsulatedFarmTiles`).

**Do not run this alongside the original or erotel's fork.** All three register the same building IDs and would clash. Enable exactly one.

## Standing Offer

If Bokonon-ONI, Martin Zatloukal, or another author in this lineage requests changes, credit adjustments, or removal, this continuation should be updated, taken down, or redirected in favour of their version as appropriate.
