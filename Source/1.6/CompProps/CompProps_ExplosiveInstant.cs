using System;
using RimWorld;

namespace SaveOurShip2
{
	class CompProperties_ExplosiveInstant : CompProperties_Explosive
	{
		public CompProperties_ExplosiveInstant()
		{
			compClass = typeof(CompExplosiveInstant);
		}
	}
}
