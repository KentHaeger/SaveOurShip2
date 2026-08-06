using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vehicles;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace SaveOurShip2
{
	class LordToil_DefendShip : LordToil
	{
		public IntVec3 baseCenter;

		public override IntVec3 FlagLoc => baseCenter;

		public LordToil_DefendShip(IntVec3 baseCenter)
		{
			this.baseCenter = baseCenter;
		}

		public override void UpdateAllDuties()
		{
			for (int i = 0; i < lord.ownedPawns.Count; i++)
			{
                AssignDutyTo(lord.ownedPawns[i]);
			}
		}
        public override void Notify_PawnJobDone(Pawn p, JobCondition condition)
        {
			if(p.mindState.duty == null)
			{
                AssignDutyTo(p);
            }
        }
        private void AssignDutyTo(Pawn pawn)
        {
            if (!(pawn is VehiclePawn))
            {
                pawn.mindState.duty = new PawnDuty(ResourceBank.DutyDefOf.SoSDefendShip, baseCenter);
            }
            else
            {
                pawn.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.TravelOrWaitVehicle);
            }
        }
    }
}
