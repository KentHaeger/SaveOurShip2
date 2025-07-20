using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SaveOurShip2
{
    public class HediffCompProperties_GearRepair : HediffCompProperties
    {
		public int ticksPerRepair;

		public HediffCompProperties_GearRepair()
		{
			compClass = typeof(HediffComp_GearRepair);
		}
	}
}
