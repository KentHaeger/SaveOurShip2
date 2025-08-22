﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace SaveOurShip2
{
	class JobDriver_RepairSatellite : JobDriver
	{
		float workDone;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(TargetA, job);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			if (TargetA != LocalTargetInfo.Invalid)
				this.FailOnDespawnedOrNull(TargetIndex.A);
			yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.ClosestTouch);
			Toil hackIt = Toils_General.Wait(2000, TargetA != LocalTargetInfo.Invalid ? TargetIndex.A : TargetIndex.None);
			hackIt.defaultCompleteMode = ToilCompleteMode.Delay;
			hackIt.initAction = delegate
			{
				workDone = 0;
			};
			hackIt.tickAction = delegate
			{
				workDone++;
			};
			hackIt.endConditions = new List<Func<JobCondition>>();
			hackIt.WithProgressBar(TargetIndex.A,() => workDone/2000f);
			hackIt.WithEffect(base.TargetThingA.def.repairEffect, TargetIndex.A);
			hackIt.AddFinishAction(delegate {
				if (workDone >= 1990 && pawn.health.State == PawnHealthState.Mobile && TargetA.HasThing && !TargetA.Thing.DestroyedOrNull() && TargetA.Thing is Building_SatelliteCore)
				{
					((Building_SatelliteCore)TargetA.Thing).RepairMe(pawn);
				}
			});
			yield return hackIt;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<float>(ref workDone, "WorkDone");
		}
	}
}
