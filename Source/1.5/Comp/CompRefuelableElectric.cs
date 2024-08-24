using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	class CompRefuelableElectric : CompRefuelable
	{
		public CompPowerBattery battery;
		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			battery = parent.GetComp<CompPowerBattery>();
		}
		public override void CompTick()
		{
			base.CompTick();
			if (battery == null)
			{
				return;
			}
			if (Find.TickManager.TicksGame % 30 == 0)
			{
				fuel = battery.StoredEnergy;
			}
		}
	}
}
