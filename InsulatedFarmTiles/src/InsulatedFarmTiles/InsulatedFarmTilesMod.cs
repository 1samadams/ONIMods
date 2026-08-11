using System;
using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

namespace InsulatedFarmTiles
{
    /// <summary>
    /// Mod entry point.
    ///
    /// Ordering matters. Both the options and the strings have to be in place
    /// before <c>base.OnLoad</c> runs <c>PatchAll</c> and, later, before the game
    /// sweeps the assembly for <c>IBuildingConfig</c> types:
    ///
    /// <list type="bullet">
    /// <item>the options feed <c>BuildingDef.ThermalConductivity</c> and decide
    /// which insulating component goes on the prefab, both read once when the
    /// definition is built.</item>
    /// <item>the string table feeds the buildings' names and descriptions.
    /// Register late and they show up in the menu as
    /// <c>MISSING.STRINGS...</c>.</item>
    /// </list>
    ///
    /// <c>UserMod2.OnLoad</c> is the only entry point the game calls --
    /// <c>KMod.DLLLoader</c> looks for one <c>UserMod2</c> per assembly and
    /// invokes exactly this method. The mod this descends from also carried a
    /// nested <c>OnModLoad.OnLoad()</c> that nothing has ever called; it was
    /// dropped rather than carried forward as though it worked.
    /// </summary>
    public class InsulatedFarmTilesMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            PUtil.InitLibrary();
            RegisterOptionHandlers();

            new POptions().RegisterOptions(this, typeof(Settings));

            // Force the read now rather than leaving it to SingletonOptions' lazy
            // initialiser, so the settings are in memory before anything can ask
            // for them and the line below reports what will actually be used.
            Settings settings = Settings.Instance;

            ModStrings.Register();

            base.OnLoad(harmony);

            UnityEngine.Debug.Log(
                "[InsulatedFarmTiles] Loaded. Insulation: " + settings.Mode + " @ " + settings.ResolvedConductivity);
        }

        /// <summary>
        /// Maps option property types to the widgets that render them, for THIS
        /// mod only -- PLib is internalised into this assembly, so the handler
        /// table is a private static of our own copy.
        ///
        /// The one change we actually want is <c>float</c>: insulation strength
        /// spans five orders of magnitude, and on the default linear
        /// <c>FloatOptionsEntry</c> every useful value lands in the leftmost few
        /// pixels. <c>LogFloatOptionsEntry</c> gives a log-scale slider.
        ///
        /// The rest of the list is not padding, and must not be trimmed to "just
        /// the types this mod uses". PLib fills the table itself in
        /// <c>OptionsHandlers.InitPredefinedOptions</c>, called from
        /// <c>OptionsEntry.BuildOptions</c> when the dialog is opened -- but it is
        /// guarded on <c>OPTIONS_HANDLERS.Count &lt; 1</c>, and
        /// <c>AddOptionClass</c> is first-wins. So the moment we register
        /// anything here, PLib's own initialisation becomes a no-op and every
        /// type we leave out has no widget at all. A property of that type then
        /// renders as *nothing* -- no row, no warning.
        ///
        /// This mod currently has one enum and one float, and enums never reach
        /// this table (<c>FindOptionClass</c> special-cases <c>IsEnum</c> before
        /// the lookup), so today only the float line matters. The others are here
        /// so that adding a bool or an int later just works.
        ///
        /// <b>The one gap is a <c>Color</c>-typed setting</b>, meaning the
        /// colour-swatch picker widget in the options dialog -- not the tiles'
        /// appearance, which comes from the kanims and was never configurable.
        /// PLib's <c>ColorOptionsEntry</c> and <c>Color32OptionsEntry</c> are
        /// <c>internal</c>, so they cannot be registered from outside PLib and
        /// suppressing <c>InitPredefinedOptions</c> costs us them for good. An
        /// accepted trade in a mod about thermal conductivity.
        ///
        /// Mirrors PLib 4.25.0, which is pinned in the .csproj. Re-check this list
        /// against <c>InitPredefinedOptions</c> when that pin moves.
        /// </summary>
        private static void RegisterOptionHandlers()
        {
            OptionsHandlers.AddOptionClass(typeof(float), typeof(LogFloatOptionsEntry));

            OptionsHandlers.AddOptionClass(typeof(bool), typeof(CheckboxOptionsEntry));
            OptionsHandlers.AddOptionClass(typeof(int), typeof(IntOptionsEntry));
            OptionsHandlers.AddOptionClass(typeof(int?), typeof(NullableIntOptionsEntry));
            OptionsHandlers.AddOptionClass(typeof(float?), typeof(NullableFloatOptionsEntry));
            OptionsHandlers.AddOptionClass(typeof(string), typeof(StringOptionsEntry));
            OptionsHandlers.AddOptionClass(typeof(Action<object>), typeof(ButtonOptionsEntry));
            OptionsHandlers.AddOptionClass(typeof(LocText), typeof(TextBlockOptionsEntry));
        }
    }
}
