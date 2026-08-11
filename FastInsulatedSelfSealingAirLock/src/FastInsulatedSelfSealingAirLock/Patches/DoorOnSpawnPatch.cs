using System;
using HarmonyLib;

namespace FastInsulatedSelfSealingAirLock;

[HarmonyPatch(typeof(Door), "OnSpawn")]
public static class DoorOnSpawnPatch
{
	public static void Postfix(Door __instance)
	{
		try
		{
			DoorDiagnostics.RegisterDoor(__instance);
			DoorPatchHelpers.ApplySelfSealingState(__instance, __instance.building?.PlacementCells, "OnSpawn.Postfix");
		}
		catch (Exception ex)
		{
			DoorDiagnostics.LogPatchException("DoorOnSpawnPatch.Postfix", __instance, ex);
		}
	}
}
