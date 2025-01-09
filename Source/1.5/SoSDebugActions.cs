using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using LudeonTK;

namespace SaveOurShip2
{
	public static class DebugToolsSOS2
	{
		private static void DoEnemyPawnsAction(Action<Pawn> action)
		{
			List<Pawn> pawnsToProcess = new List<Pawn>();
			foreach (Pawn p in Find.CurrentMap.mapPawns.pawnsSpawned)
			{
				if (p.Faction.HostileTo(Faction.OfPlayer))
				{
					pawnsToProcess.Add(p);
				}
			}
			foreach (Pawn p in pawnsToProcess)
			{
				action(p);
			}
		}

		[DebugAction("Map", null, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		// Using mod name to prevent conflict with this command expected to be added to base game
		private static void KillAllEnemiesOnMapSos2()
		{
			DoEnemyPawnsAction((Pawn p) => p.Kill(null));
		}

		[DebugAction("Map", null, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void VanishAllEnemiesOnMapSos2()
		{
			DoEnemyPawnsAction((Pawn p) => p.Destroy(DestroyMode.Vanish));
		}
	}
}
