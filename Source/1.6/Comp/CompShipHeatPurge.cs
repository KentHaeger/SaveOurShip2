﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace SaveOurShip2
{
	[StaticConstructorOnStartup]
	public class CompShipHeatPurge : CompShipHeatSink
	{
		static readonly float HEAT_PURGE_RATIO = 20;

		public bool purging = false;
		bool hiss = false;
		public ShipMapComp mapComp;
		public CompRefuelable fuelComp;

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<bool>(ref purging, "purging");
		}
		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			mapComp = parent.Map.GetComponent<ShipMapComp>();
			fuelComp = parent.TryGetComp<CompRefuelable>();
		}
		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			List<Gizmo> giz = new List<Gizmo>();
			giz.AddRange(base.CompGetGizmosExtra());
			if (parent.Faction == Faction.OfPlayer)
			{
				Command_Toggle purge = new Command_Toggle
				{
					toggleAction = delegate
					{
						if (purging)
							purging = false;
						else
							StartPurge();
					},
					isActive = delegate { return purging; },
					defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.PurgeHeat"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.PurgeHeatDesc"),
					icon = ContentFinder<Texture2D>.Get("UI/HeatPurge")
				};
				giz.Add(purge);
			}
			return giz;
		}
		public override void CompTick()
		{
			base.CompTick();
			if (!parent.Spawned || parent.Destroyed || myNet == null)
			{
				return;
			}
			if (purging)
			{
				if (CanPurge() && fuelComp.Fuel > 0 && RemHeatFromNetwork(Props.heatPurge * HEAT_PURGE_RATIO))
				{
					fuelComp.ConsumeFuel(Props.heatPurge);
					FleckMaker.ThrowAirPuffUp(parent.DrawPos + new Vector3(0, 0, 1), parent.Map);
					if (!hiss)
					{
						ResourceBank.SoundDefOf.ShipPurgeHiss.PlayOneShot(parent);
						hiss = true;
					}
				}
				else
				{
					purging = false;
				}
			}
		}
		public void StartPurge()
		{
			purging = true;
			hiss = false;

			foreach (CompShipHeatShield shield in myNet.Shields.Where(s => !s.shutDown))
			{
				shield.flickComp.SwitchIsOn = false;
				shield.shutDown = true;
			}
			foreach (Building_ShipCloakingDevice cloak in mapComp.Cloaks)
			{
				if (cloak.active && cloak.Map == parent.Map)
				{
					cloak.flickComp.SwitchIsOn = false;
					cloak.active = false;
				}
			}
			// If still under active shield after disabling shields in own heat net, it means covered by shield from other net/other ship
			// Won't disable that, because player might not understand thing with other net and may not turn other net shields back on.
			// So, just warn instead
			CompShipHeatShield coveringShield = GetActiveShieldCovering();
			if (coveringShield != null)
			{
				Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.CantPurge"), coveringShield.parent, MessageTypeDefOf.NegativeEvent);
			}
		}

		public CompShipHeatShield GetActiveShieldCovering()
		{
			foreach (CompShipHeatShield shield in mapComp.Shields)
			{
				if (!shield.shutDown && (parent.DrawPos - shield.parent.DrawPos).magnitude < shield.radius)
				{
					return shield;
				}
			}
			return null;
		}

		public bool CanPurge()
		{
			if (GetActiveShieldCovering() != null)
			{
				return false;
			}
			if (mapComp.ShipMapState != ShipMapState.inCombat)
			{
				foreach (Building_ShipCloakingDevice cloak in mapComp.Cloaks)
				{
					if (cloak.active && cloak.Map == parent.Map)
					{
						return false;
					}
				}
			}
			return true;
		}
	}
}
