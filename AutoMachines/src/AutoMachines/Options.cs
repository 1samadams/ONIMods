using System.Collections.Generic;
using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace AutoMachines
{
    /// <summary>
    /// The in-game options screen, via Peter Han's PLib. One checkbox per
    /// building, grouped into categories.
    ///
    /// Every option carries [RestartRequired], and that is structural rather
    /// than a PLib limitation: each patch class gates itself in Prepare(), which
    /// Harmony evaluates once during PatchAll at mod load, and
    /// DoPostConfigureComplete then runs once per process while Assets builds
    /// the prefabs. Nothing re-reads these values afterwards, so a checkbox
    /// toggled mid-session cannot retroactively change a prefab that has already
    /// been built. See Settings.OnOptionsChanged.
    ///
    /// Property names are the game's building IDs, deliberately: they are the
    /// keys in the JSON file, so a hand-edited config still looks like the one
    /// this mod shipped with before the options screen existed, and the legacy
    /// migration in Settings can match them by name.
    ///
    /// SharedConfigLocation: true moves the file out of the mod folder and into
    /// the game's shared mods\config\ directory. This matters for Workshop
    /// subscribers — Steam overwrites the mod folder on every update, which
    /// silently discarded their settings in the config.json era.
    /// </summary>
    [ConfigFile("config.json", true, true)]
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Options : SingletonOptions<Options>, IOptions
    {
        private const string BaseGame = "Base Game";
        private const string Dlc = "DLC Buildings";
        private const string Balance = "Balance Sensitive";
        private const string Experimental = "Experimental";

        // ------------------------------------------------------------------
        // Base game — no DLC requirement declared on the building config.
        // ------------------------------------------------------------------

        [RestartRequired]
        [Option("Rock Crusher", "Run the Rock Crusher without a Duplicant operating it.", BaseGame)]
        [JsonProperty]
        public bool RockCrusher { get; set; }

        [RestartRequired]
        [Option("Metal Refinery", "Run the Metal Refinery without a Duplicant operating it. Coolant handling is untouched.", BaseGame)]
        [JsonProperty]
        public bool MetalRefinery { get; set; }

        [RestartRequired]
        [Option("Glass Forge", "Run the Glass Forge without a Duplicant operating it.", BaseGame)]
        [JsonProperty]
        public bool GlassForge { get; set; }

        [RestartRequired]
        [Option("Supermaterial Refinery", "Run the Supermaterial Refinery without a Duplicant operating it.", BaseGame)]
        [JsonProperty]
        public bool SupermaterialRefinery { get; set; }

        [RestartRequired]
        [Option("Microbe Musher", "Run the Microbe Musher without a Duplicant operating it.", BaseGame)]
        [JsonProperty]
        public bool MicrobeMusher { get; set; }

        [RestartRequired]
        [Option("Electric Grill", "Run the Electric Grill without a Duplicant operating it.", BaseGame)]
        [JsonProperty]
        public bool CookingStation { get; set; }

        [RestartRequired]
        [Option("Gas Range", "Run the Gas Range without a Duplicant operating it.", BaseGame)]
        [JsonProperty]
        public bool GourmetCookingStation { get; set; }

        [RestartRequired]
        [Option("Egg Cracker", "Run the Egg Cracker without a Duplicant operating it.", BaseGame)]
        [JsonProperty]
        public bool EggCracker { get; set; }

        [RestartRequired]
        [Option("Textile Loom", "Run the Textile Loom without a Duplicant operating it.", BaseGame)]
        [JsonProperty]
        public bool ClothingFabricator { get; set; }

        [RestartRequired]
        [Option("Exosuit Forge", "Run the Exosuit Forge without a Duplicant operating it. Atmo and Lead suits are recipes on this one building.", BaseGame)]
        [JsonProperty]
        public bool SuitFabricator { get; set; }

        [RestartRequired]
        [Option("Apothecary", "Run the Apothecary without a Duplicant operating it.", BaseGame)]
        [JsonProperty]
        public bool Apothecary { get; set; }

        [RestartRequired]
        [Option("Emulsifier", "Run the Emulsifier without a Duplicant operating it.", BaseGame)]
        [JsonProperty]
        public bool ChemicalRefinery { get; set; }

        [RestartRequired]
        [Option("Plywood Press", "Run the Plywood Press without a Duplicant operating it.", BaseGame)]
        [JsonProperty]
        public bool FabricatedWoodMaker { get; set; }

        [RestartRequired]
        [Option("Clothing Refashionator", "Run the Clothing Refashionator without a Duplicant operating it.", BaseGame)]
        [JsonProperty]
        public bool ClothingAlterationStation { get; set; }

        // ------------------------------------------------------------------
        // DLC buildings. Safe to leave enabled without the DLC — the building
        // simply never spawns.
        // ------------------------------------------------------------------

        [RestartRequired]
        [Option("Sludge Press", "Run the Sludge Press without a Duplicant operating it.", Dlc)]
        [JsonProperty]
        public bool SludgePress { get; set; }

        [RestartRequired]
        [Option("Diamond Press", "Run the Diamond Press without a Duplicant operating it.", Dlc)]
        [JsonProperty]
        public bool DiamondPress { get; set; }

        [RestartRequired]
        [Option("Plant Pulverizer", "Run the Plant Pulverizer without a Duplicant operating it.", Dlc)]
        [JsonProperty]
        public bool MilkPress { get; set; }

        [RestartRequired]
        [Option("Blastshot Maker", "Run the Blastshot Maker without a Duplicant operating it.", Dlc)]
        [JsonProperty]
        public bool MissileFabricator { get; set; }

        [RestartRequired]
        [Option("Deep Fryer", "Run the Deep Fryer without a Duplicant operating it. It still has to be in a Kitchen.", Dlc)]
        [JsonProperty]
        public bool Deepfryer { get; set; }

        [RestartRequired]
        [Option("Sushi Bar", "Run the Sushi Bar without a Duplicant operating it.", Dlc)]
        [JsonProperty]
        public bool SushiBar { get; set; }

        [RestartRequired]
        [Option("Crafting Station", "Run the Crafting Station without a Duplicant operating it.", Dlc)]
        [JsonProperty]
        public bool CraftingTable { get; set; }

        [RestartRequired]
        [Option("Soldering Station", "Run the Soldering Station without a Duplicant operating it.", Dlc)]
        [JsonProperty]
        public bool AdvancedCraftingTable { get; set; }

        [RestartRequired]
        [Option("Nuclear Apothecary", "Run the Nuclear Apothecary without a Duplicant operating it. It will draw Radbolts on its own.", Dlc)]
        [JsonProperty]
        public bool AdvancedApothecary { get; set; }

        // ------------------------------------------------------------------
        // Balance sensitive. These two convert Duplicant labour into a resource
        // the game otherwise gates behind that labour, rather than converting
        // material into material like everything above. Enabled by default,
        // separated so they are easy to find and switch off.
        // ------------------------------------------------------------------

        [RestartRequired]
        [Option("Orbital Data Collection Lab",
            "Run it without a Duplicant operating it. This means an orbital module produces research databanks with no Duplicant aboard the rocket.",
            Balance)]
        [JsonProperty]
        public bool OrbitalResearchCenter { get; set; }

        [RestartRequired]
        [Option("Manual Radbolt Generator",
            "Run it without a Duplicant operating it. It still consumes its uranium, but Duplicant labour is this building's whole cost over the powered Radbolt Generator.",
            Balance)]
        [JsonProperty]
        public bool ManualHighEnergyParticleSpawner { get; set; }

        // ------------------------------------------------------------------
        // Experimental.
        // ------------------------------------------------------------------

        [RestartRequired]
        [Option("Oil Refinery",
            "EXPERIMENTAL, off by default. The Oil Refinery is not a fabricator, so it is driven by a completely different patch that has not been verified in game. A Duplicant may still walk over, and a Duplicant finishing work can switch the machine off mid-conversion.",
            Experimental)]
        [JsonProperty]
        public bool OilRefinery { get; set; }

        /// <summary>
        /// Defaults. Every fabricator on; OilRefinery off because it is a
        /// different mechanism and is not yet game-verified. This constructor is
        /// the single source of truth for defaults — Settings reflects over a
        /// fresh instance rather than keeping a second copy of the list.
        /// </summary>
        public Options()
        {
            RockCrusher = true;
            MetalRefinery = true;
            GlassForge = true;
            SupermaterialRefinery = true;
            MicrobeMusher = true;
            CookingStation = true;
            GourmetCookingStation = true;
            EggCracker = true;
            ClothingFabricator = true;
            SuitFabricator = true;
            Apothecary = true;
            ChemicalRefinery = true;
            FabricatedWoodMaker = true;
            ClothingAlterationStation = true;

            SludgePress = true;
            DiamondPress = true;
            MilkPress = true;
            MissileFabricator = true;
            Deepfryer = true;
            SushiBar = true;
            CraftingTable = true;
            AdvancedCraftingTable = true;
            AdvancedApothecary = true;

            OrbitalResearchCenter = true;
            ManualHighEnergyParticleSpawner = true;

            OilRefinery = false;
        }

        /// <summary>
        /// Settings is the only thing that assigns the singleton, and the setter
        /// on SingletonOptions is protected — so it has to be reached from in
        /// here.
        /// </summary>
        internal static void SetInstance(Options options) => Instance = options;

        public void OnOptionsChanged() => Settings.OnOptionsChanged();

        // No dynamically generated entries; every option is a declared property.
        public IEnumerable<IOptionsEntry> CreateOptions() => null;
    }
}
