using HarmonyLib;

namespace Lumen
{
    /// <summary>
    /// Puts the Lumen lights in the build menu and on the research tree.
    ///
    /// Defining an IBuildingConfig is enough to make the game *build* the prefab --
    /// GeneratedBuildings.LoadGeneratedBuildings sweeps every loaded assembly for
    /// them -- but a building nobody has added to a plan category and a tech is
    /// registered and unreachable. Both lists are plain data, so both are appended to
    /// rather than patched into.
    ///
    /// A PREFIX on LoadGeneratedBuildings, not a Db.Initialize postfix. The tech
    /// association has to exist before the building is registered, because of this
    /// chain (verified against build 744825):
    ///
    ///   BuildingConfigManager.RegisterBuilding
    ///     -> BuildingDef.PostProcess
    ///          -> Db.Get().TechItems.AddTechItem(PrefabID, ...)
    ///               -> GetTechFromItemID(id) searches every tech's unlockedItemIDs
    ///                  and AddTechItem RETURNS NULL if it finds nothing
    ///
    /// So a building whose ID is not yet on a tech when it registers never gets a
    /// TechItem, and never appears in the research screen. Db.Initialize is a lazy
    /// singleton that can be triggered from inside that very call, which makes a
    /// postfix on it a coin flip; this prefix is ordered by construction.
    ///
    /// The plan screen is safe here too. LoadGeneratedBuildings prunes plan entries
    /// whose BuildingDef is null, but only after its RegisterBuilding loop has run,
    /// by which point these defs exist.
    /// </summary>
    [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
    public static class LoadGeneratedBuildingsPatch
    {
        public static void Prefix()
        {
            Tech tech = Db.Get().Techs.Get(LumenLights.TechId);
            if (tech == null)
            {
                UnityEngine.Debug.LogWarning(
                    "[Lumen] Tech '" + LumenLights.TechId + "' not found, so the lights would be " +
                    "unresearchable and invisible. The game's tech tree has probably changed.");
                return;
            }

            foreach (LumenLight light in LumenLights.All)
            {
                if (!Settings.Instance.IsEnabled(light))
                {
                    UnityEngine.Debug.Log(
                        "[Lumen] " + light.Id + " is disabled in config.json; not adding it to the build menu.");
                    continue;
                }

                ModUtil.AddBuildingToPlanScreen(
                    LumenLights.PlanCategory,
                    light.Id,
                    LumenLights.PlanSubcategory);

                tech.AddUnlockedItemIDs(light.Id);
            }
        }
    }
}
