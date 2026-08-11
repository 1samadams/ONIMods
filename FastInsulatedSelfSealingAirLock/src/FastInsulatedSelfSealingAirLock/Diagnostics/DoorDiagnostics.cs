using System;
using System.Collections.Generic;
using UnityEngine;

namespace FastInsulatedSelfSealingAirLock;

internal static class DoorDiagnostics
{
	private struct CellSnapshot
	{
		private readonly bool valid;

		private readonly int cell;

		private readonly string element;

		private readonly string state;

		private readonly float mass;

		private readonly float temperature;

		private readonly byte insulation;

		private readonly bool isGas;

		private readonly bool isLiquid;

		private readonly bool isSolidElement;

		private readonly bool liquidImpermeable;

		private readonly bool foundation;

		private readonly bool hasDoor;

		private readonly bool fakeFloor;

		private readonly bool renderedByWorld;

		private CellSnapshot(bool valid, int cell, string element, string state, float mass, float temperature, byte insulation, bool isGas, bool isLiquid, bool isSolidElement, bool liquidImpermeable, bool foundation, bool hasDoor, bool fakeFloor, bool renderedByWorld)
		{
			this.valid = valid;
			this.cell = cell;
			this.element = element;
			this.state = state;
			this.mass = mass;
			this.temperature = temperature;
			this.insulation = insulation;
			this.isGas = isGas;
			this.isLiquid = isLiquid;
			this.isSolidElement = isSolidElement;
			this.liquidImpermeable = liquidImpermeable;
			this.foundation = foundation;
			this.hasDoor = hasDoor;
			this.fakeFloor = fakeFloor;
			this.renderedByWorld = renderedByWorld;
		}

		public static CellSnapshot Capture(int cell)
		{
			if (!Grid.IsValidCell(cell))
			{
				return new CellSnapshot(valid: false, cell, "<invalid>", "<invalid>", 0f, 0f, 0, isGas: false, isLiquid: false, isSolidElement: false, liquidImpermeable: false, foundation: false, hasDoor: false, fakeFloor: false, renderedByWorld: false);
			}
			Element element = Grid.Element[cell];
			string text = ((element == null) ? "<null>" : element.id.ToString());
			string text2 = ((element == null) ? "<null>" : element.state.ToString());
			bool flag = element?.IsSolid ?? false;
			bool flag2 = Grid.FakeFloor[cell];
			return new CellSnapshot(valid: true, cell, text, text2, Grid.Mass[cell], Grid.Temperature[cell], Grid.Insulation[cell], Grid.IsGas(cell), Grid.IsLiquid(cell), flag, Grid.LiquidImpermeable[cell], Grid.Foundation[cell], Grid.HasDoor[cell], flag2, Grid.RenderedByWorld[cell]);
		}

		public string Format()
		{
			int num;
			if (!valid)
			{
				num = cell;
				return "cell=" + num + " valid=false";
			}
			string[] array = new string[28];
			array[0] = "cell=";
			num = cell;
			array[1] = num.ToString();
			array[2] = " element=";
			array[3] = element;
			array[4] = " state=";
			array[5] = state;
			array[6] = " mass=";
			float num2 = mass;
			array[7] = num2.ToString("0.###");
			array[8] = " tempK=";
			num2 = temperature;
			array[9] = num2.ToString("0.###");
			array[10] = " insulation=";
			byte b = insulation;
			array[11] = b.ToString();
			array[12] = " gas=";
			bool flag = isGas;
			array[13] = flag.ToString();
			array[14] = " liquid=";
			flag = isLiquid;
			array[15] = flag.ToString();
			array[16] = " solidElement=";
			flag = isSolidElement;
			array[17] = flag.ToString();
			array[18] = " liquidImpermeable=";
			flag = liquidImpermeable;
			array[19] = flag.ToString();
			array[20] = " foundation=";
			flag = foundation;
			array[21] = flag.ToString();
			array[22] = " hasDoor=";
			flag = hasDoor;
			array[23] = flag.ToString();
			array[24] = " fakeFloor=";
			flag = fakeFloor;
			array[25] = flag.ToString();
			array[26] = " renderedByWorld=";
			flag = renderedByWorld;
			array[27] = flag.ToString();
			return string.Concat(array);
		}

