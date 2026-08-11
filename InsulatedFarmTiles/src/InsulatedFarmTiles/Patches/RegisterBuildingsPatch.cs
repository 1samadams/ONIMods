using HarmonyLib;

namespace InsulatedFarmTiles
{
    /// <summary>
    /// Puts both tiles on a research node and in the build menu.
    ///
    /// This is a <b>prefix on <c>GeneratedBuildings.LoadGeneratedBuildings</c></b>
    /// and it must stay one. The original mod used a postfix on
    /// <c>Db.Initialize</c>, which is a coin flip:
    ///
    /// <code>
    /// BuildingConfigManager.RegisterBuilding
    ///   -> BuildingDef.PostProcess
    ///        -> Db.Get().TechItems.AddTechItem(PrefabID, ...)
    ///             -> returns NULL if no tech lists this ID yet
    /// </code>
    ///
    /// A building whose ID is not on a tech at the moment it registers never gets
    /// a <c>TechItem</c> and never appears in the research screen. <c>Db.Get()</c>
    /// is a lazy singleton that <c>BuildingDef.PostProcess</c> can itself trigger,
    /// so a <c>Db.Initialize</c> postfix can land in the middle of the
    /// registration sweep. Prefixing the sweep is ordered by construction.
    ///
    /// The plan screen is safe in the same prefix: <c>LoadGeneratedBuildings</c>
    /// prunes plan entries whose <c>BuildingDef</c> is null, but only after its
    /// <c>RegisterBuilding</c> loop, by which point these defs exist.
    /// </summary>
    [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
    internal static class RegisterBuildingsPatch
    {
        private static void Prefix()
        {
            Register(InsulatedFarmTileConfig.Id, InsulatedFarmTileConfig.TechGroup,
                InsulatedFarmTileConfig.PlanCategory, InsulatedFarmTileConfig.PlanSubcategory,
                InsulatedFarmTileConfig.PlanAnchor);

            Register(InsulatedHydroponicFarmConfig.Id, InsulatedHydroponicFarmConfig.TechGroup,
                InsulatedHydroponicFarmConfig.PlanCategory, InsulatedHydroponicFarmConfig.PlanSubcategory,
                InsulatedHydroponicFarmConfig.PlanAnchor);
        }

        private static void Register(string id, string techGroup, string planCategory, string subcategory, string anchor)
        {
            Tech tech = Db.Get().Techs.Get(techGroup);
            if (tech == null)
            {
                // Klei renames tech nodes between updates. Losing the research
                // gate is survivable; a null-reference here would take the whole
                // building registration sweep down with it.
                UnityEngine.Debug.LogWarning(
                    "[InsulatedFarmTiles] Tech '" + techGroup + "' not found, '" + id + "' will not be researchable.");
            }
            else if (!tech.unlockedItemIDs.Contains(id))
            {
                tech.unlockedItemIDs.Add(id);
            }

            // The subcategory and anchor are what keep these next to vanilla Farm
            // Tile and Hydroponic Farm. The two-argument overload of this method
            // defaults the subcategory to "uncategorized", which is what put the
            // original mod's tiles in a stray group at the bottom of the Food tab.
            ModUtil.AddBuildingToPlanScreen(planCategory, id, subcategory, anchor);
        }
    }
}
