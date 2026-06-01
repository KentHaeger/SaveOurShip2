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
	public class AlertMovementBlocked : Alert
	{
        public AlertMovementBlocked()
			: base()
		{
		}

		public override AlertReport GetReport()
		{
			Map playerMap = ShipInteriorMod2.FindPlayerShipMap();
			Map currentMap = Find.CurrentMap;
			if (playerMap == null || currentMap == null || currentMap != playerMap)
			{
				return AlertReport.Inactive;
			}
			ShipMapComp mapComp = playerMap.GetComponent<ShipMapComp>();
			if (mapComp.ShipMapState != ShipMapState.inCombat)
			{
				return AlertReport.Inactive;
			}
			float twr = mapComp.SlowestThrustRatio(out SpaceShipCache blockingShip);
			if (twr < Mathf.Epsilon)
			{
				List<Thing> culprits = new List<Thing>();
				if (blockingShip != null)
				{
					if (blockingShip.Core != null)
					{
						culprits.Add(blockingShip.Core);
					}
					else if (!blockingShip.Buildings.NullOrEmpty())
					{
						// This is mainly for identifying where a small 10-tiles wrek or so on a large map is,
						// so picking just any part is ok.
						culprits.Add(blockingShip.Buildings.First());
					}
				}
				return AlertReport.CulpritsAre(culprits);
			}
			return AlertReport.Inactive;
		}

        public override string GetLabel()
		{
			return "SoS.Alert.MovemenyBlocked".Translate();
		}

        public override TaggedString GetExplanation()
		{
			return "SoS.Alert.MovemenyBlockedDesc".Translate();
		}
	}
}

