using RimWorld;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
	public class GlobalUnlockDef : Def
	{
		public bool AllShipsUnlockedFlag = false;

		public static bool AllShipsUnlocked()
		{
			GlobalUnlockDef unlock = DefDatabase<GlobalUnlockDef>.GetNamed("GlobalUnlocks");
			return unlock?.AllShipsUnlockedFlag ?? false;
		}
	}
}

