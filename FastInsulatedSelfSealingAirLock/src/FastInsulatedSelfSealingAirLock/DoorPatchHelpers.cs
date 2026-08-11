using System;
using System.Collections.Generic;

namespace FastInsulatedSelfSealingAirLock;

internal static class DoorPatchHelpers
{
	private static readonly HashSet<string> DoorRemoteOverrideFallbackPrefabIds = new HashSet<string>(StringComparer.Ordinal)
	{
		"FastInsulatedSelfSealingAirLock", "Door", "ManualPressureDoor", "PressureDoor", "BunkerDoor", "WoodenDoor", "InsulatedDoor", "GravitasDoor", "POIFacilityDoor", "POIDlc2ShowroomDoor",
		"POIDoorInternal"
	};

	public static bool IsFastAirlockDoor(Door door)
	{
		KPrefabID kPrefabID = door?.GetComponent<KPrefabID>();
		if (kPrefabID != null)
		{
			return kPrefabID.PrefabTag == "FastInsulatedSelfSealingAirLock";
		}
		return false;
	}

	public static bool ShouldUseDoorRemoteOverrideFallback(Door door)
	{
		KPrefabID kPrefabID = door?.GetComponent<KPrefabID>();
		if (kPrefabID != null)
		{
			return DoorRemoteOverrideFallbackPrefabIds.Contains(kPrefabID.PrefabTag.ToString());
		}
		return false;
	}

	public static bool IsFastAirlockPrimaryElement(PrimaryElement primaryElement)
	{
		KPrefabID kPrefabID = primaryElement?.GetComponent<KPrefabID>();
		if (kPrefabID != null)
		{
			return kPrefabID.PrefabTag == "FastInsulatedSelfSealingAirLock";
		}
		return false;
	}

	public static void EnsureValidDoorTemperature(Door door, IList<int> cells, string phase)
	{
		if (!IsFastAirlockDoor(door))
		{
			return;
		}
		PrimaryElement component = door.GetComponent<PrimaryElement>();
		if (!(component == null) && !(component.Temperature > 0f))
		{
			float bestDoorTemperature = GetBestDoorTemperature(component, cells);
			if (!(bestDoorTemperature <= 0f))
			{
				float temperature = component.Temperature;
				component.Temperature = bestDoorTemperature;
				DoorDiagnostics.LogTemperatureRepair(phase, door, temperature, bestDoorTemperature);
			}
		}
	}

	public static void RepairInvalidTemperatureRead(PrimaryElement primaryElement, ref float __result, string phase)
	{
		if (__result > 0f || !IsFastAirlockPrimaryElement(primaryElement))
		{
			return;
		}
		float bestDoorTemperature = GetBestDoorTemperature(primaryElement, primaryElement.GetComponent<Building>()?.PlacementCells);
		if (!(bestDoorTemperature <= 0f))
		{
			float previousTemperature = __result;
			__result = bestDoorTemperature;
			bool flag = primaryElement.InternalTemperature <= 0f;
			if (flag)
			{
				primaryElement.InternalTemperature = bestDoorTemperature;
			}
			DoorDiagnostics.LogTemperatureReadRepair(phase, primaryElement, previousTemperature, bestDoorTemperature, flag);
		}
	}

	public static void ApplySelfSealingState(Door door, IList<int> cells, string phase)
	{
		if (!IsFastAirlockDoor(door) || cells == null)
		{
			return;
		}
		bool flag = door.CurrentState == Door.ControlState.Opened;
		DoorDiagnostics.LogSelfSealingApplication(phase + ".before", door, cells, flag);
		try
		{
			foreach (int cell in cells)
			{
				if (flag)
				{
					SimMessages.SetInsulation(cell, 1f);
					SimMessages.ClearCellProperties(cell, 7);
				}
				else
				{
					SimMessages.SetInsulation(cell, 0f);
					SimMessages.SetCellProperties(cell, 7);
				}
			}
		}
		catch (Exception ex)
		{
			DoorDiagnostics.LogPatchException("ApplySelfSealingState." + phase, door, ex);
		}
		DoorDiagnostics.LogSelfSealingApplication(phase + ".after", door, cells, flag);
	}

	private static float GetBestDoorTemperature(PrimaryElement primaryElement, IList<int> cells)
	{
		if (primaryElement.InternalTemperature > 0f)
		{
			return primaryElement.InternalTemperature;
		}
		float num = 0f;
		int num2 = 0;
		if (cells != null)
		{
			foreach (int cell in cells)
			{
				if (Grid.IsValidCell(cell) && !(Grid.Temperature[cell] <= 0f))
				{
					num += Grid.Temperature[cell];
					num2++;
				}
			}
		}
		if (num2 > 0)
		{
			return num / (float)num2;
		}
		Element element = ElementLoader.FindElementByHash(primaryElement.ElementID);
		if (element != null && element.defaultValues.temperature > 0f)
		{
			return element.defaultValues.temperature;
		}
		return 300f;
	}
}
