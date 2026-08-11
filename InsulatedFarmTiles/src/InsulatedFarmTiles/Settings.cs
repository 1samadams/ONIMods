using System.IO;
using Newtonsoft.Json;

namespace InsulatedFarmTiles
{
    /// <summary>
    /// config.json, loaded once at mod load.
    ///
    /// It must be read before the building definitions are created:
    /// <see cref="TargetConductivity"/> is baked into
    /// <c>BuildingDef.ThermalConductivity</c> by <c>CreateBuildingDef</c>, and
    /// <see cref="MaterialIndependentInsulation"/> decides which insulating
    /// component <c>ConfigureBuildingTemplate</c> puts on the prefab. Loading it
    /// late would silently give both tiles the defaults.
    ///
    /// A malformed file falls back to defaults rather than stopping the mod from
    /// loading -- a typo in a config file should not cost the player their save's
    /// buildings.
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// How the tiles insulate.
        ///
        /// <c>false</c> (default) is vanilla Insulated Tile parity: the game's own
        /// <c>Insulator</c> component applies <see cref="TargetConductivity"/> as
        /// the cell's insulation, and the sim multiplies it by the build
        /// material's own conductivity. Sandstone lands on 0.029, Ceramic on
        /// 0.0062 -- exactly the numbers a vanilla Insulated Tile gives, so the
        /// familiar "build it from something better" progression is preserved and
        /// the building's info panel tells the truth.
        ///
        /// <c>true</c> is erotel's fork behaviour: <see cref="FarmTileInsulation"/>
        /// divides out the material, forcing the effective conductivity to
        /// <see cref="TargetConductivity"/> whatever the tile is built from. Every
        /// raw mineral then insulates better than a vanilla Insulated Tile of the
        /// same material -- and Ceramic insulates *worse* than it otherwise would,
        /// because the clamp only ever raises a poor material to the target.
        /// </summary>
        public bool MaterialIndependentInsulation = false;

        /// <summary>
        /// 0.01 is the value vanilla <c>InsulationTileConfig</c> uses, so the
        /// default is a like-for-like copy of the Insulated Tile either way.
        ///
        /// Its meaning does depend on the mode above: with
        /// <see cref="MaterialIndependentInsulation"/> off it is the *multiplier*
        /// applied to the build material's conductivity; with it on it is the
        /// resulting effective conductivity itself.
        /// </summary>
        public float TargetConductivity = 0.01f;

        private static Settings instance;

        public static Settings Instance => instance ?? (instance = new Settings());

        public static void Load(string modPath)
        {
            string path = Path.Combine(modPath, "config.json");

            if (!File.Exists(path))
            {
                UnityEngine.Debug.Log("[InsulatedFarmTiles] No config.json at " + path + ", using defaults.");
                instance = new Settings();
                return;
            }

            try
            {
                instance = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path)) ?? new Settings();

                // A non-positive conductivity would divide by zero in
                // material-independent mode and mean "perfectly conductive" in
                // parity mode -- neither is what anyone editing this file wants.
                if (!(instance.TargetConductivity > 0f))
                {
                    UnityEngine.Debug.LogWarning(
                        "[InsulatedFarmTiles] TargetConductivity must be greater than 0, ignoring "
                        + instance.TargetConductivity + " and using 0.01.");
                    instance.TargetConductivity = 0.01f;
                }

                UnityEngine.Debug.Log("[InsulatedFarmTiles] Loaded config from " + path);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning(
                    "[InsulatedFarmTiles] config.json could not be read (" + e.Message + "), using defaults.");
                instance = new Settings();
            }
        }
    }
}
