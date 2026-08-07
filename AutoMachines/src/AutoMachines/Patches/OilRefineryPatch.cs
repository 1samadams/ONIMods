using HarmonyLib;

namespace AutoMachines.Patches
{
    /// <summary>
    /// Oil Refinery is the one target that is NOT a ComplexFabricator. It is a
    /// StateMachineComponent whose conversion is done by an ElementConverter that
    /// runs while Operational.IsActive. Vanilla sets that flag from a duplicant
    /// working an infinite-duration WorkableTarget:
    ///
    ///   OnStartWork    -> operational.SetActive(true)
    ///   OnStopWork     -> operational.SetActive(false)
    ///   OnCompleteWork -> operational.SetActive(false)
    ///
    /// So "automatic" here means driving Operational directly from the `ready`
    /// state instead of from a worker.
    ///
    /// DISABLED BY DEFAULT (config.json) and not yet verified in-game. Two known
    /// caveats, both needing play-testing before this is turned on by default:
    ///   1. The Fabricate errand created by ready.ToggleChore is still generated, so
    ///      a duplicant may still walk over. Harmless (the machine already runs) but
    ///      it means testing-checklist item 4 will not pass for this building.
    ///   2. A duplicant finishing/aborting work fires OnStopWork, which would switch
    ///      the machine off mid-conversion. The WorkableTarget patch below undoes
    ///      that whenever the refinery is still in `ready`.
    ///
    /// Nothing here writes to save files; Operational.SetActive is runtime state.
    /// </summary>
    internal static class OilRefineryAuto
    {
        public static void SetActive(OilRefinery.StatesInstance smi, bool active)
        {
            if (smi == null || smi.master == null)
            {
                return;
            }

            var operational = smi.master.GetComponent<Operational>();
            if (operational != null)
            {
                operational.SetActive(active);
            }
        }
    }

    [HarmonyPatch(typeof(OilRefinery.States), nameof(OilRefinery.States.InitializeStates))]
    internal static class OilRefineryStatesPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.OilRefinery);

        internal static void Postfix(OilRefinery.States __instance)
        {
            // `ready` means operational + enough crude oil to convert. That is
            // exactly when vanilla would have a duplicant cranking it.
            __instance.ready
                .Enter("AutoMachines.Run", smi => OilRefineryAuto.SetActive(smi, true))
                .Exit("AutoMachines.Stop", smi => OilRefineryAuto.SetActive(smi, false));
        }
    }

    [HarmonyPatch(typeof(OilRefinery.WorkableTarget), "OnStopWork")]
    internal static class OilRefineryStopWorkPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.OilRefinery);

        internal static void Postfix(OilRefinery.WorkableTarget __instance) => KeepRunning(__instance);

        /// <summary>
        /// Vanilla switches the refinery off when a worker stops. If we are still in
        /// `ready`, that is now wrong -- switch it back on.
        /// </summary>
        internal static void KeepRunning(OilRefinery.WorkableTarget workable)
        {
            if (workable == null)
            {
                return;
            }

            var refinery = workable.GetComponent<OilRefinery>();
            if (refinery == null)
            {
                return;
            }

            var smi = refinery.smi;
            if (smi != null && smi.IsInsideState(smi.sm.ready))
            {
                OilRefineryAuto.SetActive(smi, true);
            }
        }
    }

    [HarmonyPatch(typeof(OilRefinery.WorkableTarget), "OnCompleteWork")]
    internal static class OilRefineryCompleteWorkPatch
    {
        internal static bool Prepare() => Settings.IsEnabled(BuildingIds.OilRefinery);

        internal static void Postfix(OilRefinery.WorkableTarget __instance)
            => OilRefineryStopWorkPatch.KeepRunning(__instance);
    }
}
