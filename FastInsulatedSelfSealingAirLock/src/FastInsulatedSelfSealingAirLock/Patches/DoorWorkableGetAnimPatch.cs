using System;
using HarmonyLib;

namespace FastInsulatedSelfSealingAirLock;

[HarmonyPatch(typeof(Workable), "GetAnim")]
internal static class DoorWorkableGetAnimPatch
{
	private static void Postfix(Workable __instance, ref Workable.AnimInfo __result)
	{
		try
		{
			if (!(__instance is Door door))
			{
				return;
			}
			__result.overrideAnims = DoorAnimPatch.GetSafeDoorOverrideAnims(__result.overrideAnims);
			if (!DoorPatchHelpers.ShouldUseDoorRemoteOverrideFallback(door))
			{
				DoorDiagnostics.LogWorkableAnimInfo("Workable.GetAnim.Postfix", door, __result.overrideAnims);
				return;
			}
			if ((__result.overrideAnims == null || __result.overrideAnims.Length == 0) && DoorAnimPatch.TryGetDoorOverrideAnims(out var doorOverrideAnims))
			{
				__result.overrideAnims = doorOverrideAnims;
			}
			DoorDiagnostics.LogWorkableAnimInfo("Workable.GetAnim.Postfix", door, __result.overrideAnims);
		}
		catch (Exception ex)
		{
			DoorDiagnostics.LogPatchException("DoorWorkableGetAnimPatch.Postfix", __instance as Door, ex);
		}
	}
}
