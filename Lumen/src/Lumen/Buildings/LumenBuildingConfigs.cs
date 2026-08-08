namespace Lumen
{
    // The five registered buildings. Each is just a name that GeneratedBuildings can
    // instantiate plus a pointer at its data; all behaviour is in LumenLightConfig.
    //
    // ID is exposed as a const on each because that is the vanilla convention
    // (CeilingLightConfig.ID and friends), and the tech / plan-screen wiring reads it.

    public class LumenSpotlightConfig : LumenLightConfig
    {
        public const string ID = "LumenSpotlight";
        protected override LumenLight Light => LumenLights.Spotlight;
    }

    public class LumenPanelLightConfig : LumenLightConfig
    {
        public const string ID = "LumenPanelLight";
        protected override LumenLight Light => LumenLights.PanelLight;
    }

    public class LumenFloodlightConfig : LumenLightConfig
    {
        public const string ID = "LumenFloodlight";
        protected override LumenLight Light => LumenLights.Floodlight;
    }

    public class LumenFloorLampConfig : LumenLightConfig
    {
        public const string ID = "LumenFloorLamp";
        protected override LumenLight Light => LumenLights.FloorLamp;
    }

    public class LumenSentryLightConfig : LumenLightConfig
    {
        public const string ID = "LumenSentryLight";
        protected override LumenLight Light => LumenLights.SentryLight;
    }
}
