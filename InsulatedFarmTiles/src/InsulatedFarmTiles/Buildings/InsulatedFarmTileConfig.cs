using STRINGS;
using TUNING;
using UnityEngine;

namespace InsulatedFarmTiles
{
    /// <summary>
    /// An Insulated Tile that is also a Farm Tile.
    ///
    /// Deliberately a line-for-line mirror of vanilla <c>FarmTileConfig</c> except
    /// where this mod has a reason to differ, which is only: the anim, the
    /// insulation, and TIER3 raw minerals instead of TIER2 farmable materials
    /// (matching the original mod's cost). Everything else -- the plot criteria,
    /// the codex tag, the anim blend, the search terms -- is vanilla's, because
    /// every one of those was missing from the 2019 original and each absence was
    /// a visible defect rather than a design choice.
    /// </summary>
    public class InsulatedFarmTileConfig : IBuildingConfig
    {
        public const string Id = "InsulatedFarmTile";
        public const string DisplayName = "Insulated Farm Tile";
        public const string Description = "An insulated version of a Farm Tile.";

        public const string Effect =
            "A farm tile insulated to help with temperature regulation. Useful for feeding plants "
            + "from outside an otherwise sealed environment.";

        public const string PlanCategory = "Food";
        public const string PlanSubcategory = "farming";

        /// <summary>Placed straight after vanilla Farm Tile in the build menu.</summary>
        public const string PlanAnchor = FarmTileConfig.ID;

        public const string TechGroup = "FinerDining";

        public override BuildingDef CreateBuildingDef()
        {
            // construction_time is 30f, vanilla's. The original mod used 300f,
            // which slowed teardown too: Deconstructable.OnSpawn derives its work
            // time from Def.ConstructionTime * 0.5f when no custom time is set.
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                Id, 1, 1, "insulatedfarmtile_kanim", 100, 30f,
                // TUNING-qualified: `using STRINGS` also defines a BUILDINGS.
                TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER3, MATERIALS.RAW_MINERALS, 1600f,
                BuildLocationRule.Tile, TUNING.BUILDINGS.DECOR.NONE, NOISE_POLLUTION.NONE);

            BuildingTemplates.CreateFoundationTileDef(def);
            def.Floodable = false;
            def.Entombable = false;
            def.Overheatable = false;
            def.ForegroundLayer = Grid.SceneLayer.BuildingBack;
            def.AudioCategory = "HollowMetal";
            def.AudioSize = "small";
            def.BaseTimeUntilRepair = -1f;
            def.SceneLayer = Grid.SceneLayer.TileMain;
            def.ConstructionOffsetFilter = BuildingDef.ConstructionOffsetFilter_OneDown;
            def.PermittedRotations = PermittedRotations.FlipV;
            def.DragBuild = true;
            def.AddSearchTerms(SEARCH_TERMS.FOOD);
            def.AddSearchTerms(SEARCH_TERMS.FARM);
            def.AddSearchTerms(SEARCH_TERMS.TILE);

            TileInsulation.ApplyTo(def);
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            KPrefabID prefabId = go.GetComponent<KPrefabID>();
            prefabId.AddTag(GameTags.CodexCategories.FarmBuilding);

            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(typeof(RequiresFoundation), prefab_tag);

            SimCellOccupier simCellOccupier = go.AddOrGet<SimCellOccupier>();
            simCellOccupier.doReplaceElement = true;
            simCellOccupier.notifyOnMelt = true;

            TileInsulation.AddComponentTo(go);
            go.AddOrGet<TileTemperature>();
            BuildingTemplates.CreateDefaultStorage(go).SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);

            PlantablePlot plantablePlot = go.AddOrGet<PlantablePlot>();
            plantablePlot.occupyingObjectRelativePosition = new Vector3(0f, 1f, 0f);
            plantablePlot.AddDepositTag(GameTags.CropSeed);
            plantablePlot.AddDepositTag(GameTags.WaterSeed);
            plantablePlot.AddAdditionalCriteria(FarmTileConfig.ForbiddenTags);
            plantablePlot.SetFertilizationFlags(true, false);

            go.AddOrGet<CopyBuildingSettings>().copyGroupTag = GameTags.Farm;
            go.AddOrGet<AnimTileable>();
            Prioritizable.AddRef(go);

            prefabId.prefabInitFn += OnPrefabInit;
        }

        /// <summary>
        /// Vanilla re-applies the criteria per instance as well as on the prefab.
        /// Both are kept: <c>PlantablePlot</c> is added by
        /// <c>ConfigureBuildingTemplate</c> for the template and re-resolved for
        /// each placed building, and only the instance copy gates what a Duplicant
        /// can actually plant. Without it, seeds tagged <c>LargeSeed</c> (Wide Farm
        /// Tile crops) and <c>BackwallSeed</c> (Large Backwall Farm crops) go into
        /// a 1x1 plot, which vanilla forbids.
        /// </summary>
        private void OnPrefabInit(GameObject instance)
        {
            instance.AddOrGet<PlantablePlot>().AddAdditionalCriteria(FarmTileConfig.ForbiddenTags);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            // 4 == KBatchedAnimInstanceData.BlendActiveOptions.WaterProof, which is
            // what stops a submerged tile from being tinted by the liquid it sits
            // in. Vanilla FarmTileConfig sets the same literal.
            go.GetComponent<KBatchedAnimController>().initialBlendParameters = 4;
            GeneratedBuildings.RemoveLoopingSounds(go);
            go.GetComponent<KPrefabID>().AddTag(GameTags.FarmTiles);
            FarmTileConfig.SetUpFarmPlotTags(go);
        }
    }
}
