using System;
using HarmonyLib;

namespace FastInsulatedSelfSealingAirLock;

[HarmonyPatch(typeof(StructureTemperatureComponents), "OnGetTemperature")]
public static class StructureTemperatureGetPatch
{
	public static void Postfix(PrimaryElement primary_element, ref float __result)
	{
		try
		{
			DoorPatchHelpers.RepairInvalidTemperatureRead(primary_element, ref __result, "StructureTemperature.OnGetTemperature");
		}
		catch (Exception ex)
		{
			FISSACLog.Error("StructureTemperatureGetPatch.Postfix", ex);
		}
	}
}
