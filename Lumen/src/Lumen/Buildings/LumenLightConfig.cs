using TUNING;
using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// One building implementation shared by all five Lumen lights. Everything that
    /// varies between them lives in the <see cref="LumenLight"/> returned by
    /// <see cref="Light"/>.
    ///
    /// Modelled directly on CeilingLightConfig / FloorLampConfig so the buildings
    /// behave like vanilla lights in every respect the mod does not deliberately
    /// change. The two deliberate changes are the wattage and the
    /// <see cref="LumenMotionSensor"/>.
    ///
    /// Abstract on purpose: GeneratedBuildings.LoadGeneratedBuildings enumerates every
    /// non-abstract IBuildingConfig in every loaded assembly and calls
    /// Activator.CreateInstance on it, so this base is skipped and only the five
    /// concrete subclasses are registered.
    /// </summary>
    public abstract class LumenLightConfig : IBuildingConfig
    {
        /// <summary>
        /// CeilingLightConfig: 10 W of power, 0.5 kW of self heat. Reused so Lumen
        /// fixtures sit on the same heat-per-watt curve as Klei's.
        /// </summary>
        private const float VanillaKilowattsOfHeatPerWatt = 0.05f;

        protected abstract LumenLight Light { get; }

        public override BuildingDef CreateBuildingDef()
        {
            LumenLight light = Light;

            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                light.Id,
                light.Width,
                light.Height,
                light.Anim,
                hitpoints: 10,
                construction_time: 10f,
                construction_mass: light.Mass,
                construction_materials: light.Materials,
                melting_point: 800f,
                build_location_rule: light.BuildLocation,
                decor: light.Decor,
                noise: NOISE_POLLUTION.NONE);

            float watts = Settings.Instance.WattsFor(light);

            def.RequiresPowerInput = true;

            // The whole point of the mod. EnergyConsumer copies this into
            // BaseWattageRating at OnPrefabInit, so it drives both the build-menu
            // number and the real draw.
            def.EnergyConsumptionWhenActive = watts;

            // Heat is kept proportional to power at vanilla's own ratio: a Ceiling
            // Light burns 10 W and emits 0.5 kW, i.e. 0.05 kW per watt. Deriving it
            // rather than picking a number means raising Watts in config.json also
            // makes the fixture hotter, instead of quietly handing out free cooling.
            def.SelfHeatKilowattsWhenActive = watts * VanillaKilowattsOfHeatPerWatt;

            def.ViewMode = OverlayModes.Light.ID;
            def.AudioCategory = "Metal";

            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.GetComponent<KPrefabID>().AddTag(GameTags.LightSource);
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            LumenLight light = Light;

            LightShapePreview preview = go.AddComponent<LightShapePreview>();
            preview.lux = light.Lux;
            preview.radius = light.Range;
            preview.shape = light.Shape;
            preview.offset = new CellOffset((int)light.Offset.x, (int)light.Offset.y);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            LumenLight light = Light;

            go.AddOrGet<LoopingSounds>();

            // FloorLampConfig adds this explicitly and CeilingLightConfig relies on
            // RequiresPowerInput to supply it. Adding it here covers both.
            go.AddOrGet<EnergyConsumer>();

            Light2D light2D = go.AddOrGet<Light2D>();
            light2D.overlayColour = LIGHT2D.LIGHT_OVERLAY;
            light2D.Color = light.LightColour;
            light2D.Range = light.Range;
            light2D.Angle = light.Angle;
            light2D.Direction = LIGHT2D.DEFAULT_DIRECTION;
            light2D.Offset = light.Offset;
            light2D.shape = light.Shape;
            light2D.drawOverlay = true;
            light2D.Lux = light.Lux;

            // Vanilla state machine: plays the "on"/"off" anims and flips
            // Operational.SetActive. Left entirely alone -- the motion sensor works
            // by making IsOperational false, which this reacts to on its own.
            go.AddOrGetDef<LightController.Def>();

            LumenTint tint = go.AddOrGet<LumenTint>();
            tint.colour = light.Tint;

            LumenMotionSensor sensor = go.AddOrGet<LumenMotionSensor>();
            sensor.sensorRadius = Settings.Instance.SensorRadiusFor(light);
            sensor.lingerSeconds = Settings.Instance.LingerSecondsFor(light);
        }
    }
}
