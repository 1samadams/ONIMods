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
            "Motion activated: lights when a Duplicant enters its beam, and for {0:0} seconds after";

        public const string SensorDescriptorExtended =
            "Motion activated: detects Duplicants up to {0:0} tiles away, and stays lit {1:0} seconds after";

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
