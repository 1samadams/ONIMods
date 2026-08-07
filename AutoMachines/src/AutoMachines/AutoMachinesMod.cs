using HarmonyLib;
using KMod;

namespace AutoMachines
{
    /// <summary>
    /// Mod entry point. Config must be read before base.OnLoad, because that call
    /// runs Harmony's PatchAll, which evaluates each patch class's Prepare().
    /// </summary>
    public sealed class AutoMachinesMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Settings.Load(path);
            base.OnLoad(harmony);
            Settings.Log("loaded");
        }
    }
}
