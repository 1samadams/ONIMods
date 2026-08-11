using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace AutoMachines
{
    /// <summary>
    /// Building keys used by the patch classes. These are the game's building
    /// IDs, matching the `ID` const on each config class, and they are also the
    /// property names on Options and therefore the keys in the config file.
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

        // Added in the second sweep. Found by enumerating every config in
        // Assembly-CSharp that attaches a ComplexFabricator (or a subclass) and
        // leaves duplicantOperated true — see CLAUDE.md "How the target list was
        // derived". Several are DLC-only; the config classes exist in the base
        // assembly regardless, so patching them is safe without the DLC.
        public const string SludgePress = "SludgePress";
        public const string ChemicalRefinery = "ChemicalRefinery";
        public const string DiamondPress = "DiamondPress";
        public const string MilkPress = "MilkPress";
        public const string FabricatedWoodMaker = "FabricatedWoodMaker";
        public const string MissileFabricator = "MissileFabricator";
        public const string Deepfryer = "Deepfryer";
        public const string SushiBar = "SushiBar";
        public const string CraftingTable = "CraftingTable";
        public const string AdvancedCraftingTable = "AdvancedCraftingTable";
        public const string AdvancedApothecary = "AdvancedApothecary";
        public const string ClothingAlterationStation = "ClothingAlterationStation";
        public const string OrbitalResearchCenter = "OrbitalResearchCenter";
        public const string ManualHighEnergyParticleSpawner = "ManualHighEnergyParticleSpawner";
    }

    /// <summary>
    /// Facade over the PLib options object. The patch classes only ever ask
    /// IsEnabled(buildingId), so the move from a hand-parsed config.json to an
    /// options screen did not touch any of them.
    ///
    /// Load() must run before base.OnLoad(harmony): that call runs Harmony's
    /// PatchAll, which evaluates each patch class's Prepare().
    /// </summary>
    internal static class Settings
    {
        private const string LegacyFileName = "config.json";

        /// <summary>
        /// Snapshot of the options taken at load, keyed by building ID.
        ///
        /// Deliberately NOT refreshed by OnOptionsChanged. Harmony has already
        /// decided which patches exist by the time the player can open the
        /// options screen, so this frozen copy is an honest record of what is
        /// actually patched in this session; a live-updating one would claim
        /// changes that had not happened.
        /// </summary>
        private static Dictionary<string, bool> enabled;

        public static void Load(string modPath)
        {
            var options = POptions.ReadSettings<Options>();

            if (options == null)
            {
                // No config file yet: either a fresh install, or the first run
                // after upgrading from the config.json era. Seed from the old
                // file if one is there, then write it out so this only ever
                // happens once.
                options = new Options();
                MigrateLegacyConfig(modPath, options);

                try
                {
                    POptions.WriteSettings(options);
                }
                catch (Exception e)
                {
                    // Non-fatal: the defaults are already in memory and the mod
                    // works fine. Only persistence is lost.
                    LogWarning("could not write the options file; running with defaults this session. " + e.Message);
                }
            }

            Options.SetInstance(options);
            enabled = Snapshot(options);
            Log("options loaded from " + POptions.GetConfigFilePath(typeof(Options)));
        }

        /// <summary>
        /// Reads the pre-options-screen config.json out of the mod folder and
        /// copies any keys it recognises onto the new options object, so a
        /// player who had customised it does not silently get the defaults back.
        ///
        /// Only ever runs once — after this, a config file exists in the shared
        /// location and ReadSettings stops returning null. The legacy file is
        /// left in place rather than deleted; on a Workshop install it lives in
        /// the mod folder, which is not ours to tidy up.
        /// </summary>
        private static void MigrateLegacyConfig(string modPath, Options options)
        {
            if (string.IsNullOrEmpty(modPath))
            {
                return;
            }

            var file = Path.Combine(modPath, LegacyFileName);
            if (!File.Exists(file))
            {
                return;
            }

            try
            {
                var parsed = JsonConvert.DeserializeObject<Dictionary<string, bool>>(File.ReadAllText(file));
                if (parsed == null)
                {
                    return;
                }

                var migrated = 0;
                foreach (var pair in parsed)
                {
                    var property = typeof(Options).GetProperty(pair.Key,
                        BindingFlags.Public | BindingFlags.Instance);

                    if (property != null && property.PropertyType == typeof(bool) && property.CanWrite)
                    {
                        property.SetValue(options, pair.Value, null);
                        migrated++;
                    }
                    else
                    {
                        Log("ignoring unknown building key '" + pair.Key + "' while migrating " + LegacyFileName);
                    }
                }

                Log("migrated " + migrated + " setting(s) from the old " + LegacyFileName
                    + ". That file is no longer read; use the mod's options screen from now on.");
            }
            catch (Exception e)
            {
                // A malformed legacy file must never stop the mod from loading;
                // the caller's defaults stand.
                Log("could not migrate the old " + LegacyFileName + "; using defaults. " + e.Message);
            }
        }

        /// <summary>
        /// Flattens the options object into an id-keyed map by reflection, so
        /// adding a building means adding one property to Options and one patch
        /// class — nothing here has to change.
        /// </summary>
        private static Dictionary<string, bool> Snapshot(Options options)
        {
            var map = new Dictionary<string, bool>();

            foreach (var property in typeof(Options).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.PropertyType == typeof(bool) && property.CanRead)
                {
                    map[property.Name] = (bool)property.GetValue(options, null);
                }
            }

            return map;
        }

        public static bool IsEnabled(string buildingId)
        {
            // Prepare() can in principle run before Load(); fall back to the
            // constructor defaults rather than disabling everything.
            if (enabled == null)
            {
                return Snapshot(new Options()).TryGetValue(buildingId, out var fallback) && fallback;
            }

            if (!enabled.TryGetValue(buildingId, out var value))
            {
                // A BuildingIds constant with no matching Options property.
                LogWarning("no option found for '" + buildingId + "'; leaving that building vanilla.");
                return false;
            }

            return value;
        }

        /// <summary>
        /// Called by PLib when the player saves the options screen. Every option
        /// is [RestartRequired], so there is nothing to re-apply — the patches
        /// for this session were fixed at load. This only refreshes the
        /// singleton so the screen reflects what was just written.
        /// </summary>
        public static void OnOptionsChanged()
        {
            Options.SetInstance(POptions.ReadSettings<Options>() ?? new Options());
            Log("options changed; restart the game for them to take effect.");
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
