using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	public class MentalBreakThresholdsCache
	{
		private Dictionary<Pawn, float> minorBreakThresholds = new Dictionary<Pawn, float>();
		private Dictionary<Pawn, float> majorBreakThresholds = new Dictionary<Pawn, float>();
		private Dictionary<Pawn, float> extremeBreakThresholds = new Dictionary<Pawn, float>();

		private int updateTick;
		private const int updateInterval = 180;

		public float GetMinorBreakThreshold(Pawn pawn)
		{
			if (Find.TickManager.TicksGame > updateTick + updateInterval)
			{
				Clear();
			}
			if (!minorBreakThresholds.ContainsKey(pawn))
			{
				minorBreakThresholds.Add(pawn, pawn.GetStatValue(StatDefOf.MentalBreakThreshold));
			}
			return minorBreakThresholds[pawn];
		}
		public float GetMajorBreakThreshold(Pawn pawn)
		{
			if (Find.TickManager.TicksGame > updateTick + updateInterval)
			{
				Clear();
			}
			if (!majorBreakThresholds.ContainsKey(pawn))
			{
				majorBreakThresholds.Add(pawn, pawn.GetStatValue(StatDefOf.MentalBreakThreshold) * 0.5714286f);
			}
			return majorBreakThresholds[pawn];
		}
		public float GetExtremeBreakThreshold(Pawn pawn)
		{
			if (Find.TickManager.TicksGame > updateTick + updateInterval)
			{
				Clear();
			}
			if (!extremeBreakThresholds.ContainsKey(pawn))
			{
				extremeBreakThresholds.Add(pawn, pawn.GetStatValue(StatDefOf.MentalBreakThreshold) / 7f);
			}
			return extremeBreakThresholds[pawn];
		}
		public void Clear()
		{
			minorBreakThresholds.Clear();
			majorBreakThresholds.Clear();
			extremeBreakThresholds.Clear();
			updateTick = Find.TickManager.TicksGame;
		}
	}
}
