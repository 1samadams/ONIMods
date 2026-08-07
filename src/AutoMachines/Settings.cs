using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AutoMachines
{
    /// <summary>
    /// Building keys used in config.json and by the patch classes. These are the
    /// game's building IDs, matching the `ID` const on each config class.
    /// </summary>
    internal static class BuildingIds
    {
        public const string RockCrusher = "RockCrusher";
        public const string MetalRefinery = "MetalRefinery";
        public const string GlassForge = "GlassForge";
        public const string SupermaterialRefinery = "SupermaterialRefinery";
        public const string OilRefinery = "OilRefinery";
        public const string MicrobeMusher = "MicrobeMusher";
        public const string CookingStation = "CookingStation";
        public const string GourmetCookingStation = "GourmetCookingStation";
        public const string EggCracker = "EggCracker";
        public const string ClothingFabricator = "ClothingFabricator";
        public const string SuitFabricator = "SuitFabricator";
        public const string Apothecary = "Apothecary";
    }

    /// <summary>
    /// Reads config.json from the mod folder. Loaded once, before Harmony's
    /// PatchAll runs, because each patch class gates itself on this via Prepare().
    /// </summary>
    internal static class Settings
    {
        private const string FileName = "config.json";

        /// <summary>
        /// Defaults. Every ComplexFabricator-based building is on; OilRefinery is
        /// off because it is a different mechanism and is not yet game-verified.
        /// See CLAUDE.md "Oil Refinery is a special case".
        /// </summary>
        private static readonly Dictionary<string, bool> Defaults = new Dictionary<string, bool>
        {
            { BuildingIds.RockCrusher, true },
            { BuildingIds.MetalRefinery, true },
            { BuildingIds.GlassForge, true },
            { BuildingIds.SupermaterialRefinery, true },
            { BuildingIds.MicrobeMusher, true },
            { BuildingIds.CookingStation, true },
            { BuildingIds.GourmetCookingStation, true },
            { BuildingIds.EggCracker, true },
            { BuildingIds.ClothingFabricator, true },
            { BuildingIds.SuitFabricator, true },
            { BuildingIds.Apothecary, true },
            { BuildingIds.OilRefinery, false },
        };

        private static Dictionary<string, bool> enabled;

        public static void Load(string modPath)
        {
            enabled = new Dictionary<string, bool>(Defaults);

            if (string.IsNullOrEmpty(modPath))
            {
                Log("mod path was empty; using defaults");
                return;
            }

            var file = Path.Combine(modPath, FileName);
            if (!File.Exists(file))
            {
                Log("no " + FileName + " found; using defaults");
                return;
            }

            try
            {
                var parsed = JsonConvert.DeserializeObject<Dictionary<string, bool>>(File.ReadAllText(file));
                if (parsed == null)
                {
                    Log(FileName + " was empty; using defaults");
                    return;
                }

                foreach (var pair in parsed)
                {
                    if (Defaults.ContainsKey(pair.Key))
                    {
                        enabled[pair.Key] = pair.Value;
                    }
                    else
                    {
                        Log("ignoring unknown building key '" + pair.Key + "' in " + FileName);
                    }
                }

                Log("loaded " + FileName);
            }
            catch (Exception e)
            {
                // A malformed config must never stop the mod from loading.
                Log("failed to read " + FileName + "; using defaults. " + e.Message);
                enabled = new Dictionary<string, bool>(Defaults);
            }
        }

        public static bool IsEnabled(string buildingId)
        {
            // Prepare() can in principle run before Load(); fall back to defaults.
            if (enabled == null)
            {
                return Defaults.TryGetValue(buildingId, out var fallback) && fallback;
            }

            return enabled.TryGetValue(buildingId, out var value) && value;
        }

        // Fully qualified: ONI declares its own global `Debug` class alongside
        // UnityEngine.Debug, so an unqualified call is ambiguous.
        public static void Log(string message)
        {
            UnityEngine.Debug.Log("[AutoMachines] " + message);
        }

        public static void LogWarning(string message)
        {
            UnityEngine.Debug.LogWarning("[AutoMachines] " + message);
        }
    }
}
