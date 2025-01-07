using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using LudeonTK;

namespace SaveOurShip2
{
	public static class DebugToolsSOS2
	{
		[DebugAction("Map", null, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		// Using mod name to prevent conflict with this command expected to be added to base game 
		private static void KillAllEnemiesOnMapSos2()
		{
			List<Pawn> pawnsToKill = new List<Pawn>();
			foreach (Pawn p in Find.CurrentMap.mapPawns.pawnsSpawned)
			{
				if (p.Faction.HostileTo(Faction.OfPlayer))
				{
					pawnsToKill.Add(p);
				}
			}
			foreach (Pawn p in pawnsToKill)
			{
				p.Kill(null);
			}
		}
	}
}
