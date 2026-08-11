using System.Collections.Generic;
using Newtonsoft.Json;
using PeterHan.PLib.Options;
using UnityEngine;

namespace InsulatedFarmTiles
{
    /// <summary>
    /// How the tiles insulate. An enum rather than a bool so the options dialog
    /// renders a dropdown naming both designs, instead of a checkbox whose
    /// unchecked state has to be explained in a tooltip.
    /// </summary>
    public enum InsulationMode
    {
        [Option("Like a vanilla Insulated Tile",
            "The build material still matters, exactly as it does for a vanilla Insulated Tile: "
            + "Ceramic insulates better than Sandstone, Insulation better still.")]
        VanillaTileParity,

        [Option("Constant, ignores build material",
            "Every tile insulates the same no matter what it is built from. Stronger than a vanilla "
            + "Insulated Tile on every raw mineral - but weaker on Ceramic, which is already better "
            + "than the target and gets pulled back down to it.")]
        MaterialIndependent
    }

    /// <summary>
    /// Mod options, shown in the Mods menu behind the gear icon and persisted by
    /// PLib.
    ///
    /// Both settings are <c>[RestartRequired]</c> and genuinely are:
    /// <see cref="TargetConductivity"/> is baked into
    /// <c>BuildingDef.ThermalConductivity</c> when the definition is created, and
    /// <see cref="Mode"/> decides which insulating component
    /// <c>ConfigureBuildingTemplate</c> puts on the prefab. Neither can be changed
    /// on a definition that already exists, so PLib's restart warning is the
    /// honest answer rather than a cop-out.
    ///
    /// <c>SharedConfigLocation: true</c> puts the file under the game's
    /// <c>mods/config/</c> rather than in the mod folder, so it survives
    /// reinstalling or updating the mod. The trade-off PLib documents is that it
    /// is not necessarily removed on uninstall.
    /// </summary>
    [ConfigFile("config.json", true, true)]
    [JsonObject(MemberSerialization.OptIn)]
    [ModInfo("https://github.com/1samadams/ONIMods")]
    public sealed class Settings : SingletonOptions<Settings>, IOptions
    {
        /// <summary>
        /// Below this the slider is a lie -- Insulation, the best buildable
        /// material in the game, is 0.00001 on its own.
        /// </summary>
        public const float MinConductivity = 0.00001f;

        public const float MaxConductivity = 1f;

        /// <summary>The value vanilla <c>InsulationTileConfig</c> uses.</summary>
        public const float DefaultConductivity = 0.01f;

        [Option("Insulation model",
            "How much the material a tile is built from affects how well it insulates.")]
        [RestartRequired]
        [JsonProperty]
        public InsulationMode Mode { get; set; }

        [Option("Insulation strength",
            "Lower insulates better. 0.01 is what a vanilla Insulated Tile uses, and means the same "
            + "thing here under either model. In the vanilla model this multiplies the build "
            + "material's own conductivity; in the constant model it is the result outright.",
            Format = "0.#####")]
        [Limit(MinConductivity, MaxConductivity)]
        [RestartRequired]
        [JsonProperty]
        public float TargetConductivity { get; set; }

        public Settings()
        {
            Mode = InsulationMode.VanillaTileParity;
            TargetConductivity = DefaultConductivity;
        }

        /// <summary>
        /// No <c>[Option]</c>, so PLib builds no dialog row for it -- entries are
        /// only created from attributes. Reading the mode through one named
        /// predicate keeps the enum comparison out of the building configs.
        /// </summary>
        public bool MaterialIndependentInsulation => Mode == InsulationMode.MaterialIndependent;

        /// <summary>
        /// The dialog clamps to <c>[Limit]</c>, but a hand-edited config file does
        /// not go through the dialog. A zero or negative value would divide by
        /// zero in material-independent mode and mean "perfectly conductive" in
        /// the vanilla model, so it is clamped at the point of use instead of
        /// being trusted.
        /// </summary>
        public float ResolvedConductivity => Mathf.Clamp(TargetConductivity, MinConductivity, MaxConductivity);

        public void OnOptionsChanged()
        {
            Instance = POptions.ReadSettings<Settings>() ?? new Settings();
        }

        /// <summary>Null means "use the attribute-driven entries", which is all this mod needs.</summary>
        public IEnumerable<IOptionsEntry> CreateOptions() => null;
    }
}
