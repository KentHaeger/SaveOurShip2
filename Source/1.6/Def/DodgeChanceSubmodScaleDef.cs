using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
	public class DodgeChanceSubmodScaleDef : Def
	{
		// See DodgeChanceSubmodScaleDef XML comments for details
		public List<float> multiplierList = new List<float>();

		public static float GetEffectiveMultiplier()
		{
			DodgeChanceSubmodScaleDef multipliers = DefDatabase<DodgeChanceSubmodScaleDef>.GetNamed("DodgeChanceSubmodScale");
			if (multipliers == null || multipliers.multiplierList.NullOrEmpty())
			{
				return 1f;
			}
			return multipliers.multiplierList.Min();
		}
	}
}

