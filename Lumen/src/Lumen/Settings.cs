using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Lumen
{
    /// <summary>Per-building overrides read from config.json.</summary>
    public class LightSettings
    {
        /// <summary>
        /// When false the building is still registered with the game, but is not added
        /// to the build menu or to any tech, so it cannot be built. It has to stay
        /// registered: GeneratedBuildings enumerates config types out of the assembly
        /// and there is no supported way to opt a type out of that sweep. Buildings
        /// already placed in an existing save keep working.
        /// </summary>
        public bool Enabled = true;

        /// <summary>Null means "use the building's default".</summary>
        public float? Watts;

        /// <summary>
        /// Extra detection reach beyond the lit area. Renamed from the old
        /// SensorRadius on purpose: that field meant a plain sphere around the
        /// fixture, which this version no longer uses. The rename means an existing
        /// config.json written against the old meaning is ignored rather than
        /// silently reinstating the old broken behaviour -- Newtonsoft drops unknown
        /// fields, so those files fall back to the new defaults.
        /// </summary>
        public float? ExtraSensorRadius;

        public float? LingerSeconds;
    }

    /// <summary>
    /// config.json, loaded once at mod load.
    ///
    /// Must be read before the building definitions are created, because
    /// CreateBuildingDef bakes the wattage into BuildingDef.EnergyConsumptionWhenActive
    /// and EnergyConsumer copies that into BaseWattageRating at prefab init. Loading it
    /// late would silently give every light its default wattage.
    ///
    /// A malformed file falls back to defaults rather than stopping the mod from
    /// loading -- a typo in a config file should not cost the player their save's
    /// buildings.
    /// </summary>
    public class Settings
    {
        public Dictionary<string, LightSettings> Lights = new Dictionary<string, LightSettings>();

        private static Settings instance;

        public static Settings Instance => instance ?? (instance = new Settings());

        public static void Load(string modPath)
        {
            string path = Path.Combine(modPath, "config.json");

            if (!File.Exists(path))
            {
                UnityEngine.Debug.Log("[Lumen] No config.json at " + path + ", using defaults.");
                instance = new Settings();
                return;
            }

            try
            {
                Settings loaded = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path));
                instance = loaded ?? new Settings();

                if (instance.Lights == null)
                {
                    instance.Lights = new Dictionary<string, LightSettings>();
                }

                // An unknown key is almost always a typo in a building name, which
                // would otherwise fail silently as "my setting did nothing".
                foreach (string key in instance.Lights.Keys)
                {
                    if (LookUp(key) == null)
                    {
                        UnityEngine.Debug.LogWarning(
                            "[Lumen] config.json has an entry for unknown light '" + key + "', ignoring it.");
                    }
                }

                UnityEngine.Debug.Log("[Lumen] Loaded config from " + path);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning(
                    "[Lumen] config.json could not be read (" + e.Message + "), using defaults.");
                instance = new Settings();
            }
        }

        private static LumenLight LookUp(string id)
        {
            foreach (LumenLight light in LumenLights.All)
            {
                if (light.Id == id)
                {
                    return light;
                }
            }

            return null;
        }

        private LightSettings For(LumenLight light)
        {
            LightSettings settings;
            return Lights.TryGetValue(light.Id, out settings) ? settings : null;
        }

        public bool IsEnabled(LumenLight light)
        {
            LightSettings settings = For(light);
            return settings == null || settings.Enabled;
        }

        public float WattsFor(LumenLight light)
        {
            LightSettings settings = For(light);
            return settings?.Watts ?? light.Watts;
        }

        public float ExtraSensorRadiusFor(LumenLight light)
        {
            LightSettings settings = For(light);
            return settings?.ExtraSensorRadius ?? light.ExtraSensorRadius;
        }

        public float LingerSecondsFor(LumenLight light)
        {
            LightSettings settings = For(light);
            return settings?.LingerSeconds ?? light.LingerSeconds;
        }
    }
}
