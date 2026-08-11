using UnityEngine;

namespace InsulatedFarmTiles
{
    /// <summary>
    /// The one place that decides how a tile insulates, so both building configs
    /// stay identical on the subject and neither can drift.
    /// </summary>
    public static class TileInsulation
    {
        /// <summary>
        /// Called from <c>CreateBuildingDef</c>.
        ///
        /// In parity mode this is the whole mechanism: 0.01 is exactly what
        /// vanilla <c>InsulationTileConfig</c> puts here, and the game's
        /// <c>Insulator</c> component feeds it to the sim.
        ///
        /// In material-independent mode <see cref="FarmTileInsulation"/> overrides
        /// the cell value at spawn, but the def still needs a sane number: it is
        /// what the building's info panel multiplies by the material to show a
        /// thermal conductivity, and leaving the vanilla-tile value there keeps
        /// that display in the right neighbourhood instead of quoting the
        /// original mod's 1/32.
        /// </summary>
        public static void ApplyTo(BuildingDef def)
        {
            def.ThermalConductivity = Settings.Instance.TargetConductivity;
        }

        /// <summary>
        /// Called from <c>ConfigureBuildingTemplate</c>. Exactly one of the two
        /// components goes on -- see <see cref="FarmTileInsulation"/> for why they
        /// must not both be present.
        /// </summary>
        public static void AddComponentTo(GameObject go)
        {
            if (Settings.Instance.MaterialIndependentInsulation)
            {
                go.AddOrGet<FarmTileInsulation>();
            }
            else
            {
                go.AddOrGet<Insulator>();
            }
        }
    }
}
