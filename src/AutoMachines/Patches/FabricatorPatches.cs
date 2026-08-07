using HarmonyLib;
using UnityEngine;

namespace AutoMachines.Patches
{
    /// <summary>
    /// Shared logic for the eleven ComplexFabricator-based buildings.
    ///
    /// Verified against Assembly-CSharp (build 744825) by diffing KilnConfig (the
    /// vanilla automatic fabricator) against the duplicant-operated configs:
    ///
    ///   * duplicantOperated must be false.
    ///   * showProgressBar must be true. None of the duplicant-operated configs set
    ///     it, and it defaults to false, so flipping only duplicantOperated yields a
    ///     machine that runs with no progress bar. ComplexFabricator.ShowProgressBar
    ///     gates on `show && showProgressBar && !duplicantOperated`.
    ///
    /// The ComplexFabricatorWorkable component is deliberately LEFT IN PLACE. Most of
    /// these configs register a KPrefabID.prefabSpawnFn delegate that calls
    /// GetComponent&lt;ComplexFabricatorWorkable&gt;() and dereferences it with no null
    /// check; removing the component NREs at prefab spawn. It is harmless to keep:
    /// ComplexFabricator.HasWorker returns true without touching the workable once
    /// duplicantOperated is false.
    ///
    /// duplicantOperated carries no KSerialization attribute, so this writes nothing
    /// to save files.
    /// </summary>
    internal static class Fabricator
    {
        /// <summary>
        /// GetComponent (not AddOrGet) is required: several of these buildings use
        /// ComplexFabricator subclasses (LiquidCooledRefinery, GlassForge,
        /// GourmetCookingStation, MicrobeMusher, CookingStation, Apothecary).
        /// GetComponent finds the subclass; AddOrGet would bolt on a second, plain
        /// ComplexFabricator alongside it.
        /// </summary>
        public static void MakeAutomatic(GameObject go, string buildingId)
        {
            var fabricator = go.GetComponent<ComplexFabricator>();
            if (fabricator == null)
            {
                Settings.LogWarning(buildingId + ": no ComplexFabricator found, leaving vanilla. "
                    + "The building layout probably changed in a game update.");
                return;
            }

            fabricator.duplicantOperated = false;
            fabricator.showProgressBar = true;
        }
    }

    [HarmonyPatch(typeof(RockCrusherConfig), nameof(RockCrusherConfig.DoPostConfigureComplete))]
    internal static class RockCrusherPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.RockCrusher);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.RockCrusher);
    }

    [HarmonyPatch(typeof(MetalRefineryConfig), nameof(MetalRefineryConfig.DoPostConfigureComplete))]
    internal static class MetalRefineryPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.MetalRefinery);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.MetalRefinery);
    }

    [HarmonyPatch(typeof(GlassForgeConfig), nameof(GlassForgeConfig.DoPostConfigureComplete))]
    internal static class GlassForgePatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.GlassForge);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.GlassForge);
    }

    [HarmonyPatch(typeof(SupermaterialRefineryConfig), nameof(SupermaterialRefineryConfig.DoPostConfigureComplete))]
    internal static class SupermaterialRefineryPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.SupermaterialRefinery);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.SupermaterialRefinery);
    }

    [HarmonyPatch(typeof(MicrobeMusherConfig), nameof(MicrobeMusherConfig.DoPostConfigureComplete))]
    internal static class MicrobeMusherPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.MicrobeMusher);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.MicrobeMusher);
    }

    [HarmonyPatch(typeof(CookingStationConfig), nameof(CookingStationConfig.DoPostConfigureComplete))]
    internal static class CookingStationPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.CookingStation);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.CookingStation);
    }

    [HarmonyPatch(typeof(GourmetCookingStationConfig), nameof(GourmetCookingStationConfig.DoPostConfigureComplete))]
    internal static class GourmetCookingStationPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.GourmetCookingStation);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.GourmetCookingStation);
    }

    [HarmonyPatch(typeof(EggCrackerConfig), nameof(EggCrackerConfig.DoPostConfigureComplete))]
    internal static class EggCrackerPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.EggCracker);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.EggCracker);
    }

    [HarmonyPatch(typeof(ClothingFabricatorConfig), nameof(ClothingFabricatorConfig.DoPostConfigureComplete))]
    internal static class ClothingFabricatorPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.ClothingFabricator);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.ClothingFabricator);
    }

    [HarmonyPatch(typeof(SuitFabricatorConfig), nameof(SuitFabricatorConfig.DoPostConfigureComplete))]
    internal static class SuitFabricatorPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.SuitFabricator);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.SuitFabricator);
    }

    [HarmonyPatch(typeof(ApothecaryConfig), nameof(ApothecaryConfig.DoPostConfigureComplete))]
    internal static class ApothecaryPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.Apothecary);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.Apothecary);
    }
}
