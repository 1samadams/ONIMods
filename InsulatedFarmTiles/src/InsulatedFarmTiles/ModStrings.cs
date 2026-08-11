namespace InsulatedFarmTiles
{
    /// <summary>
    /// Build-menu name, description and effect for both tiles.
    ///
    /// <c>Strings.Add</c> hashes the whole dotted key as one string, so the flat
    /// <c>STRINGS.BUILDINGS.PREFABS.&lt;ID&gt;.NAME</c> form is what
    /// <c>Strings.Get</c> looks up. Register late and the buildings appear in the
    /// menu as <c>MISSING.STRINGS...</c> instead of their names.
    /// </summary>
    public static class ModStrings
    {
        public static void Register()
        {
            Add(InsulatedFarmTileConfig.Id, InsulatedFarmTileConfig.DisplayName,
                InsulatedFarmTileConfig.Description, InsulatedFarmTileConfig.Effect);

            Add(InsulatedHydroponicFarmConfig.Id, InsulatedHydroponicFarmConfig.DisplayName,
                InsulatedHydroponicFarmConfig.Description, InsulatedHydroponicFarmConfig.Effect);
        }

        private static void Add(string id, string name, string description, string effect)
        {
            string prefix = "STRINGS.BUILDINGS.PREFABS." + id.ToUpperInvariant() + ".";
            Strings.Add(prefix + "NAME", name);
            Strings.Add(prefix + "DESC", description);
            Strings.Add(prefix + "EFFECT", effect);
        }
    }
}
