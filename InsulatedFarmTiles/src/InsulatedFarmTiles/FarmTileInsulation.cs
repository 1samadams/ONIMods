using UnityEngine;

namespace InsulatedFarmTiles
{
    /// <summary>
    /// Material-independent insulation, used only when
    /// <see cref="Settings.MaterialIndependentInsulation"/> is on.
    ///
    /// The sim's effective conductivity for a cell is
    /// <c>material.thermalConductivity * insulation</c>, and vanilla
    /// <c>Insulator</c> sets <c>insulation = BuildingDef.ThermalConductivity</c>.
    /// So a fixed def value still scales with what the tile is built from. This
    /// component divides the material back out, pinning the effective
    /// conductivity to <see cref="Settings.TargetConductivity"/> regardless of
    /// build material.
    ///
    /// It is a *replacement* for <c>Insulator</c>, not an addition to it. erotel's
    /// fork added both and relied on this one's <c>OnSpawn</c> running second to
    /// win the <c>SetInsulation</c> call -- but Unity does not define the order of
    /// <c>Start()</c> across components of one GameObject, which is what
    /// <c>KMonoBehaviour.Spawn</c> hangs off. Losing that race would silently
    /// leave the tile on the def's own conductivity with no visible symptom.
    /// Owning both the set and the reset makes the ordering irrelevant.
    /// </summary>
    public sealed class FarmTileInsulation : KMonoBehaviour
    {
        protected override void OnSpawn()
        {
            base.OnSpawn();

            float target = Settings.Instance.ResolvedConductivity;
            PrimaryElement pe = GetComponent<PrimaryElement>();
            float materialConductivity = pe?.Element?.thermalConductivity ?? 0f;

            // effective = materialConductivity * insulation, so insulation =
            // target / materialConductivity. Clamped to 1 so a material already
            // better than the target is left alone rather than made worse.
            float insulation = materialConductivity <= 0f
                ? 1f
                : Mathf.Clamp(target / materialConductivity, 0f, 1f);

            ApplyToPlacementCells(insulation);
        }

        /// <summary>
        /// 1f is "no insulation", which is what vanilla <c>Insulator.OnCleanUp</c>
        /// restores. Without this the cell would keep insulating after the tile
        /// was deconstructed.
        /// </summary>
        protected override void OnCleanUp()
        {
            ApplyToPlacementCells(1f);
            base.OnCleanUp();
        }

        private void ApplyToPlacementCells(float insulation)
        {
            Building building = GetComponent<Building>();
            int[] cells = building?.PlacementCells;
            if (cells == null)
            {
                return;
            }

            for (int i = 0; i < cells.Length; i++)
            {
                SimMessages.SetInsulation(cells[i], insulation);
            }
        }
    }
}
