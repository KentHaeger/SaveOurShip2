using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace SaveOurShip2
{
	public class AlertTWR : Alert
	{
        public AlertTWR()
			: base()
		{
			
		}

		public override AlertReport GetReport()
		{
			Map playerMap = ShipInteriorMod2.FindPlayerShipMap();
			Map enemyMap = ShipInteriorMod2.FindEnemyShipMap();
			Map currentMap = Find.CurrentMap;
			if (playerMap == null || enemyMap == null|| currentMap == null ||
				(currentMap != playerMap && currentMap != enemyMap))
			{
				return AlertReport.Inactive;
			}
			ShipMapComp playerMapComp = playerMap.GetComponent<ShipMapComp>();
			ShipMapComp enemyMapComp = enemyMap.GetComponent<ShipMapComp>();
			if (playerMapComp.ShipMapState != ShipMapState.inCombat)
			{
				return AlertReport.Inactive;
			}
			// Saving alert space: if player movement is blocked alers shows that and culprit wrecks.
			// if not, one alert shows both enemy and player TWRs.
			float playerTWR = playerMapComp.SlowestThrustRatio(out SpaceShipCache blockingShip, out SpaceShipCache slowestPlayerShip);
			if (playerTWR < Mathf.Epsilon)
			{
				defaultPriority = AlertPriority.High;
				defaultLabel = "SoS.Alert.MovemenyBlocked".Translate();
				defaultExplanation = "SoS.Alert.MovemenyBlockedDesc".Translate();
				List<Thing> culprits = blockingShip?.GetCulpritsForAlert() ?? new List<Thing>();
				return AlertReport.CulpritsAre(culprits);
			}
			else
			{
				// Player can move
				defaultPriority = AlertPriority.Medium;
				float enemyTWR = enemyMapComp.SlowestThrustRatio(out SpaceShipCache blockingShipEnemy, out SpaceShipCache slowestEnemyShip);
				defaultLabel = "SoS.Alert.TWR".Translate(playerTWR.ToString("F2"), enemyTWR.ToString("F2"));
				string playerSlowestLabel = slowestPlayerShip != null ? slowestPlayerShip.GetLabel() : "";
				string enemySlowestLabel = slowestEnemyShip != null ? slowestEnemyShip.GetLabel() : "";
				defaultExplanation = "SoS.Alert.TWRDesc".Translate(playerSlowestLabel, enemySlowestLabel);
				List<Thing> culprits = slowestPlayerShip?.GetCulpritsForAlert() ?? new List<Thing>();
				return AlertReport.CulpritsAre(culprits);
			}
		}
	}
}

