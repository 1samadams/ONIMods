using System;
using System.Collections.Generic;
using System.IO;

namespace FastInsulatedSelfSealingAirLock;

internal static class TranslationFileResolver
{
	internal static string GetBrokenExtractorFileName(string code)
	{
		return "translations\\" + code + ".po";
	}

	internal static string[] GetCandidatePaths(string modDir, string code)
	{
		if (string.IsNullOrEmpty(modDir) || string.IsNullOrEmpty(code))
		{
			return Array.Empty<string>();
		}
		List<string> list = new List<string>();
		foreach (string candidateCode in GetCandidateCodes(code))
		{
			list.Add(Path.Combine(Path.Combine(modDir, "translations"), candidateCode + ".po"));
			list.Add(Path.Combine(modDir, GetBrokenExtractorFileName(candidateCode)));
		}
		return list.ToArray();
	}

	internal static string Resolve(string modDir, string code)
	{
		string[] candidatePaths = GetCandidatePaths(modDir, code);
		foreach (string text in candidatePaths)
		{
			if (File.Exists(text))
			{
				return text;
			}
		}
		return null;
	}

	private static IEnumerable<string> GetCandidateCodes(string code)
	{
		HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
		List<string> list = new List<string>();
		AddCandidateCode(list, seen, code);
		string text = code.Replace('_', '-');
		AddCandidateCode(list, seen, text);
		AddCandidateCode(list, seen, NormalizeCultureCode(text, '-'));
		string candidate = code.Replace('-', '_');
		AddCandidateCode(list, seen, candidate);
		AddCandidateCode(list, seen, NormalizeCultureCode(text, '_'));
		int num = text.IndexOf('-');
		if (text.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
		{
			AddCandidateCode(list, seen, "zh");
		}
		if (text.Equals("pt-BR", StringComparison.OrdinalIgnoreCase))
		{
			AddCandidateCode(list, seen, "pt_BR");
		}
		if (num > 0)
		{
			AddCandidateCode(list, seen, text.Substring(0, num).ToLowerInvariant());
		}
		return list;
	}

	private static void AddCandidateCode(List<string> candidateCodes, HashSet<string> seen, string candidate)
	{
		if (!string.IsNullOrEmpty(candidate) && seen.Add(candidate))
		{
			candidateCodes.Add(candidate);
		}
	}

	private static string NormalizeCultureCode(string dashCode, char separator)
	{
		int num = dashCode.IndexOf('-');
		if (num <= 0 || num >= dashCode.Length - 1)
		{
			return dashCode.Replace('-', separator);
		}
		string text = dashCode.Substring(0, num).ToLowerInvariant();
		string text2 = dashCode.Substring(num + 1);
		if (text2.Length == 2)
		{
			text2 = text2.ToUpperInvariant();
		}
		else if (text2.Equals("hans", StringComparison.OrdinalIgnoreCase))
		{
			text2 = "Hans";
		}
		else if (text2.Equals("hant", StringComparison.OrdinalIgnoreCase))
		{
			text2 = "Hant";
		}
		return text + separator + text2;
	}
}
