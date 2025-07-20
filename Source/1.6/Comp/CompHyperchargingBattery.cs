using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;


namespace SaveOurShip2
{
	// Battery that charges itself at overpowered rate. To be used in Junebox mod, for testing purposes.
	class CompHyperchargingBattery : CompPowerBattery
	{
		public override void CompTick()
		{
			base.CompTick();
			const float WattDaysPerTick = 1.67E-05f;
			AddEnergy(WattDaysPerTick * 1E8f);
		}
	}
}
