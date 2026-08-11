using System;
using System.Collections.Generic;
using HarmonyLib;

namespace FastInsulatedSelfSealingAirLock;

[HarmonyPatch(typeof(Door), "SetSimState")]
public static class DoorSetSimStatePatch
{
	public static void Prefix(Door __instance, IList<int> cells)
	{
		try
		{
			DoorPatchHelpers.EnsureValidDoorTemperature(__instance, cells, "SetSimState.Prefix");
		}
		catch (Exception ex)
		{
			DoorDiagnostics.LogPatchException("DoorSetSimStatePatch.Prefix", __instance, ex);
		}
	}

	public static void Postfix(Door __instance, IList<int> cells)
	{
		try
		{
			DoorPatchHelpers.ApplySelfSealingState(__instance, cells, "SetSimState.Postfix");
		}
		catch (Exception ex)
		{
			DoorDiagnostics.LogPatchException("DoorSetSimStatePatch.Postfix", __instance, ex);
		}
	}
}
