using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace SaveOurShip2
{
	// Becase base game creates one instannce of all lert leaf subclasses, 
	// got to make 2 different classes for player dodge chance and enemy dodge chance
	public abstract class AlertDodgeChanceBase : Alert
	{
		// To be overridden/set in subclasses
		protected abstract Map GetMapSub();
		// To be set in subclass, key to get Enemy or Player word from XML
		protected string actorXMLKey;
		protected Map map
		{
			get
			{
				return GetMapSub();
			}
		}
		// default label fully managed by descendants
		public AlertDodgeChanceBase()
		{
			defaultPriority = AlertPriority.High;
		}

		protected bool IsActive()
		{
			return (map != null) && (map.GetComponent<ShipMapComp>().ShipMapState == ShipMapState.inCombat);
		}

		public override AlertReport GetReport()
		{
			if (!IsActive())
			{
				return false;
			}
			string actor = TranslatorFormattedStringExtensions.Translate(actorXMLKey);
			defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.DodgeChanceAlert", actor, AverageDodgeChance.ToString("P0"));
			// Alert's main purpose is to explain dodge chance mechanics in tooltip.
			// Can't really point to someting specific in relation to a feature that works map-wide
			return AlertReport.CulpritsAre(new List<GlobalTargetInfo>() { new GlobalTargetInfo(map.Center, map)});
		}

		public override TaggedString GetExplanation()
		{
			// StringBuilder result = new StringBuilder();
			return TranslatorFormattedStringExtensions.Translate("SoS.DodgeChanceExplanation",
				 LongRangedWeaponDodgeChance.ToString("P0"), AverageDodgeChance.ToString("P0"), ShortRangedWeaponDodgeChance.ToString("P0"));

		}
		protected float AverageDodgeChance
		{
			get
			{
				if (!IsActive())
				{
					// Should look as error if gets to UI
					return -1f;
				}
				return map.GetComponent<ShipMapComp>().AccuracyCalc.AverageDodgeChance;
			}
		}

		protected float LongRangedWeaponDodgeChance
		{
			get
			{
				if (!IsActive())
				{
					// Should look as error if gets to UI
					return -1f;
				}
				return map.GetComponent<ShipMapComp>().AccuracyCalc.LongRangedWeaponDodgeChance;
			}
		}

		protected float ShortRangedWeaponDodgeChance
		{
			get
			{
				if (!IsActive())
				{
					// Should look as error if gets to UI
					return -1f;
				}
				return map.GetComponent<ShipMapComp>().AccuracyCalc.ShortRangedWeaponDodgeChance;
			}
		}
	}
}

