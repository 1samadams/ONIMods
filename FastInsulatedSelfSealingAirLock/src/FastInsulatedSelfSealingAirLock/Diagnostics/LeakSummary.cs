using System;
using System.Collections.Generic;

namespace FastInsulatedSelfSealingAirLock;

// One line per door per sample, designed so a real breach is a single grep rather than an
// archaeology dig through per-cell dumps.
//
// The dispositive number is doorGas. Gas cannot cross the door without occupying the door's own
// cells, so while the door is sealed that figure must stay at exactly 0. A sealed door reading
// doorGas>0 is a genuine breach; everything else is room-level gas movement.
//
// The per-side element labels are deliberately reported alongside a running mass delta, because
// ONI stores one element per cell: a mixed-gas cell flips its label between (say) Oxygen and
// CarbonDioxide as the dominant gas changes, which looks like gas appearing from nowhere. A label
// that oscillates while dA/dB stay flat is that artefact, not a leak.
internal static class LeakSummary
{
	private sealed class Baseline
	{
		public float GasA;

		public float GasB;

		public readonly HashSet<string> SpeciesA = new HashSet<string>(StringComparer.Ordinal);

		public readonly HashSet<string> SpeciesB = new HashSet<string>(StringComparer.Ordinal);
	}

	private struct SideReading
	{
		public float GasMass;

		public float LiquidMass;

		public string Dominant;

		public readonly HashSet<string> Species;

		public SideReading(bool _)
		{
			GasMass = 0f;
			LiquidMass = 0f;
			Dominant = "-";
			Species = new HashSet<string>(StringComparer.Ordinal);
		}
	}

	// Below the sim's own noise floor, but far under anything a real transfer moves.
	private const float BreachThresholdKg = 0.0001f;

	private static readonly Dictionary<int, Baseline> Baselines = new Dictionary<int, Baseline>();

	public static void Log(Door door, IList<int> cells, int doorId, string doorDescription)
	{
		if (!FISSACLog.Active || cells == null || cells.Count == 0)
		{
			return;
		}
		try
		{
			// A 1x2 door placed upright has its second cell above the first, so the open faces are
			// left/right. Rotated flat (PermittedRotations.R90) the cells sit side by side and the
			// open faces are below/above instead.
			bool upright = cells.Count < 2 || cells[1] == Grid.CellAbove(cells[0]);
			SideReading sideA = new SideReading(true);
			SideReading sideB = new SideReading(true);
			float doorGas = 0f;
			float doorLiquid = 0f;
			foreach (int cell in cells)
			{
				AccumulateDoorCell(cell, ref doorGas, ref doorLiquid);
				Accumulate(upright ? Grid.CellLeft(cell) : Grid.CellBelow(cell), ref sideA);
				Accumulate(upright ? Grid.CellRight(cell) : Grid.CellAbove(cell), ref sideB);
			}
			if (!Baselines.TryGetValue(doorId, out var baseline))
			{
				baseline = new Baseline
				{
					GasA = sideA.GasMass,
					GasB = sideB.GasMass
				};
				baseline.SpeciesA.UnionWith(sideA.Species);
				baseline.SpeciesB.UnionWith(sideB.Species);
				Baselines[doorId] = baseline;
			}
			bool isSealed = door.CurrentState != Door.ControlState.Opened;
			string transfer = DescribeTransfer(baseline, sideA, sideB, isSealed);
			string verdict;
			if (!isSealed)
			{
				verdict = "vent(Opened)";
			}
			else if (doorGas > BreachThresholdKg || doorLiquid > BreachThresholdKg)
			{
				verdict = "BREACH";
			}
			else
			{
				verdict = "ok";
			}
			FISSACLog.Log("Leak", string.Concat(
				doorDescription,
				" axis=", upright ? "LR" : "UD",
				" sealed=", isSealed ? "Y" : "N",
				" doorGas=", doorGas.ToString("0.####"),
				" doorLiq=", doorLiquid.ToString("0.####"),
				" A=", sideA.Dominant, ":", sideA.GasMass.ToString("0.###"),
				" B=", sideB.Dominant, ":", sideB.GasMass.ToString("0.###"),
				" dA=", (sideA.GasMass - baseline.GasA).ToString("+0.###;-0.###;0"),
				" dB=", (sideB.GasMass - baseline.GasB).ToString("+0.###;-0.###;0"),
				" xfer=", transfer,
				" verdict=", verdict));
		}
		catch (Exception ex)
		{
			FISSACLog.Error("LeakSummary.Log", ex);
		}
	}

	public static void Forget(int doorId)
	{
		Baselines.Remove(doorId);
	}

	private static void AccumulateDoorCell(int cell, ref float gas, ref float liquid)
	{
		if (Grid.IsValidCell(cell))
		{
			if (Grid.IsGas(cell))
			{
				gas += Grid.Mass[cell];
			}
			else if (Grid.IsLiquid(cell))
			{
				liquid += Grid.Mass[cell];
			}
		}
	}

	private static void Accumulate(int cell, ref SideReading side)
	{
		if (!Grid.IsValidCell(cell))
		{
			return;
		}
		bool gas = Grid.IsGas(cell);
		if (!gas && !Grid.IsLiquid(cell))
		{
			return;
		}
		float mass = Grid.Mass[cell];
		Element element = Grid.Element[cell];
		string name = ((element == null) ? "<null>" : element.id.ToString());
		side.Species.Add(name);
		if (gas)
		{
			// Dominant = whichever neighbour on this face currently holds the most gas, so the
			// label tracks the atmosphere rather than a stray pocket.
			if (mass > side.GasMass || side.Dominant == "-")
			{
				side.Dominant = name;
			}
			side.GasMass += mass;
		}
		else
		{
			side.LiquidMass += mass;
		}
	}

	// Flags a species that started out exclusive to one face and has now shown up on the other.
	// Only meaningful while sealed; an Opened door is supposed to mix.
	private static string DescribeTransfer(Baseline baseline, SideReading sideA, SideReading sideB, bool isSealed)
	{
		if (!isSealed)
		{
			return "n/a";
		}
		List<string> crossed = null;
		foreach (string species in sideB.Species)
		{
			if (baseline.SpeciesA.Contains(species) && !baseline.SpeciesB.Contains(species))
			{
				(crossed ?? (crossed = new List<string>())).Add(species + ">B");
			}
		}
		foreach (string species in sideA.Species)
		{
			if (baseline.SpeciesB.Contains(species) && !baseline.SpeciesA.Contains(species))
			{
				(crossed ?? (crossed = new List<string>())).Add(species + ">A");
			}
		}
		return (crossed == null) ? "none" : string.Join(",", crossed.ToArray());
	}
}
