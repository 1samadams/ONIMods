using HarmonyLib;
using UnityEngine;

namespace AutoMachines.Patches
{
    /// <summary>
    /// Shared logic for the twenty-five ComplexFabricator-based buildings.
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
        /// GourmetCookingStation, MicrobeMusher, CookingStation, Apothecary,
        /// AdvancedApothecary, Deepfryer, SushiBar). GetComponent finds the
        /// subclass; AddOrGet would bolt on a second, plain ComplexFabricator
        /// alongside it.
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

    // ---------------------------------------------------------------------
    // Second sweep: the buildings the first pass missed.
    //
    // Every config below was confirmed against Assembly-CSharp (build 744825)
    // to attach a ComplexFabricator or a subclass of one, to leave
    // duplicantOperated at true, and to declare its own override of
    // DoPostConfigureComplete. That last point matters: if a config did NOT
    // declare the override, Harmony would resolve the name up the hierarchy to
    // IBuildingConfig.DoPostConfigureComplete and patch every building in the
    // game. Re-check it before adding any further building here.
    //
    // Several are DLC-only. The config classes ship in the base assembly
    // regardless of which DLC is owned, so patching them is harmless — the
    // building simply never spawns without its DLC.
    // ---------------------------------------------------------------------

    /// <summary>Sludge Press.</summary>
    [HarmonyPatch(typeof(SludgePressConfig), nameof(SludgePressConfig.DoPostConfigureComplete))]
    internal static class SludgePressPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.SludgePress);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.SludgePress);
    }

    /// <summary>Emulsifier.</summary>
    [HarmonyPatch(typeof(ChemicalRefineryConfig), nameof(ChemicalRefineryConfig.DoPostConfigureComplete))]
    internal static class ChemicalRefineryPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.ChemicalRefinery);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.ChemicalRefinery);
    }

    /// <summary>Diamond Press.</summary>
    [HarmonyPatch(typeof(DiamondPressConfig), nameof(DiamondPressConfig.DoPostConfigureComplete))]
    internal static class DiamondPressPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.DiamondPress);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.DiamondPress);
    }

    /// <summary>Plant Pulverizer (config class is MilkPressConfig).</summary>
    [HarmonyPatch(typeof(MilkPressConfig), nameof(MilkPressConfig.DoPostConfigureComplete))]
    internal static class MilkPressPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.MilkPress);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.MilkPress);
    }

    /// <summary>
    /// Plywood Press (config class is FabricatedWoodMakerConfig). This is the
    /// one target that already sets showProgressBar = true itself; MakeAutomatic
    /// setting it again is a no-op.
    /// </summary>
    [HarmonyPatch(typeof(FabricatedWoodMakerConfig), nameof(FabricatedWoodMakerConfig.DoPostConfigureComplete))]
    internal static class FabricatedWoodMakerPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.FabricatedWoodMaker);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.FabricatedWoodMaker);
    }

    /// <summary>Blastshot Maker (config class is MissileFabricatorConfig).</summary>
    [HarmonyPatch(typeof(MissileFabricatorConfig), nameof(MissileFabricatorConfig.DoPostConfigureComplete))]
    internal static class MissileFabricatorPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.MissileFabricator);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.MissileFabricator);
    }

    /// <summary>
    /// Deep Fryer. Deepfryer derives from ComplexFabricator. Its RoomTracker
    /// still requires a Kitchen; automation does not change that.
    /// </summary>
    [HarmonyPatch(typeof(DeepfryerConfig), nameof(DeepfryerConfig.DoPostConfigureComplete))]
    internal static class DeepfryerPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.Deepfryer);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.Deepfryer);
    }

    /// <summary>
    /// Sushi Bar. SushiBar derives from ComplexFabricator, but its workable is a
    /// ComplexFabricatorLayeredWorkable rather than the plain
    /// ComplexFabricatorWorkable — worth knowing if this one misbehaves in
    /// testing while the others are fine.
    /// </summary>
    [HarmonyPatch(typeof(SushiBarConfig), nameof(SushiBarConfig.DoPostConfigureComplete))]
    internal static class SushiBarPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.SushiBar);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.SushiBar);
    }

    /// <summary>Crafting Station.</summary>
    [HarmonyPatch(typeof(CraftingTableConfig), nameof(CraftingTableConfig.DoPostConfigureComplete))]
    internal static class CraftingTablePatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.CraftingTable);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.CraftingTable);
    }

    /// <summary>Soldering Station (config class is AdvancedCraftingTableConfig).</summary>
    [HarmonyPatch(typeof(AdvancedCraftingTableConfig), nameof(AdvancedCraftingTableConfig.DoPostConfigureComplete))]
    internal static class AdvancedCraftingTablePatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.AdvancedCraftingTable);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.AdvancedCraftingTable);
    }

    /// <summary>
    /// Nuclear Apothecary. AdvancedApothecary derives from ComplexFabricator.
    /// It also carries an ActiveParticleConsumer, so once automated it draws
    /// radbolts without a duplicant present — which is what running it means.
    /// </summary>
    [HarmonyPatch(typeof(AdvancedApothecaryConfig), nameof(AdvancedApothecaryConfig.DoPostConfigureComplete))]
    internal static class AdvancedApothecaryPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.AdvancedApothecary);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.AdvancedApothecary);
    }

    /// <summary>Clothing Refashionator (config class is ClothingAlterationStationConfig).</summary>
    [HarmonyPatch(typeof(ClothingAlterationStationConfig), nameof(ClothingAlterationStationConfig.DoPostConfigureComplete))]
    internal static class ClothingAlterationStationPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.ClothingAlterationStation);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.ClothingAlterationStation);
    }

    /// <summary>
    /// Orbital Data Collection Lab. A normal fabricator (5 kg of ore in, one
    /// Orbital Research Databank out), but automating it means an orbital module
    /// produces research with no duplicant aboard the rocket. Enabled by
    /// default; a player who wants the crewed-rocket requirement back should
    /// turn this one off.
    /// </summary>
    [HarmonyPatch(typeof(OrbitalResearchCenterConfig), nameof(OrbitalResearchCenterConfig.DoPostConfigureComplete))]
    internal static class OrbitalResearchCenterPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.OrbitalResearchCenter);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.OrbitalResearchCenter);
    }

    /// <summary>
    /// Manual Radbolt Generator. Still consumes its uranium (1 kg Uranium Ore →
    /// 0.5 kg Depleted Uranium + 5 radbolts, or Enriched Uranium → 25), so this
    /// is not free radbolts — but it does remove the duplicant labour that is
    /// the building's entire cost over the powered Radbolt Generator. Enabled by
    /// default; turn it off to keep that trade-off.
    ///
    /// Note: this config's DoPostConfigureComplete is declared but empty. That
    /// is still a real, patchable method body, and PatchAll runs at mod load —
    /// long before Assets builds the prefabs — so the patched version is the one
    /// that gets called.
    /// </summary>
    [HarmonyPatch(typeof(ManualHighEnergyParticleSpawnerConfig), nameof(ManualHighEnergyParticleSpawnerConfig.DoPostConfigureComplete))]
    internal static class ManualHighEnergyParticleSpawnerPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.ManualHighEnergyParticleSpawner);
        internal static void Postfix(GameObject go) => Fabricator.MakeAutomatic(go, BuildingIds.ManualHighEnergyParticleSpawner);
    }
}
