using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;
using Vehicles;

namespace SaveOurShip2
{
	class LordToil_AssaultShip : LordToil
	{
		private bool attackDownedIfStarving;
		private bool canPickUpOpportunisticWeapons;
		public override bool ForceHighStoryDanger
		{
			get
			{
				return true;
			}
		}
		public LordToil_AssaultShip(bool attackDownedIfStarving = false, bool canPickUpOpportunisticWeapons = false)
		{
			this.attackDownedIfStarving = attackDownedIfStarving;
			this.canPickUpOpportunisticWeapons = canPickUpOpportunisticWeapons;
		}
		public override bool AllowSatisfyLongNeeds
		{
			get
			{
				return false;
			}
		}
		public override void Init()
		{
			base.Init();
			LessonAutoActivator.TeachOpportunity(ConceptDefOf.Drafting, OpportunityType.Critical);
		}
		public override void UpdateAllDuties()
		{
			for (int i = 0; i < this.lord.ownedPawns.Count; i++)
			{
                AssignDutyTo(this.lord.ownedPawns[i]);

			}
		}

		private void AssignDutyTo(Pawn pawn)
		{
            if (!(pawn is VehiclePawn))
            {
                pawn.mindState.duty = new PawnDuty(ResourceBank.DutyDefOf.SoSAssaultShip);
                pawn.mindState.duty.attackDownedIfStarving = this.attackDownedIfStarving;
                pawn.mindState.duty.pickupOpportunisticWeapon = this.canPickUpOpportunisticWeapons;
            }
            else
            {
                pawn.mindState.duty = new PawnDuty(DefDatabase<DutyDef>.GetNamed("TravelOrWaitVehicle"));
            }
        }
	}
}
