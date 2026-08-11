using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

namespace AutoMachines
{
    /// <summary>
    /// Mod entry point.
    ///
    /// The ordering here is load-bearing:
    ///   1. PUtil.InitLibrary must come first — every other PLib call depends on it.
    ///   2. RegisterOptions puts the gear icon next to this mod in the Mods menu.
    ///   3. Settings.Load reads the options file.
    ///   4. base.OnLoad runs Harmony's PatchAll, which evaluates each patch
    ///      class's Prepare() — so the settings have to already be in memory, or
    ///      every toggle reads as its default.
    /// </summary>
    public sealed class AutoMachinesMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            PUtil.InitLibrary();
            new POptions().RegisterOptions(this, typeof(Options));

            Settings.Load(path);
            base.OnLoad(harmony);
            Settings.Log("loaded");
        }
    }
}
