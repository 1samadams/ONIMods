using STRINGS;
using TUNING;
using UnityEngine;

namespace InsulatedFarmTiles
{
    /// <summary>
    /// An Insulated Tile that is also a Hydroponic Farm Tile. Mirrors vanilla
    /// <c>HydroponicFarmConfig</c> on the same terms as
    /// <see cref="InsulatedFarmTileConfig"/> does <c>FarmTileConfig</c>.
    ///
    /// Materials stay RAW_MINERALS rather than vanilla's ALL_METALS: this is the
    /// original mod's choice and the right one here, because insulation quality
    /// comes from the build material and metals are the worst insulators in the
    /// game.
    /// </summary>
    public class InsulatedHydroponicFarmConfig : IBuildingConfig
    {
        public const string Id = "InsulatedHydroponicFarm";
        public const string DisplayName = "Insulated Hydroponic Farm Tile";
        public const string Description = "An insulated version of a Hydroponic Farm Tile.";

        public const string Effect =
            "A hydroponic farm tile insulated to help with temperature regulation. Plants are watered "
            + "by pipe, so the room around them can stay sealed.";

        public const string PlanCategory = "Food";
        public const string PlanSubcategory = "farming";

        /// <summary>Placed straight after vanilla Hydroponic Farm in the build menu.</summary>
        public const string PlanAnchor = HydroponicFarmConfig.ID;

        public const string TechGroup = "FinerDining";

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                Id, 1, 1, "insulatedfarmtilehydroponic_kanim", 100, 30f,
                // TUNING-qualified: `using STRINGS` also defines a BUILDINGS.
                TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER3, MATERIALS.RAW_MINERALS, 1600f,
                BuildLocationRule.Tile, TUNING.BUILDINGS.DECOR.PENALTY.TIER0, NOISE_POLLUTION.NONE);

            BuildingTemplates.CreateFoundationTileDef(def);
            def.Floodable = false;
            def.Entombable = false;
            def.Overheatable = false;
            def.UseStructureTemperature = false;
            def.AudioCategory = "Metal";
            def.AudioSize = "small";
            def.BaseTimeUntilRepair = -1f;
            def.SceneLayer = Grid.SceneLayer.TileMain;
            def.ConstructionOffsetFilter = BuildingDef.ConstructionOffsetFilter_OneDown;
            def.PermittedRotations = PermittedRotations.FlipV;
            def.InputConduitType = ConduitType.Liquid;
            def.UtilityInputOffset = new CellOffset(0, 0);
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

            SimCellOccupier simCellOccupier = go.AddOrGet<SimCellOccupier>();
            simCellOccupier.doReplaceElement = true;
            simCellOccupier.notifyOnMelt = true;

            TileInsulation.AddComponentTo(go);
            go.AddOrGet<TileTemperature>();

            ConduitConsumer conduitConsumer = go.AddOrGet<ConduitConsumer>();
            conduitConsumer.conduitType = ConduitType.Liquid;
            conduitConsumer.consumptionRate = 1f;
            conduitConsumer.capacityKG = 5f;
            conduitConsumer.capacityTag = GameTags.Liquid;
            conduitConsumer.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;

            go.AddOrGet<Storage>();

            PlantablePlot plantablePlot = go.AddOrGet<PlantablePlot>();
            plantablePlot.AddDepositTag(GameTags.CropSeed);
            plantablePlot.AddDepositTag(GameTags.WaterSeed);
            plantablePlot.AddAdditionalCriteria(FarmTileConfig.ForbiddenTags);
            plantablePlot.occupyingObjectRelativePosition.y = 1f;
            plantablePlot.SetFertilizationFlags(true, true);

            go.AddOrGet<CopyBuildingSettings>().copyGroupTag = GameTags.Farm;
            BuildingTemplates.CreateDefaultStorage(go).SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            go.AddOrGet<PlanterBox>();
            go.AddOrGet<AnimTileable>();
            go.AddOrGet<DropAllWorkable>();
            Prioritizable.AddRef(go);

            prefabId.prefabInitFn += OnPrefabInit;
        }

        /// <summary>
        /// See <see cref="InsulatedFarmTileConfig"/> for the plot criteria. The two
        /// blend values are vanilla's and are what keep a hydroponic tile from
        /// being drawn as though it were flooded -- the tile is full of piped
        /// water by design, so it opts out of the liquid visibility layer and
        /// declares itself waterproof.
        /// </summary>
        private void OnPrefabInit(GameObject instance)
        {
            instance.AddOrGet<PlantablePlot>().AddAdditionalCriteria(FarmTileConfig.ForbiddenTags);

            KBatchedAnimController anim = instance.GetComponent<KBatchedAnimController>();
            anim.SetBlendValue(KBatchedAnimInstanceData.BlendActiveOptions.LiquidVisibilityLayer, false);
            anim.SetBlendValue(KBatchedAnimInstanceData.BlendActiveOptions.WaterProof, true);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            FarmTileConfig.SetUpFarmPlotTags(go);
            go.GetComponent<KPrefabID>().AddTag(GameTags.FarmTiles);
            go.GetComponent<RequireInputs>().requireConduitHasMass = false;
        }
    }
}
