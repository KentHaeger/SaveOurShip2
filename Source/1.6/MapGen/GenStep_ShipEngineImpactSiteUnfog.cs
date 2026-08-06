using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
    class GenStep_ShipEngineImpactSiteUnfog : GenStep
	{
        public override int SeedPart => 87240651;
        public override void Generate(Map map, GenStepParams parms)
		{
            foreach(Building b in map.listerBuildings.allBuildingsNonColonist)
            {
                if (b.def == ResourceBank.ThingDefOf.JTDriveSalvage)
                {
                    foreach (IntVec3 tile in b.OccupiedRect())
                    {
                        map.fogGrid.Unfog(tile);
                    }
                }
            }
		}
	}
}
