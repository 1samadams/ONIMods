using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

namespace Lumen
{
    /// <summary>
    /// Mod entry point.
    ///
    /// Ordering matters here:
    ///
    ///   1. PUtil.InitLibrary must come first -- every other PLib call depends on it.
    ///   2. RegisterOptions puts the gear icon next to this mod in the Mods menu.
    ///   3. Settings.Load reads the options. They feed
    ///      BuildingDef.EnergyConsumptionWhenActive, which is read once when the
    ///      definition is created and copied into EnergyConsumer at prefab init.
    ///      Load them late and every light silently reverts to its default wattage.
    ///   4. The string table feeds the building's name and description. Register
    ///      late and the buildings show up in the menu as MISSING.STRINGS...
    ///
    /// All of that has to happen before base.OnLoad runs PatchAll and, later, before
    /// the game sweeps the assembly for IBuildingConfig types.
    /// </summary>
    public class LumenMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            PUtil.InitLibrary();
            new POptions().RegisterOptions(this, typeof(Options));

            Settings.Load(path);
            LumenStrings.Register();

            base.OnLoad(harmony);

            Settings.Log("Loaded.");
        }
    }
}
