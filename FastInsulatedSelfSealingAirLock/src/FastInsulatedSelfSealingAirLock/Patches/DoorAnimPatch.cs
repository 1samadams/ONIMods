using System;
using HarmonyLib;

namespace FastInsulatedSelfSealingAirLock;

[HarmonyPatch(typeof(Door), "OnPrefabInit")]
internal static class DoorAnimPatch
{
	private const string DoorRemoteWorkAnim = "anim_use_remote_kanim";

	private static readonly KAnimFile[] NoOverrideAnims = new KAnimFile[0];

	private static KAnimFile[] resolvedDoorOverrideAnims;

	private static void Postfix(Door __instance)
	{
		try
		{
			bool num = DoorPatchHelpers.IsFastAirlockDoor(__instance);
			RepairDoorOverrideAnims(__instance, "OnPrefabInit.Postfix");
			if (num)
			{
				DoorDiagnostics.LogPrefabInit(__instance);
			}
		}
		catch (Exception ex)
		{
			DoorDiagnostics.LogPatchException("DoorAnimPatch.Postfix", __instance, ex);
		}
	}

	private static void RepairDoorOverrideAnims(Door door, string phase)
	{
		if (!(door == null))
		{
			KAnimFile[] overrideAnims = door.overrideAnims;
			KAnimFile[] array = GetSafeDoorOverrideAnims(overrideAnims);
			if ((array == null || array.Length == 0) && DoorPatchHelpers.ShouldUseDoorRemoteOverrideFallback(door) && TryGetDoorOverrideAnims(out var doorOverrideAnims))
			{
				array = doorOverrideAnims;
			}
			door.overrideAnims = array;
			DoorDiagnostics.LogOverrideAnimRepair(phase, door, overrideAnims, array);
		}
	}

	internal static KAnimFile[] GetSafeDoorOverrideAnims(KAnimFile[] overrideAnims)
	{
		if (overrideAnims == null || overrideAnims.Length == 0)
		{
			return overrideAnims;
		}
		int num = 0;
		for (int i = 0; i < overrideAnims.Length; i++)
		{
			if (overrideAnims[i] != null)
			{
				num++;
			}
		}
		if (num == overrideAnims.Length)
		{
			return overrideAnims;
		}
		if (num == 0)
		{
			return NoOverrideAnims;
		}
		KAnimFile[] array = new KAnimFile[num];
		int num2 = 0;
		for (int j = 0; j < overrideAnims.Length; j++)
		{
			if (overrideAnims[j] != null)
			{
				array[num2++] = overrideAnims[j];
			}
		}
		return array;
	}

	internal static bool TryGetDoorOverrideAnims(out KAnimFile[] doorOverrideAnims)
	{
		if (resolvedDoorOverrideAnims != null)
		{
			doorOverrideAnims = resolvedDoorOverrideAnims;
			return true;
		}
		if (Assets.TryGetAnim("anim_use_remote_kanim", out var anim) && anim != null)
		{
			resolvedDoorOverrideAnims = new KAnimFile[1] { anim };
			doorOverrideAnims = resolvedDoorOverrideAnims;
			return true;
		}
		doorOverrideAnims = null;
		return false;
	}
}
