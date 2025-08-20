using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;

namespace SaveOurShip2
{
	public static class JobUtility
	{
		// Hacking jobs considered finished beforehead with small threshold which is generally fine for game mechanic.
		public static readonly float WorkFinishThreshold = 10f;
		public static float GetHackSpeed(Pawn pawn)
		{
			float hackingSpeed = pawn.GetStatValue(StatDefOf.HackingSpeed);
			// TODO: may need clamping?
			return hackingSpeed;
		}
		public static float GetHackSpeedClamped(Pawn pawn)
		{
			return Mathf.Clamp(GetHackSpeed(pawn), 0.1f, 10f);
		}
	}
}
