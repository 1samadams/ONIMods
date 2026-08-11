using HarmonyLib;
using KMod;

namespace InsulatedFarmTiles
{
    /// <summary>
    /// Mod entry point.
    ///
    /// Ordering matters. Both the config and the strings have to be in place
    /// before <c>base.OnLoad</c> runs <c>PatchAll</c> and, later, before the game
    /// sweeps the assembly for <c>IBuildingConfig</c> types:
    ///
    /// <list type="bullet">
    /// <item>config.json feeds <c>BuildingDef.ThermalConductivity</c> and decides
    /// which insulating component goes on the prefab, both read once when the
    /// definition is built.</item>
    /// <item>the string table feeds the buildings' names and descriptions.
    /// Register late and they show up in the menu as
    /// <c>MISSING.STRINGS...</c>.</item>
    /// </list>
    ///
    /// <c>UserMod2.OnLoad</c> is the only entry point the game calls --
    /// <c>KMod.DLLLoader</c> looks for one <c>UserMod2</c> per assembly and
    /// invokes exactly this method. The original mod also carried a nested
    /// <c>OnModLoad.OnLoad()</c> that nothing has ever called; it has been dropped
    /// rather than carried forward as though it worked.
    /// </summary>
    public class InsulatedFarmTilesMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Settings.Load(path);
            ModStrings.Register();

            base.OnLoad(harmony);

            UnityEngine.Debug.Log(
                "[InsulatedFarmTiles] Loaded. Insulation: "
                + (Settings.Instance.MaterialIndependentInsulation
                    ? "material-independent, effective conductivity "
                    : "vanilla Insulated Tile parity, material x ")
                + Settings.Instance.TargetConductivity);
        }
    }
}