		public string DeltaFrom(CellSnapshot previous)
		{
			bool flag;
			if (!valid || !previous.valid)
			{
				flag = previous.valid;
				string text = flag.ToString();
				flag = valid;
				return "valid " + text + "->" + flag;
			}
			string text2 = ((previous.element == element) ? "same" : (previous.element + "->" + element));
			string[] obj = new string[14]
			{
				"element=",
				text2,
				" mass=",
				(mass - previous.mass).ToString("+0.###;-0.###;0"),
				" tempK=",
				(temperature - previous.temperature).ToString("+0.###;-0.###;0"),
				" insulation=",
				null,
				null,
				null,
				null,
				null,
				null,
				null
			};
			byte b = previous.insulation;
			obj[7] = b.ToString();
			obj[8] = "->";
			b = insulation;
			obj[9] = b.ToString();
			obj[10] = " liquidImpermeable=";
			flag = previous.liquidImpermeable;
			obj[11] = flag.ToString();
			obj[12] = "->";
			flag = liquidImpermeable;
			obj[13] = flag.ToString();
			return string.Concat(obj);
		}
	}

	private const float LeakageSampleIntervalSeconds = 5f;

	private static readonly Dictionary<int, Door> RegisteredDoors = new Dictionary<int, Door>();

	private static readonly Dictionary<int, float> NextLeakageSampleAt = new Dictionary<int, float>();

	private static readonly Dictionary<string, CellSnapshot> LastSnapshots = new Dictionary<string, CellSnapshot>();

	private static readonly HashSet<int> LoggedTemperatureReadRepairs = new HashSet<int>();

	private static float nextRegisteredSampleAt;

	public static void LogPrefabInit(Door door)
	{
		if (FISSACLog.Active)
		{
			FISSACLog.Log("Door", "OnPrefabInit " + DescribeDoor(door, includeIsOpen: false) + " overrideAnims=" + DescribeOverrideAnims(door));
		}
	}

	public static void RegisterDoor(Door door)
	{
		if (DoorPatchHelpers.IsFastAirlockDoor(door))
		{
			RegisteredDoors[GetDoorId(door)] = door;
			if (FISSACLog.Active)
			{
				FISSACLog.Log("Door", "Registered " + DescribeDoor(door));
			}
		}
	}

	public static void UnregisterDoor(Door door)
	{
		int doorId = GetDoorId(door);
		RegisteredDoors.Remove(doorId);
		RemoveSampleState(door);
		if (FISSACLog.Active)
		{
			FISSACLog.Log("Door", "Unregistered " + DescribeDoor(door));
		}
	}

	public static void LogSelfSealingApplication(string phase, Door door, IList<int> cells, bool allowTransmission)
	{
		if (FISSACLog.Active)
		{
			FISSACLog.Log("DoorState", phase + " " + DescribeDoor(door) + " allowTransmission=" + allowTransmission);
			LogCells(phase, door, cells, includeNeighbors: true, forceDelta: true);
		}
	}

	public static void LogTemperatureRepair(string phase, Door door, float previousTemperature, float repairedTemperature)
	{
		if (FISSACLog.Active)
		{
			FISSACLog.Log("DoorState", phase + " repaired PrimaryElement temperature " + previousTemperature.ToString("0.###") + "->" + repairedTemperature.ToString("0.###") + " " + DescribeDoor(door));
		}
	}

	public static void LogTemperatureReadRepair(string phase, PrimaryElement primaryElement, float previousTemperature, float repairedTemperature, bool updatedInternalTemperature)
	{
		if (FISSACLog.Active)
		{
			int objectId = GetObjectId(primaryElement);
			if (LoggedTemperatureReadRepairs.Add(objectId))
			{
				FISSACLog.Log("DoorState", phase + " repaired delegated temperature read " + previousTemperature.ToString("0.###") + "->" + repairedTemperature.ToString("0.###") + " updatedInternalTemperature=" + updatedInternalTemperature + " " + DescribePrimaryElement(primaryElement));
			}
		}
	}

