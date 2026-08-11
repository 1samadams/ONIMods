using HarmonyLib;

namespace FastInsulatedSelfSealingAirLock;

[HarmonyPatch(typeof(Db), "Initialize")]
public static class DatabasePatches
{
	public static void Prefix()
	{
		string text = "STRINGS.BUILDINGS.PREFABS." + "FastInsulatedSelfSealingAirLock".ToUpperInvariant();
		Strings.Add(text + ".NAME", ModStrings.Building.NAME);
		Strings.Add(text + ".EFFECT", ModStrings.Building.EFFECT);
		Strings.Add(text + ".DESC", ModStrings.Building.DESC);
	}

	public static void Postfix()
	{
		Db.Get().Techs.TryGet("TemperatureModulation")?.unlockedItemIDs.Add("FastInsulatedSelfSealingAirLock");
		ModUtil.AddBuildingToPlanScreen("Base", "FastInsulatedSelfSealingAirLock", "doors", "PressureDoor");
	}
}
