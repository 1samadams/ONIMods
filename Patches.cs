using Database;
using HarmonyLib;

namespace UnlockAllBlueprints
{
    /// <summary>
    /// Forces every Printing Pod blueprint (building facades, artables, clothing
    /// items, and balloon artist facades) to report Universal rarity. Universal
    /// rarity is the tier the game always considers unlocked, so these patches
    /// make every blueprint selectable regardless of Colony Achievement progress
    /// or Klei account unlock status.
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
    }
}
