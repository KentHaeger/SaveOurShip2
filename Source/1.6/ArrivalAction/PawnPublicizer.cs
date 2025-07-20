using System;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace SaveOurShip2
{
	public class PawnPublicizer : Pawn
	{
		// Makes tick, which was hidden in 1.6 update available again for mod purposes
		public static void DoTick(Pawn pawn)
		{
			((PawnPublicizer)pawn).Tick();
		}
	}
}

