using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

namespace InsulatedFarmTiles
{
    /// <summary>
    /// Mod entry point.
    ///
    /// Ordering matters. Both the options and the strings have to be in place
    /// before <c>base.OnLoad</c> runs <c>PatchAll</c> and, later, before the game
    /// sweeps the assembly for <c>IBuildingConfig</c> types:
    ///
    /// <list type="bullet">
    /// <item>the options feed <c>BuildingDef.ThermalConductivity</c> and decide
    /// which insulating component goes on the prefab, both read once when the
    /// definition is built.</item>
    /// <item>the string table feeds the buildings' names and descriptions.
    /// Register late and they show up in the menu as
    /// <c>MISSING.STRINGS...</c>.</item>
    /// </list>
    ///
    /// <c>UserMod2.OnLoad</c> is the only entry point the game calls --
    /// <c>KMod.DLLLoader</c> looks for one <c>UserMod2</c> per assembly and
    /// invokes exactly this method. The mod this descends from also carried a
    /// nested <c>OnModLoad.OnLoad()</c> that nothing has ever called; it was
    /// dropped rather than carried forward as though it worked.
    /// </summary>
    public class InsulatedFarmTilesMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            PUtil.InitLibrary();

            // Insulation strength spans five orders of magnitude -- 0.00001 for
            // Insulation up to 1. A linear slider would put every useful value in
            // the leftmost pixel, so floats in THIS mod's options get PLib's
            // log-scale slider instead. AddOptionClass is scoped to this mod.
            OptionsHandlers.AddOptionClass(typeof(float), typeof(LogFloatOptionsEntry));

            new POptions().RegisterOptions(this, typeof(Settings));

            // Force the read now rather than leaving it to SingletonOptions' lazy
            // initialiser, so the settings are in memory before anything can ask
            // for them and the line below reports what will actually be used.
            Settings settings = Settings.Instance;

            ModStrings.Register();

            base.OnLoad(harmony);

            UnityEngine.Debug.Log(
                "[InsulatedFarmTiles] Loaded. Insulation: " + settings.Mode + " @ " + settings.ResolvedConductivity);
        }
    }
}
