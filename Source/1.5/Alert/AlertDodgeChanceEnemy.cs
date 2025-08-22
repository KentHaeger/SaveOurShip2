using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace SaveOurShip2
{
	public class AlertDodgeChanceEnemy : AlertDodgeChanceBase
	{
		protected override Map GetMapSub()
		{
			Map playerMap = ShipInteriorMod2.FindPlayerShipMap();
			if (playerMap == null)
			{
				return null;
			}
			ShipMapComp playerMapComp = playerMap.GetComponent<ShipMapComp>();
			if (playerMapComp.ShipMapState != ShipMapState.inCombat)
			{
				return null;
			}
			return playerMapComp.TargetMapComp.map;
		}

		public AlertDodgeChanceEnemy()
		{
			actorXMLKey = "SoS.ActorEnemy";
		}
	}
}