	public static void LogCleanUp(Door door, IList<int> cells)
	{
		if (!FISSACLog.Active)
		{
			RemoveSampleState(door);
			return;
		}
		FISSACLog.Log("Door", "OnCleanUp " + DescribeDoor(door));
		LogCells("cleanup", door, cells, includeNeighbors: false, forceDelta: true);
		RemoveSampleState(door);
	}

	public static void SampleRegisteredDoors()
	{
		if (!FISSACLog.Active || RegisteredDoors.Count == 0)
		{
			return;
		}
		float unscaledTime = Time.unscaledTime;
		if (unscaledTime < nextRegisteredSampleAt)
		{
			return;
		}
		nextRegisteredSampleAt = unscaledTime + 5f;
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, Door> registeredDoor in RegisteredDoors)
		{
			Door value = registeredDoor.Value;
			if (value == null || !DoorPatchHelpers.IsFastAirlockDoor(value) || value.building?.PlacementCells == null)
			{
				list.Add(registeredDoor.Key);
			}
			else
			{
				SampleLeakage(value);
			}
		}
		foreach (int item in list)
		{
			RegisteredDoors.Remove(item);
			RemoveSampleState(item);
		}
	}

	public static void SampleLeakage(Door door)
	{
		if (FISSACLog.Active && DoorPatchHelpers.IsFastAirlockDoor(door))
		{
			int doorId = GetDoorId(door);
			float unscaledTime = Time.unscaledTime;
			if (!NextLeakageSampleAt.TryGetValue(doorId, out var value) || !(unscaledTime < value))
			{
				NextLeakageSampleAt[doorId] = unscaledTime + 5f;
				IList<int> cells = door?.building?.PlacementCells;
				LeakSummary.Log(door, cells, doorId, DescribeDoor(door));
				if (FISSACLog.VerboseCells)
				{
					LogCells("leakage-sample", door, cells, includeNeighbors: true, forceDelta: false);
				}
			}
		}
	}

	public static void LogPatchException(string patchName, Door door, Exception ex)
	{
		FISSACLog.Error(patchName + " " + DescribeDoor(door), ex);
	}

	public static void LogOverrideAnimRepair(string phase, Door door, KAnimFile[] before, KAnimFile[] after)
	{
		if (FISSACLog.Active)
		{
			string text = DescribeOverrideAnims(before);
			string text2 = DescribeOverrideAnims(after);
			if (!(text == text2))
			{
				FISSACLog.Log("DoorAnim", phase + " " + DescribeDoor(door, includeIsOpen: false) + " overrideAnims=" + text + "->" + text2);
			}
		}
	}

	public static void LogWorkableAnimInfo(string phase, Door door, KAnimFile[] overrideAnims)
	{
		if (FISSACLog.Active)
		{
			FISSACLog.Log("DoorAnim", phase + " " + DescribeDoor(door, includeIsOpen: false) + " resultOverrideAnims=" + DescribeOverrideAnims(overrideAnims));
		}
	}

	private static void LogCells(string phase, Door door, IList<int> cells, bool includeNeighbors, bool forceDelta)
	{
		if (cells == null)
		{
			FISSACLog.Log("Cells", phase + " " + DescribeDoor(door) + " cells=<null>");
			return;
		}
		for (int i = 0; i < cells.Count; i++)
		{
			int cell = cells[i];
			LogCell(phase, door, "door[" + i + "]", cell, forceDelta);
			if (includeNeighbors)
			{
				LogCell(phase, door, "left[" + i + "]", Grid.CellLeft(cell), forceDelta);
				LogCell(phase, door, "right[" + i + "]", Grid.CellRight(cell), forceDelta);
				LogCell(phase, door, "above[" + i + "]", Grid.CellAbove(cell), forceDelta);
				LogCell(phase, door, "below[" + i + "]", Grid.CellBelow(cell), forceDelta);
			}
		}
	}

	private static void LogCell(string phase, Door door, string role, int cell, bool forceDelta)
	{
		CellSnapshot value = CellSnapshot.Capture(cell);
		string key = GetDoorId(door) + ":" + role + ":" + cell;
		string text = string.Empty;
		if (LastSnapshots.TryGetValue(key, out var value2))
		{
			text = " delta=" + value.DeltaFrom(value2);
		}
		else if (forceDelta)
		{
			text = " delta=<first>";
		}
		LastSnapshots[key] = value;
		FISSACLog.Log("Cell", phase + " " + role + " " + value.Format() + text);
	}

	private static string DescribeDoor(Door door, bool includeIsOpen = true)
	{
		if (door == null)
		{
			return "door=<null>";
		}
		try
		{
			KPrefabID component = door.GetComponent<KPrefabID>();
			string text = ((component == null) ? "<no KPrefabID>" : component.PrefabTag.ToString());
			string text2 = SafeDoorState(door);
			string text3 = (includeIsOpen ? (" isOpen=" + SafeIsOpen(door)) : string.Empty);
			string text4 = ((door.transform == null) ? "<no transform>" : door.transform.position.ToString());
			return "id=" + GetDoorId(door) + " prefab=" + text + " state=" + text2 + text3 + " pos=" + text4;
		}
		catch (Exception ex)
		{
			return "door=<describe failed: " + ex.Message + ">";
		}
	}

	private static string DescribePrimaryElement(PrimaryElement primaryElement)
	{
		if (primaryElement == null)
		{
			return "primaryElement=<null>";
		}
		try
		{
			KPrefabID component = primaryElement.GetComponent<KPrefabID>();
			string text = ((component == null) ? "<no KPrefabID>" : component.PrefabTag.ToString());
			string text2 = ((primaryElement.transform == null) ? "<no transform>" : primaryElement.transform.position.ToString());
			return "id=" + GetObjectId(primaryElement) + " prefab=" + text + " element=" + primaryElement.ElementID.ToString() + " internalTempK=" + primaryElement.InternalTemperature.ToString("0.###") + " pos=" + text2;
		}
		catch (Exception ex)
		{
			return "primaryElement=<describe failed: " + ex.Message + ">";
		}
	}

	private static string DescribeOverrideAnims(Door door)
	{
		return DescribeOverrideAnims(door?.overrideAnims);
	}

	private static string DescribeOverrideAnims(KAnimFile[] overrideAnims)
	{
		if (overrideAnims == null)
		{
			return "<null>";
		}
		int num = 0;
		for (int i = 0; i < overrideAnims.Length; i++)
		{
			if (overrideAnims[i] == null)
			{
				num++;
			}
		}
		return "count=" + overrideAnims.Length + " nulls=" + num;
	}

	private static string SafeDoorState(Door door)
	{
		try
		{
			return door.CurrentState.ToString();
		}
		catch (Exception ex)
		{
			return "<state failed: " + ex.Message + ">";
		}
	}

	private static string SafeIsOpen(Door door)
	{
		try
		{
			return door.IsOpen().ToString();
		}
		catch
		{
			return "<unavailable>";
		}
	}

	private static int GetDoorId(Door door)
	{
		try
		{
			return GetObjectId(door);
		}
		catch
		{
			return 0;
		}
	}

	private static int GetObjectId(UnityEngine.Object value)
	{
		if (!(value == null))
		{
			return value.GetInstanceID();
		}
		return 0;
	}

	private static void RemoveSampleState(Door door)
	{
		RemoveSampleState(GetDoorId(door));
	}

	private static void RemoveSampleState(int doorId)
	{
		NextLeakageSampleAt.Remove(doorId);
		LeakSummary.Forget(doorId);
		string value = doorId + ":";
		List<string> list = new List<string>();
		foreach (string key in LastSnapshots.Keys)
		{
			if (key.StartsWith(value, StringComparison.Ordinal))
			{
				list.Add(key);
			}
		}
		foreach (string item in list)
		{
			LastSnapshots.Remove(item);
		}
	}
}
