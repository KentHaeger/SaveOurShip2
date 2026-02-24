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
	// Becase base game creates one instannce of all lert leaf subclasses, 
	// got to make 2 different classes for player dodge chance and enemy dodge chance
	public class AlertNotoriety : Alert
	{
        public AlertNotoriety()
			: base()
		{
		}

		public override AlertReport GetReport()
		{
			if (ShipInteriorMod2.WorldComp.PlayerFactionBounty <= 0)
			{
				return false;
			}
			Map map = ShipInteriorMod2.FindPlayerShipMap();
			if (map != null)
			{
				ShipMapComp mapComp = map.GetComponent<ShipMapComp>();
				IEnumerable<SpaceShipCache> ships = mapComp.ShipsOnMap.Values.Where(s => !s.IsWreck);
				SpaceShipCache flagship = ships.MaxBy(s => s.MassActual);
				List<Thing> culprits = new List<Thing>{ flagship.Core };
                return AlertReport.CulpritsAre(culprits);
			}
			return true;
		}

        public override string GetLabel()
		{
			return TranslatorFormattedStringExtensions.Translate("SoS.Alert.NotoriertyLabel", ShipInteriorMod2.WorldComp.PlayerFactionBounty);
		}

        public override TaggedString GetExplanation()
		{
			if(ShipInteriorMod2.WorldComp.NotorietyActive)
			{
                return TranslatorFormattedStringExtensions.Translate("SoS.Alert.NotoriertyHighDesc", GenDate.TicksToDays(ShipInteriorMod2.WorldComp.TicksBetweenNotorietyAttacks).ToString("F1"));
            }
			else
			{
				return TranslatorFormattedStringExtensions.Translate("SoS.Alert.NotoriertyLowDesc");
            }
		}
	}
}

