using System;
using RimWorld;

namespace SaveOurShip2
{
	class CompExplosiveInstant : CompExplosive
	{
		public override void CompTick()
		{
			if (parent.Spawned)
			{
				Detonate(parent.MapHeld);
			}
		}
	}
}
