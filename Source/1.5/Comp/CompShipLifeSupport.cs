﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	public class CompShipLifeSupport : ThingComp
	{
		public bool active = false;
		CompPowerTrader powerComp;
		CompFlickable flickComp;
		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			powerComp = parent.TryGetComp<CompPowerTrader>();
			flickComp = parent.TryGetComp<CompFlickable>();
			if (powerComp.PowerOn && flickComp.SwitchIsOn)
				active = true;
			//Log.Message("Spawned LS: " + this.parent + " on map: " + this.parent.Map);
		}
		public override void CompTick()
		{
			base.CompTick();
			if (Find.TickManager.TicksGame % 360 == 0)
			{
				if (powerComp.PowerOn && flickComp.SwitchIsOn)
					active = true;
				else
					active = false;
			}
		}
		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<bool>(ref active, "active", false);
		}

		public override void ReceiveCompSignal(string signal)
		{
			if (parent.Map.IsSpace())
			{
				if (signal == "PowerTurnedOff" || signal == "FlickedOff")
					parent.Map.GetComponent<ShipMapComp>().breathableZoneDirty = true;
				else if (signal == "PowerTurnedOn" || signal == "FlickedOn")
					parent.Map.GetComponent<ShipMapComp>().breathableZoneDirty = true;
			}
		}
	}
}
