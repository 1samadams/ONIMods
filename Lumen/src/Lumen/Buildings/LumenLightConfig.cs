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
                ResolveAnim(light),
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

            if (!Settings.Instance.IsEnabled(light))
            {
                // The game has its own concept for "registered but not available",
                // and it does exactly what a disabled light needs:
                //   BuildingConfigManager.RegisterBuilding tags the prefab
                //     GameTags.DeprecatedContent
                //   BuildingDef.PostProcess skips AddTechItem entirely (it is
                //     guarded by `if (!Deprecated)`)
                // Combined with LoadGeneratedBuildingsPatch not adding it to a plan
                // category or a tech, the building becomes genuinely unreachable
                // rather than merely unlisted. It still has to register at all:
                // GeneratedBuildings sweeps the assembly for IBuildingConfig types
                // and there is no supported way to opt a type out of that sweep.
                def.Deprecated = true;
            }

            return def;
        }

        /// <summary>
        /// Returns the light's anim if this install actually has it, otherwise a
        /// base-game one.
        ///
        /// BuildingTemplates.CreateBuildingDef does an unchecked
        /// `AnimFiles = new KAnimFile[1] { Assets.GetAnim(anim) }`, and GetAnim
        /// returns null for an anim this install never loaded. The resulting
        /// [null] array does not read as empty, so BuildingLoader.Add2DComponents
        /// sails past its length check and throws inside
        /// KAnimControllerBase.set_AnimFiles -- which aborts RegisterBuilding for
        /// that whole building.
        ///
        /// This bit once: the Panel Light originally reused
        /// glassceilinglight_jelly_green_kanim, which belongs to a building gated
        /// behind DlcManager.DLC5 and is simply not present without that DLC. Every
        /// anim named in LumenLights should be base-game, but this makes a mistake
        /// there cost one wrong-looking fixture instead of a missing building.
        /// </summary>
        private static string ResolveAnim(LumenLight light)
        {
            if (Assets.GetAnim(light.Anim) != null)
            {
                return light.Anim;
            }

            UnityEngine.Debug.LogWarning(
                "[Lumen] " + light.Id + " wants anim '" + light.Anim + "', which this install does not have " +
                "(a DLC-only anim?). Falling back to '" + LumenLights.FallbackAnim + "'.");

            if (Assets.GetAnim(LumenLights.FallbackAnim) == null)
            {
                UnityEngine.Debug.LogError(
                    "[Lumen] Fallback anim '" + LumenLights.FallbackAnim + "' is missing too. " +
                    light.Id + " will fail to register.");
            }

            return LumenLights.FallbackAnim;
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

            // Added after Light2D: the sensor requires it, and derives its whole
            // trigger area from the light's own range and shape.
            LumenMotionSensor sensor = go.AddOrGet<LumenMotionSensor>();
            sensor.extraSensorRadius = Settings.Instance.ExtraSensorRadiusFor(light);
            sensor.lingerSeconds = Settings.Instance.LingerSecondsFor(light);
        }
    }
}
