using HarmonyLib;
using KMod;

namespace Lumen
{
    /// <summary>
    /// Mod entry point.
    ///
    /// Ordering matters here. Both the config and the strings have to be in place
    /// before base.OnLoad runs PatchAll and, later, before the game sweeps the
    /// assembly for IBuildingConfig types:
    ///
    ///   - config.json feeds BuildingDef.EnergyConsumptionWhenActive, which is read
    ///     once when the definition is created and copied into EnergyConsumer at
    ///     prefab init. Load it late and every light silently reverts to its default
    ///     wattage.
    ///   - the string table feeds the building's name and description. Register late
    ///     and the buildings show up in the menu as MISSING.STRINGS...
    /// </summary>
    public class LumenMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Settings.Load(path);
            LumenStrings.Register();

            base.OnLoad(harmony);

            UnityEngine.Debug.Log("[Lumen] Loaded.");
        }
    }
}
