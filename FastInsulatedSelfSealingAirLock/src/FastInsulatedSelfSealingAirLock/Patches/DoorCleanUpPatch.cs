using System;
using HarmonyLib;

namespace FastInsulatedSelfSealingAirLock;

[HarmonyPatch(typeof(Door), "OnCleanUp")]
public static class DoorCleanUpPatch
{
	public static void Prefix(Door __instance)
	{
		try
		{
			if (DoorPatchHelpers.IsFastAirlockDoor(__instance) && __instance.building?.PlacementCells != null)
			{
				DoorDiagnostics.LogCleanUp(__instance, __instance.building.PlacementCells);
				DoorDiagnostics.UnregisterDoor(__instance);
				int[] placementCells = __instance.building.PlacementCells;
				for (int i = 0; i < placementCells.Length; i++)
				{
					SimMessages.ClearCellProperties(placementCells[i], 7);
				}
			}
		}
		catch (Exception ex)
		{
			DoorDiagnostics.LogPatchException("DoorCleanUpPatch.Prefix", __instance, ex);
		}
	}
}
