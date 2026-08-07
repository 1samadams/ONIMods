using HarmonyLib;
using KMod;

namespace UnlockAllBlueprints
{
    /// <summary>Standard ONI mod entry point — applies the Harmony patches on load.</summary>
    public class UnlockAllBlueprintsMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            harmony.PatchAll();
        }
    }
}
