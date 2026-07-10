using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using RimWorld;
using Verse;
using LudeonTK;

namespace SaveOurShip2
{
	public static class DebugToolsSOS2
	{
		private const string sos2Category = "SOS 2";
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

		[DebugAction("Map", null, false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		// Using mod name to prevent conflict with this command expected to be added to base game
		private static void KillAllEnemiesOnMapSos2()
		{
			DoEnemyPawnsAction((Pawn p) => p.Kill(null));
		}

		[DebugAction("Map", null, false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void VanishAllEnemiesOnMapSos2()
		{
			DoEnemyPawnsAction((Pawn p) => p.Destroy(DestroyMode.Vanish));
		}

		// Public because win ship battle command is intended to be used by automated testing, not invoked for real gameplay.
		[DebugAction(sos2Category, null, false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void WinShipBattle()
		{
			Map playerShipMap = ShipInteriorMod2.FindPlayerShipMap();
			if (playerShipMap == null)
			{
				return;
			}
			ShipMapComp playerMapComp = playerShipMap.GetComponent<ShipMapComp>();
			if (playerMapComp.ShipMapState != ShipMapState.inCombat)
			{
				return;
			}
			ShipMapComp enemyMapComp = playerMapComp.TargetMapComp;
			if (enemyMapComp == null)
			{
				return;
			}
			List<SpaceShipCache> ships = enemyMapComp.ShipsOnMap.Values.Where(s => !s.IsWreck).ToList();
			foreach (SpaceShipCache ship in ships)
			{
				List<Building_ShipBridge> bridges = ship.Bridges.ListFullCopy();
				foreach (Building_ShipBridge bridge in bridges)
				{
					bridge.Destroy();
				}
			}
		}

		[DebugAction(sos2Category, null, false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void PngToShipBlueprint()
		{
			ShipImporter.ImportShipDesign(placeActualBuildings:false);
		}
		[DebugAction(sos2Category, null, false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void PngToShipBuildings()
		{
			ShipImporter.ImportShipDesign(placeActualBuildings:true);
		}
		[DebugAction(sos2Category, null, false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void PngToNanameBlueprint()
		{
			ShipImporter.ImportShipDesign(placeActualBuildings: true, useNanameWalls:true);
		}

		[DebugAction(sos2Category, null, false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void AddColoredFloor()
		{
			Map map = Find.CurrentMap;
			ShipPainter.PaintFloorAndBuildings(map, useBuiltInTexture:false);
		}
	}
}
