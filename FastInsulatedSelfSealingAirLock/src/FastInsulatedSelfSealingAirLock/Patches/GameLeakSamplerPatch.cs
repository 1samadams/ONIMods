using System;
using HarmonyLib;

namespace FastInsulatedSelfSealingAirLock;

[HarmonyPatch(typeof(Game), "UnsafeSim200ms")]
public static class GameLeakSamplerPatch
{
	public static void Postfix()
	{
		try
		{
			DoorDiagnostics.SampleRegisteredDoors();
		}
		catch (Exception ex)
		{
			FISSACLog.Error("GameLeakSamplerPatch.Postfix", ex);
		}
	}
}
