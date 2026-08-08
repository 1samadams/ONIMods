using TUNING;
using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// The five Lumen lights, as data. Order here is the order they appear in the
    /// build menu.
    ///
    /// Balance note: every one of these is 1 W, which would be absurd for a
    /// conventional light -- but they are only lit while a duplicant is standing
    /// under them, so they are useless for the two things cheap light would
    /// otherwise trivialise (growing Bristle Blossoms and lighting an empty base).
    /// The motion sensor is what pays for the wattage. Raising Watts in config.json
    /// without also disabling the sensor is the wrong dial to turn.
    /// </summary>
    public static class LumenLights
    {
        private static readonly Vector2 CeilingOffset = LIGHT2D.CEILINGLIGHT_OFFSET;
        private static readonly Vector2 FloorOffset = LIGHT2D.FLOORLAMP_OFFSET;

        // Symbol names dumped from the builds at runtime; they are not readable by
        // decompiling Assembly-CSharp. ceilinglight_kanim has exactly one body part
        // (temp_base), so the lens is its bloom plus its off-state sprite.
        private static readonly string[] CeilingLensSymbols = { "generator_light_bloom", "light_off" };
        private static readonly string[] FloorLampLensSymbols = { "light", "beam", "shade" };

        public static readonly LumenLight Spotlight = new LumenLight
        {
            Id = "LumenSpotlight",
            Name = "Lumen Spotlight",
            Description =
                "A tight, cheap work light that only draws power while somebody is under it.",
            Effect =
                "Lights a small area <b>only while a Duplicant is nearby</b>, then switches itself off. " +
                "Duplicants working in lit tiles receive the usual light work speed bonus.",
            Width = 1,
            Height = 1,
            Anim = "ceilinglight_kanim",
            BuildLocation = BuildLocationRule.OnCeiling,
            Materials = MATERIALS.ALL_METALS,
            Mass = BUILDINGS.CONSTRUCTION_MASS_KG.TIER0,
            Tint = new Color32(214, 220, 228, 255),
            LensTint = new Color32(255, 196, 120, 255),
            LensSymbols = CeilingLensSymbols,
            AnimScale = 0.85f,
            Shape = LightShape.Cone,
            Range = 4f,
            Angle = LIGHT2D.CEILINGLIGHT_ANGLE,
            Lux = LIGHT2D.CEILINGLIGHT_LUX,
            LightColour = LIGHT2D.LIGHT_YELLOW,
            Offset = CeilingOffset,
            Decor = BUILDINGS.DECOR.NONE,
            Watts = 1f,
            ExtraSensorRadius = 0f,
            LingerSeconds = 5f,
            Aimable = true,
        };

        public static readonly LumenLight PanelLight = new LumenLight
        {
            Id = "LumenPanelLight",
            Name = "Lumen Panel Light",
            Description =
                "A broad glass panel for corridors and workrooms. Dark until someone needs it.",
            Effect =
                "Lights a wide area <b>only while a Duplicant is nearby</b>, then switches itself off.",
            Width = 1,
            Height = 1,
            // NOT glassceilinglight_jelly_green_kanim. That anim belongs to the Glass
            // Ceiling Light, whose config declares GetRequiredDlcIds() = DlcManager.DLC5,
            // so it is absent on installs without DLC5 -- Assets.GetAnim returns null
            // and BuildingLoader NREs. Only base-game anims are safe here.
            Anim = "ceilinglight_kanim",
            BuildLocation = BuildLocationRule.OnCeiling,
            Materials = MATERIALS.GLASSES,
            Mass = BUILDINGS.CONSTRUCTION_MASS_KG.TIER1,
            Tint = new Color32(214, 220, 228, 255),
            LensTint = new Color32(198, 230, 255, 255),
            LensSymbols = CeilingLensSymbols,
            AnimScale = 1f,
            Shape = LightShape.Cone,
            Range = 8f,
            Angle = LIGHT2D.CEILINGLIGHT_ANGLE,
            Lux = LIGHT2D.CEILINGLIGHT_LUX,
            LightColour = LIGHT2D.GLASSCEILINGLIGHT_GREEN,
            Offset = CeilingOffset,
            Decor = BUILDINGS.DECOR.BONUS.TIER1,
            Watts = 1f,
            ExtraSensorRadius = 0f,
            LingerSeconds = 5f,
            Aimable = true,
        };

        public static readonly LumenLight Floodlight = new LumenLight
        {
            Id = "LumenFloodlight",
            Name = "Lumen Floodlight",
            Description =
                "A high-output fixture for large rooms, built from refined metal.",
            Effect =
                "Lights a very large area <b>only while a Duplicant is nearby</b>, then switches itself off.",
            Width = 1,
            Height = 1,
            Anim = "ceilinglight_kanim",
            BuildLocation = BuildLocationRule.OnCeiling,
            Materials = MATERIALS.REFINED_METALS,
            Mass = BUILDINGS.CONSTRUCTION_MASS_KG.TIER2,
            Tint = new Color32(214, 220, 228, 255),
            LensTint = new Color32(120, 220, 255, 255),
            LensSymbols = CeilingLensSymbols,
            AnimScale = 1.2f,
            Shape = LightShape.Cone,
            Range = 12f,
            Angle = LIGHT2D.CEILINGLIGHT_ANGLE,
            Lux = 2400,
            LightColour = LIGHT2D.LIGHT_BLUE,
            Offset = CeilingOffset,
            Decor = BUILDINGS.DECOR.NONE,
            Watts = 1f,
            ExtraSensorRadius = 0f,
            LingerSeconds = 8f,
            Aimable = true,
        };

        public static readonly LumenLight FloorLamp = new LumenLight
        {
            Id = "LumenFloorLamp",
            Name = "Lumen Floor Lamp",
            Description =
                "A free-standing lamp for rooms with no ceiling to mount to.",
            Effect =
                "Lights the area around it <b>only while a Duplicant is nearby</b>, then switches itself off.",
            Width = 1,
            Height = 2,
            Anim = "floorlamp_kanim",
            BuildLocation = BuildLocationRule.OnFloor,
            Materials = MATERIALS.ALL_METALS,
            Mass = BUILDINGS.CONSTRUCTION_MASS_KG.TIER1,
            Tint = new Color32(236, 232, 224, 255),
            LensTint = new Color32(255, 214, 150, 255),
            LensSymbols = FloorLampLensSymbols,
            AnimScale = 1f,
            Shape = LightShape.Circle,
            Range = 5f,
            Angle = LIGHT2D.FLOORLAMP_ANGLE,
            Lux = 1400,
            LightColour = LIGHT2D.LIGHT_YELLOW,
            Offset = FloorOffset,
            Decor = BUILDINGS.DECOR.BONUS.TIER1,
            Watts = 1f,
            ExtraSensorRadius = 0f,
            LingerSeconds = 5f,
        };

        public static readonly LumenLight SentryLight = new LumenLight
        {
            Id = "LumenSentryLight",
            Name = "Lumen Sentry Light",
            Description =
                "An over-sensitive fixture that sees Duplicants coming and lights the way ahead of them.",
            Effect =
                "Detects Duplicants far beyond the area it lights, so corridors are already bright " +
                "on arrival. Stays lit longer than other Lumen fixtures after the last Duplicant leaves.",
            Width = 1,
            Height = 1,
            Anim = "ceilinglight_kanim",
            BuildLocation = BuildLocationRule.OnCeiling,
            Materials = MATERIALS.REFINED_METALS,
            Mass = BUILDINGS.CONSTRUCTION_MASS_KG.TIER2,
            Tint = new Color32(214, 220, 228, 255),
            LensTint = new Color32(140, 255, 170, 255),
            LensSymbols = CeilingLensSymbols,
            AnimScale = 1f,
            Shape = LightShape.Cone,
            Range = 8f,
            Angle = LIGHT2D.CEILINGLIGHT_ANGLE,
            Lux = LIGHT2D.CEILINGLIGHT_LUX,
            LightColour = LIGHT2D.LIGHT_YELLOW,
            Offset = CeilingOffset,
            Decor = BUILDINGS.DECOR.NONE,
            Watts = 1f,
            ExtraSensorRadius = 12f,
            LingerSeconds = 10f,
            Aimable = true,
        };

        /// <summary>
        /// Used when a light's own anim is missing from this install. Must be a
        /// base-game anim with no DLC gating -- ceilinglight_kanim is loaded by
        /// CeilingLightConfig, which has no GetRequiredDlcIds override.
        /// </summary>
        public const string FallbackAnim = "ceilinglight_kanim";

        /// <summary>
        /// Whether a prefab name belongs to this mod. Every Lumen ID starts with
        /// "Lumen", and the game derives the preview and under-construction prefab
        /// names from the ID ("<c>&lt;ID&gt;Preview</c>"), so a prefix test covers all
        /// three variants of a building.
        /// </summary>
        public static bool IsLumenPrefab(string prefabName)
        {
            return prefabName != null && prefabName.StartsWith("Lumen");
        }

        /// <summary>Build-menu order.</summary>
        public static readonly LumenLight[] All =
        {
            Spotlight,
            PanelLight,
            Floodlight,
            FloorLamp,
            SentryLight,
        };

        /// <summary>
        /// The tech that unlocks the Duplicant Motion Sensor. Verified in
        /// Database.Techs: new Tech("LogicControl", { ..., "LogicDuplicantSensor" }).
        /// </summary>
        public const string TechId = "LogicControl";

        /// <summary>Build menu placement, matching every vanilla light.</summary>
        public const string PlanCategory = "Furniture";
        public const string PlanSubcategory = "lights";
    }
}
