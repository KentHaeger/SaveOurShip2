using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vehicles;
using Verse;

namespace SaveOurShip2
{
    class CompShuttleAutoDoc : ThingComp
    {
        CompProps_ShuttleAutoDoc Props => (CompProps_ShuttleAutoDoc)props;

        public override void CompTickRare()
        {
            base.CompTickRare();
            if(parent is VehiclePawn vehicle)
            {
                List<Pawn> pawns = vehicle.AllPawnsAboard.ListFullCopy();
                List<Thing> cargoPawns = vehicle.inventory.innerContainer.Where(t => (t is Pawn)).ToList();
                pawns.AddRange(cargoPawns.ConvertAll(t => (Pawn)t));
                foreach (Pawn pawn in pawns)
                {
                    Hediff bleed = HealthUtility.FindMostBleedingHediff(pawn, new HediffDef[] { });
                    if (bleed != null)
                        bleed.Tended(Props.tendQualityRange.RandomInRange, Props.tendQualityRange.TrueMax);
                }
            }
        }
    }
}
