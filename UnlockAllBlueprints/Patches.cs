using Database;
using HarmonyLib;

namespace UnlockAllBlueprints
{
    /// <summary>
    /// Forces every Printing Pod blueprint to report Universal rarity. Universal
    /// rarity is the tier the game always considers unlocked, so these patches
    /// make every blueprint selectable regardless of Colony Achievement progress
    /// or Klei account unlock status.
    ///
    /// There is one patch class per implementation of <c>IBlueprintInfo</c> that
    /// declares a <c>rarity</c> getter — Harmony cannot patch the interface member
    /// itself, so each concrete type needs its own patch. The info types live in
    /// the global namespace; only <c>PermitRarity</c> comes from <c>Database</c>.
    /// </summary>
    public static class Patches
    {
        [HarmonyPatch(typeof(BuildingFacadeInfo), nameof(BuildingFacadeInfo.rarity), MethodType.Getter)]
        public static class BuildingFacadeInfo_Rarity_Patch
        {
            public static bool Prefix(ref PermitRarity __result)
            {
                __result = PermitRarity.Universal;
                return false;
            }
        }

        [HarmonyPatch(typeof(ArtableInfo), nameof(ArtableInfo.rarity), MethodType.Getter)]
        public static class ArtableInfo_Rarity_Patch
        {
            public static bool Prefix(ref PermitRarity __result)
            {
                __result = PermitRarity.Universal;
                return false;
            }
        }

        [HarmonyPatch(typeof(ClothingItemInfo), nameof(ClothingItemInfo.rarity), MethodType.Getter)]
        public static class ClothingItemInfo_Rarity_Patch
        {
            public static bool Prefix(ref PermitRarity __result)
            {
                __result = PermitRarity.Universal;
                return false;
            }
        }

        [HarmonyPatch(typeof(BalloonArtistFacadeInfo), nameof(BalloonArtistFacadeInfo.rarity), MethodType.Getter)]
        public static class BalloonArtistFacadeInfo_Rarity_Patch
        {
            public static bool Prefix(ref PermitRarity __result)
            {
                __result = PermitRarity.Universal;
                return false;
            }
        }

        [HarmonyPatch(typeof(StickerBombFacadeInfo), nameof(StickerBombFacadeInfo.rarity), MethodType.Getter)]
        public static class StickerBombFacadeInfo_Rarity_Patch
        {
            public static bool Prefix(ref PermitRarity __result)
            {
                __result = PermitRarity.Universal;
                return false;
            }
        }

        [HarmonyPatch(typeof(EquippableFacadeInfo), nameof(EquippableFacadeInfo.rarity), MethodType.Getter)]
        public static class EquippableFacadeInfo_Rarity_Patch
        {
            public static bool Prefix(ref PermitRarity __result)
            {
                __result = PermitRarity.Universal;
                return false;
            }
        }

        [HarmonyPatch(typeof(MonumentPartInfo), nameof(MonumentPartInfo.rarity), MethodType.Getter)]
        public static class MonumentPartInfo_Rarity_Patch
        {
            public static bool Prefix(ref PermitRarity __result)
            {
                __result = PermitRarity.Universal;
                return false;
            }
        }
    }
}
