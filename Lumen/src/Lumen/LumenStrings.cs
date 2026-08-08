namespace Lumen
{
    /// <summary>
    /// Registers the building names, descriptions and effects.
    ///
    /// ONI resolves building UI text from the string table by prefab ID, uppercased:
    /// STRINGS.BUILDINGS.PREFABS.&lt;ID&gt;.NAME / .DESC / .EFFECT. A missing key shows
    /// up in game as the literal text "MISSING.STRINGS.BUILDINGS...", which is the
    /// first thing to look for if a building appears unnamed.
    /// </summary>
    public static class LumenStrings
    {
        public const string SensorDescriptor =
            "Motion activated: {0:0} tile radius";

        public const string SensorDescriptorTooltip =
            "Lights only while a Duplicant is within {0:0} tiles, and for {1:0} seconds after " +
            "the last one leaves. Draws no power at all while dark.";

        public static void Register()
        {
            foreach (LumenLight light in LumenLights.All)
            {
                string root = "STRINGS.BUILDINGS.PREFABS." + light.Id.ToUpperInvariant();
                Strings.Add(root + ".NAME", light.Name);
                Strings.Add(root + ".DESC", light.Description);
                Strings.Add(root + ".EFFECT", light.Effect);
            }
        }
    }
}
