using System;
using HarmonyLib;

namespace FastInsulatedSelfSealingAirLock;

// Door.RefreshControlState() ends with SetWorldState(updateSim: false), which deliberately skips
// SetSimState. Every control-state change routes through there -- the UI toggle, the logic wire,
// ApplyRequestedControlState, OnSpawn, and both Sealed enter/exit -- so hooking SetSimState alone
// misses all of them.
//
// Cell properties are sticky in the sim, so the door keeps whatever the *last* SetSimState left:
//
//   Auto/Locked -> Opened : stays sealed until the state machine reaches `open`  (harmless)
//   Opened -> Auto/Locked : stays UNSEALED until it reaches `closed`/`locked`    (gas leak)
//
// That second window is normally sub-second (closeblocked -> closedelay 0.5s -> closing -> closed),
// but `closeblocked` has no SetWorldState entry at all, so a dupe or critter standing in the doorway
// parks the door there and holds it open to gas indefinitely.
//
// It leaks freely rather than seeping: Door.OnSpawn unconditionally Bypasses structure temperature
// for any non-Internal door, which lets vanilla SetSimState fall through to SimMessages.Dig() and
// physically remove the door from its own cells. Our impermeability flags are the only thing
// holding gas back while it is open, so any gap in re-applying them is a wide-open hole.
//
// Patching SetWorldState closes this: it runs on both the updateSim:true and updateSim:false paths,
// so the seal is re-asserted the instant the control state changes.
[HarmonyPatch(typeof(Door), "SetWorldState")]
public static class DoorSetWorldStatePatch
{
	public static void Postfix(Door __instance, bool updateSim)
	{
		try
		{
			// updateSim:true already ran SetSimState, where DoorSetSimStatePatch.Postfix sealed.
			// Re-applying here as well would just double the sim messages per transition.
			if (updateSim || !DoorPatchHelpers.IsFastAirlockDoor(__instance))
			{
				return;
			}
			Building building = __instance.building;
			DoorPatchHelpers.ApplySelfSealingState(__instance, (building != null) ? building.PlacementCells : null, "SetWorldState.Postfix");
		}
		catch (Exception ex)
		{
			DoorDiagnostics.LogPatchException("DoorSetWorldStatePatch.Postfix", __instance, ex);
		}
	}
}
