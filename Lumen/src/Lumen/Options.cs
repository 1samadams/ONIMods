using System.Collections.Generic;
using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace Lumen
{
    /// <summary>
    /// The in-game options screen, via Peter Han's PLib. One category per light,
    /// four settings in each.
    ///
    /// Property names follow the pattern <c>&lt;LightId&gt;&lt;Setting&gt;</c> --
    /// e.g. <c>LumenSpotlightWatts</c> -- and that is load-bearing, not cosmetic:
    /// <see cref="Settings"/> finds each light's four properties by that name, both
    /// to seed the defaults from <see cref="LumenLights"/> and to migrate the old
    /// nested config.json. Adding a sixth light means four properties here, named
    /// to the pattern; <see cref="Settings"/> warns at load if any are missing.
    ///
    /// Every option is [RestartRequired], and that is structural. Enabled decides
    /// whether a building registers as available, Watts is baked into the
    /// BuildingDef, and the sensor values are copied onto the prefab -- all once per
    /// process, while the game builds its prefabs. A value changed mid-session
    /// cannot reach a prefab that already exists.
    ///
    /// SharedConfigLocation (the third ConfigFile argument) keeps the file in the
    /// game's shared mods\config\ directory instead of the mod folder, which Steam
    /// overwrites on every Workshop update.
    ///
    /// The constructor IS the defaults, and must stay that way: PLib's dialog calls
    /// it for its "Reset to default" button and when no file exists yet. It copies
    /// them from <see cref="LumenLights"/> rather than repeating the numbers, so the
    /// per-light values still live in exactly one place.
    /// </summary>
    [ConfigFile("config.json", true, true)]
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Options : SingletonOptions<Options>, IOptions
    {
        // Limits shared by every light. Applied to values read from disk as well as
        // to the screen, so a hand-edited file cannot sneak past them.
        internal const double MinWatts = 0.1, MaxWatts = 100;
        internal const double MinRadius = 0, MaxRadius = 32;
        internal const double MinLinger = 0, MaxLinger = 60;

        private const string EnabledTip =
            "Off hides it from the build menu and the research tree. Copies already built in a save keep working.";
        private const string WattsTip =
            "Power drawn while lit. Zero while dark regardless. Self-heat scales with it.";
        private const string RadiusTip =
            "Detection reach beyond the area it lights, in tiles, ignoring walls. 0 means it triggers only on Duplicants it would light.";
        private const string LingerTip =
            "How long it stays lit after the last Duplicant leaves.";

        // ------------------------------------------------------------------
        private const string Spotlight = "Lumen Spotlight";

        [RestartRequired, Option("Enabled", EnabledTip, Spotlight), JsonProperty]
        public bool LumenSpotlightEnabled { get; set; }

        [RestartRequired, Option("Power (W)", WattsTip, Spotlight, Format = "F1"), Limit(MinWatts, MaxWatts), JsonProperty]
        public float LumenSpotlightWatts { get; set; }

        [RestartRequired, Option("Extra sensor reach (tiles)", RadiusTip, Spotlight, Format = "F0"), Limit(MinRadius, MaxRadius), JsonProperty]
        public float LumenSpotlightExtraSensorRadius { get; set; }

        [RestartRequired, Option("Stays lit (seconds)", LingerTip, Spotlight, Format = "F0"), Limit(MinLinger, MaxLinger), JsonProperty]
        public float LumenSpotlightLingerSeconds { get; set; }

        // ------------------------------------------------------------------
        private const string PanelLight = "Lumen Panel Light";

        [RestartRequired, Option("Enabled", EnabledTip, PanelLight), JsonProperty]
        public bool LumenPanelLightEnabled { get; set; }

        [RestartRequired, Option("Power (W)", WattsTip, PanelLight, Format = "F1"), Limit(MinWatts, MaxWatts), JsonProperty]
        public float LumenPanelLightWatts { get; set; }

        [RestartRequired, Option("Extra sensor reach (tiles)", RadiusTip, PanelLight, Format = "F0"), Limit(MinRadius, MaxRadius), JsonProperty]
        public float LumenPanelLightExtraSensorRadius { get; set; }

        [RestartRequired, Option("Stays lit (seconds)", LingerTip, PanelLight, Format = "F0"), Limit(MinLinger, MaxLinger), JsonProperty]
        public float LumenPanelLightLingerSeconds { get; set; }

        // ------------------------------------------------------------------
        private const string Floodlight = "Lumen Floodlight";

        [RestartRequired, Option("Enabled", EnabledTip, Floodlight), JsonProperty]
        public bool LumenFloodlightEnabled { get; set; }

        [RestartRequired, Option("Power (W)", WattsTip, Floodlight, Format = "F1"), Limit(MinWatts, MaxWatts), JsonProperty]
        public float LumenFloodlightWatts { get; set; }

        [RestartRequired, Option("Extra sensor reach (tiles)", RadiusTip, Floodlight, Format = "F0"), Limit(MinRadius, MaxRadius), JsonProperty]
        public float LumenFloodlightExtraSensorRadius { get; set; }

        [RestartRequired, Option("Stays lit (seconds)", LingerTip, Floodlight, Format = "F0"), Limit(MinLinger, MaxLinger), JsonProperty]
        public float LumenFloodlightLingerSeconds { get; set; }

        // ------------------------------------------------------------------
        private const string FloorLamp = "Lumen Floor Lamp";

        [RestartRequired, Option("Enabled", EnabledTip, FloorLamp), JsonProperty]
        public bool LumenFloorLampEnabled { get; set; }

        [RestartRequired, Option("Power (W)", WattsTip, FloorLamp, Format = "F1"), Limit(MinWatts, MaxWatts), JsonProperty]
        public float LumenFloorLampWatts { get; set; }

        [RestartRequired, Option("Extra sensor reach (tiles)", RadiusTip, FloorLamp, Format = "F0"), Limit(MinRadius, MaxRadius), JsonProperty]
        public float LumenFloorLampExtraSensorRadius { get; set; }

        [RestartRequired, Option("Stays lit (seconds)", LingerTip, FloorLamp, Format = "F0"), Limit(MinLinger, MaxLinger), JsonProperty]
        public float LumenFloorLampLingerSeconds { get; set; }

        // ------------------------------------------------------------------
        private const string SentryLight = "Lumen Sentry Light";

        [RestartRequired, Option("Enabled", EnabledTip, SentryLight), JsonProperty]
        public bool LumenSentryLightEnabled { get; set; }

        [RestartRequired, Option("Power (W)", WattsTip, SentryLight, Format = "F1"), Limit(MinWatts, MaxWatts), JsonProperty]
        public float LumenSentryLightWatts { get; set; }

        [RestartRequired, Option("Extra sensor reach (tiles)", RadiusTip, SentryLight, Format = "F0"), Limit(MinRadius, MaxRadius), JsonProperty]
        public float LumenSentryLightExtraSensorRadius { get; set; }

        [RestartRequired, Option("Stays lit (seconds)", LingerTip, SentryLight, Format = "F0"), Limit(MinLinger, MaxLinger), JsonProperty]
        public float LumenSentryLightLingerSeconds { get; set; }

        public Options()
        {
            foreach (LumenLight light in LumenLights.All)
            {
                Settings.Write(this, light, new LightSettings
                {
                    Enabled = true,
                    Watts = light.Watts,
                    ExtraSensorRadius = light.ExtraSensorRadius,
                    LingerSeconds = light.LingerSeconds,
                });
            }
        }

        /// <summary>
        /// Settings is the only thing that assigns the singleton, and the setter on
        /// SingletonOptions is protected -- so it has to be reached from in here.
        /// </summary>
        internal static void SetInstance(Options options) => Instance = options;

        public void OnOptionsChanged() => Settings.OnOptionsChanged();

        // No dynamically generated entries; every option is a declared property.
        public IEnumerable<IOptionsEntry> CreateOptions() => null;
    }
}
