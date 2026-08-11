using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace FastInsulatedSelfSealingAirLock;

[HarmonyPatch(typeof(Db), "Initialize")]
[HarmonyPriority(0)]
internal static class TranslationPatch
{
	internal static void Postfix()
	{
		Localization.Locale locale = Localization.GetLocale();
		if (locale == null)
		{
			return;
		}
		string text = locale.Code;
		if (string.IsNullOrEmpty(text))
		{
			text = Localization.GetCurrentLanguageCode();
		}
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		string directoryName = Path.GetDirectoryName(Assembly.GetAssembly(typeof(FastInsulatedSelfSealingAirLockMod)).Location);
		string text2 = TranslationFileResolver.Resolve(directoryName, text);
		if (text2 == null)
		{
			if (!text.StartsWith("en", StringComparison.OrdinalIgnoreCase))
			{
				UnityEngine.Debug.LogWarning("[FISSAC] No translation file found for locale " + text + ". Checked: " + string.Join(", ", TranslationFileResolver.GetCandidatePaths(directoryName, text)));
			}
			return;
		}
		string b = Path.Combine(directoryName, TranslationFileResolver.GetBrokenExtractorFileName(text));
		if (string.Equals(text2, b, StringComparison.Ordinal))
		{
			UnityEngine.Debug.Log("[FISSAC] Loading translation from fallback path: " + text2);
		}
		try
		{
			Dictionary<string, string> dictionary = Localization.LoadStringsFile(text2, isTemplate: false);
			if (dictionary != null && dictionary.Count != 0)
			{
				ApplyToType(typeof(ModStrings), "FastInsulatedSelfSealingAirLock.ModStrings", dictionary);
				RefreshBuildingStrings();
			}
		}
		catch (Exception ex)
		{
			UnityEngine.Debug.LogWarning("[FISSAC] Failed to load translation: " + ex.Message);
		}
	}

	private static void ApplyToType(Type type, string path, Dictionary<string, string> translations)
	{
		FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
		foreach (FieldInfo fieldInfo in fields)
		{
			if (!(fieldInfo.FieldType != typeof(LocString)))
			{
				string text = path + "." + fieldInfo.Name;
				if (translations.TryGetValue(text, out var value))
				{
					fieldInfo.SetValue(null, new LocString(value, text));
					Strings.Add(text, value);
				}
			}
		}
		Type[] nestedTypes = type.GetNestedTypes(BindingFlags.Static | BindingFlags.Public);
		foreach (Type type2 in nestedTypes)
		{
			ApplyToType(type2, path + "." + type2.Name, translations);
		}
	}

	private static void RefreshBuildingStrings()
	{
		string text = "STRINGS.BUILDINGS.PREFABS." + "FastInsulatedSelfSealingAirLock".ToUpperInvariant();
		Strings.Add(text + ".NAME", ModStrings.Building.NAME);
		Strings.Add(text + ".EFFECT", ModStrings.Building.EFFECT);
		Strings.Add(text + ".DESC", ModStrings.Building.DESC);
	}
}
