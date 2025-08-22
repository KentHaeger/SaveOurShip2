﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	class AttackableShip : PassingShip
	{
		public ShipDef attackableShip;
		public Faction shipFaction;
		public NavyDef spaceNavyDef;

		public AttackableShip() : base()
		{
			loadID = Find.UniqueIDsManager.GetNextPassingShipID();
			ticksUntilDeparture = Rand.RangeInclusive(60000, 120000);
		}

		protected override AcceptanceReport CanCommunicateWith(Pawn negotiator)
		{
			return "This ship refuses your hails";
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Defs.Look(ref attackableShip, "EnemyShip");
			Scribe_Defs.Look(ref spaceNavyDef, "spaceNavyDef");
			Scribe_References.Look<Faction>(ref shipFaction, "shipFaction", false);
		}
		public override string FullTitle
		{
			get
			{
				if (attackableShip != null)
					return (loadID + " " + attackableShip.label);
				return "Glitched ship";
			}
		}
		public override string GetCallLabel()
		{
			return attackableShip != null ? attackableShip.label : "Glitched ship";
		}
	}
}
