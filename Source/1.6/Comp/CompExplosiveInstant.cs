using System;
using RimWorld;

namespace SaveOurShip2
{
	class CompExplosiveInstant : CompExplosive
	{
		public override void CompTick()
		{
			// This means fighter wings won't split into separate fighters with CK active, for editing convenience.
			if (parent.Spawned && !ShipInteriorMod2.HasSoS2CK)
			{
				Detonate(parent.MapHeld);
			}
		}
	}
}
