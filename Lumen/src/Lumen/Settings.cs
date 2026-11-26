using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace Lumen
{
    /// <summary>One light's resolved settings.</summary>
    public sealed class LightSettings
    {
        /// <summary>
        /// When false the building is marked BuildingDef.Deprecated and is added to
        /// neither a plan category nor a tech, which is the game's own way of saying
        /// "registered but not available" -- it gets GameTags.DeprecatedContent and
        /// BuildingDef.PostProcess skips creating a TechItem for it.
        ///
        /// It has to stay registered: GeneratedBuildings enumerates config types out
        /// of the assembly and there is no supported way to opt a type out of that
        /// sweep. Buildings already placed in an existing save keep working.
        /// </summary>
        public bool Enabled;
        public float Watts;

        /// <summary>
        /// Extra detection reach beyond the lit area. Renamed from the old
        /// SensorRadius on purpose: that field meant a plain sphere around the
        /// fixture, which this version no longer uses.
        /// </summary>
        public float ExtraSensorRadius;
        public float LingerSeconds;
    }

    /// <summary>
    /// Facade over the PLib <see cref="Options"/>. The building code only ever asks
    /// <c>Settings.Instance.WattsFor(light)</c> and friends, so moving from a
    /// hand-parsed config.json to an options screen did not touch any of it.
    ///
    /// Must be loaded before the building definitions are created, because
    /// CreateBuildingDef bakes the wattage into BuildingDef.EnergyConsumptionWhenActive
    /// and EnergyConsumer copies that into BaseWattageRating at prefab init. Loading it
    /// late would silently give every light its default wattage.
    ///
    /// Values are a snapshot taken at load and are deliberately NOT refreshed by
    /// OnOptionsChanged: every option is [RestartRequired], so the snapshot is an
    /// honest record of what the buildings in this session were actually built with.
    /// </summary>
    public sealed class Settings
    {
        private const string LegacyFileName = "config.json";

        // Property-name suffixes on Options; the prefix is the light's Id.
        private const string EnabledSuffix = "Enabled";
        private const string WattsSuffix = "Watts";
        private const string RadiusSuffix = "ExtraSensorRadius";
        private const string LingerSuffix = "LingerSeconds";

        private static Settings instance;

        private readonly Dictionary<string, LightSettings> lights;

        private Settings(Dictionary<string, LightSettings> lights)
        {
            this.lights = lights;
        }

        /// <summary>
        /// Never null. Before Load runs -- which should not happen, but costs nothing
        /// to survive -- this is the built-in defaults.
        /// </summary>
        public static Settings Instance => instance ?? (instance = Snapshot(new Options()));

        public static void Load(string modPath)
        {
            ValidateOptionsShape();

            Options options = POptions.ReadSettings<Options>();

            if (options == null)
            {
                // No file yet: a fresh install, or the first run after upgrading from
                // the config.json-in-the-mod-folder era. Seed from the old file if it
                // is there, then write it out so this only ever happens once.
                options = new Options();
                MigrateLegacyConfig(modPath, options);

                try
                {
                    POptions.WriteSettings(options);
                }
                catch (Exception e)
                {
                    // Non-fatal: the values are in memory and the mod works. Only
                    // persistence is lost.
                    LogWarning("could not write the options file; using these values this session only. " + e.Message);
                }
            }

            Options.SetInstance(options);
            instance = Snapshot(options);
            Log("options loaded from " + POptions.GetConfigFilePath(typeof(Options)));
        }

        /// <summary>
        /// Called by PLib when the player saves the options screen. Everything is
        /// [RestartRequired], so there is nothing to re-apply; this only refreshes
        /// the singleton the screen reads from.
        /// </summary>
        public static void OnOptionsChanged()
        {
            Options.SetInstance(POptions.ReadSettings<Options>() ?? new Options());
            Log("options changed; restart the game for them to take effect.");
        }

        public bool IsEnabled(LumenLight light) => For(light).Enabled;

        public float WattsFor(LumenLight light) => For(light).Watts;

        public float ExtraSensorRadiusFor(LumenLight light) => For(light).ExtraSensorRadius;

        public float LingerSecondsFor(LumenLight light) => For(light).LingerSeconds;

        private LightSettings For(LumenLight light)
        {
            LightSettings settings;
            return lights.TryGetValue(light.Id, out settings)
                ? settings
                : new LightSettings
                {
                    Enabled = true,
                    Watts = light.Watts,
                    ExtraSensorRadius = light.ExtraSensorRadius,
                    LingerSeconds = light.LingerSeconds,
                };
        }

        // ------------------------------------------------------------------
        // Options <-> LightSettings, by property name.
        // ------------------------------------------------------------------

        private static Settings Snapshot(Options options)
        {
            var map = new Dictionary<string, LightSettings>();

            foreach (LumenLight light in LumenLights.All)
            {
                map[light.Id] = Read(options, light);
            }

            return new Settings(map);
        }

        /// <summary>
        /// Reads one light's values, falling back to the light's own default for any
        /// missing property and clamping floats to their [Limit]. The clamp matters
        /// for values read from disk: PLib applies the limits on its screen, not to a
        /// hand-edited file.
        /// </summary>
        private static LightSettings Read(Options options, LumenLight light)
        {
            return new LightSettings
            {
                Enabled = GetBool(options, light.Id + EnabledSuffix, true),
                Watts = GetFloat(options, light.Id + WattsSuffix, light.Watts),
                ExtraSensorRadius = GetFloat(options, light.Id + RadiusSuffix, light.ExtraSensorRadius),
                LingerSeconds = GetFloat(options, light.Id + LingerSuffix, light.LingerSeconds),
            };
        }

        /// <summary>Writes one light's values onto the options object.</summary>
        internal static void Write(Options options, LumenLight light, LightSettings values)
        {
            Set(options, light.Id + EnabledSuffix, values.Enabled);
            Set(options, light.Id + WattsSuffix, values.Watts);
            Set(options, light.Id + RadiusSuffix, values.ExtraSensorRadius);
            Set(options, light.Id + LingerSuffix, values.LingerSeconds);
        }

        private static PropertyInfo Property(string name, Type type)
        {
            PropertyInfo property = typeof(Options).GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            return property != null && property.PropertyType == type ? property : null;
        }

        private static bool GetBool(Options options, string name, bool fallback)
        {
            PropertyInfo property = Property(name, typeof(bool));
            return property != null ? (bool)property.GetValue(options, null) : fallback;
        }

        private static float GetFloat(Options options, string name, float fallback)
        {
            PropertyInfo property = Property(name, typeof(float));
            if (property == null)
            {
                return fallback;
            }

            float value = (float)property.GetValue(options, null);
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return fallback;
            }

            LimitAttribute limit = property.GetCustomAttribute<LimitAttribute>();
            return limit != null ? limit.ClampToRange(value) : value;
        }

        private static void Set(Options options, string name, object value)
        {
            Property(name, value.GetType())?.SetValue(options, value, null);
        }

        /// <summary>
        /// A light added to <see cref="LumenLights.All"/> without its four Options
        /// properties would quietly run on defaults with no way to change them. Say so.
        /// </summary>
        private static void ValidateOptionsShape()
        {
            foreach (LumenLight light in LumenLights.All)
            {
                if (Property(light.Id + EnabledSuffix, typeof(bool)) == null)
                {
                    LogWarning("no option '" + light.Id + EnabledSuffix + "'; " + light.Id + " cannot be configured.");
                }

                foreach (string suffix in new[] { WattsSuffix, RadiusSuffix, LingerSuffix })
                {
                    if (Property(light.Id + suffix, typeof(float)) == null)
                    {
                        LogWarning("no option '" + light.Id + suffix + "'; " + light.Id + " cannot be configured.");
                    }
                }
            }
        }

        // ------------------------------------------------------------------
        // One-time migration from the old mod-folder config.json.
        // ------------------------------------------------------------------

        /// <summary>The old file's shape. Every field optional; null meant "default".</summary>
        // Properties rather than fields only so the compiler does not flag them as
        // never assigned -- Newtonsoft fills them by reflection.
        internal sealed class LegacyFile
        {
            public Dictionary<string, LegacyLight> Lights { get; set; }
        }

        internal sealed class LegacyLight
        {
            public bool? Enabled { get; set; }
            public float? Watts { get; set; }
            public float? ExtraSensorRadius { get; set; }
            public float? LingerSeconds { get; set; }
        }

        /// <summary>
        /// Copies a pre-options-screen config.json out of the mod folder onto the new
        /// options object, so a player who had customised it does not silently get the
        /// defaults back.
        ///
        /// Runs only when no options file exists yet. An old file's SensorRadius key
        /// (pre lit-cell sensor) has no field here and is dropped, which is what the
        /// rename to ExtraSensorRadius was for. The legacy file is left in place: on
        /// a Workshop install it lives in the mod folder, which is not ours to tidy.
        /// </summary>
        private static void MigrateLegacyConfig(string modPath, Options options)
        {
            if (string.IsNullOrEmpty(modPath))
            {
                return;
            }

            string file = Path.Combine(modPath, LegacyFileName);
            if (!File.Exists(file))
            {
                return;
            }

            try
            {
                LegacyFile legacy = JsonConvert.DeserializeObject<LegacyFile>(File.ReadAllText(file));
                if (legacy?.Lights == null)
                {
                    return;
                }

                int migrated = 0;
                foreach (KeyValuePair<string, LegacyLight> pair in legacy.Lights)
                {
                    LumenLight light = Array.Find(LumenLights.All, l => l.Id == pair.Key);
                    if (light == null || pair.Value == null)
                    {
                        Log("ignoring unknown light '" + pair.Key + "' while migrating " + LegacyFileName);
                        continue;
                    }

                    LightSettings current = Read(options, light);
                    Write(options, light, new LightSettings
                    {
                        Enabled = pair.Value.Enabled ?? current.Enabled,
                        Watts = pair.Value.Watts ?? current.Watts,
                        ExtraSensorRadius = pair.Value.ExtraSensorRadius ?? current.ExtraSensorRadius,
                        LingerSeconds = pair.Value.LingerSeconds ?? current.LingerSeconds,
                    });
                    migrated++;
                }

                // Round-trip through Read so out-of-range legacy values are clamped
                // before they are written to the new file.
                foreach (LumenLight light in LumenLights.All)
                {
                    Write(options, light, Read(options, light));
                }

                Log("migrated settings for " + migrated + " light(s) from the old " + LegacyFileName
                    + ". That file is no longer read; use the mod's options screen from now on.");
            }
            catch (Exception e)
            {
                // A malformed legacy file must never stop the mod from loading.
                Log("could not migrate the old " + LegacyFileName + "; using defaults. " + e.Message);
            }
        }

        // Fully qualified: ONI declares its own global `Debug` class alongside
        // UnityEngine.Debug, so an unqualified call is ambiguous.
        internal static void Log(string message)
        {
            UnityEngine.Debug.Log("[Lumen] " + message);
        }

        internal static void LogWarning(string message)
        {
            UnityEngine.Debug.LogWarning("[Lumen] " + message);
        }
    }
}
