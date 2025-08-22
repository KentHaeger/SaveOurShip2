using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;

namespace SaveOurShip2
{
	class JobDriver_HackLabConsole : JobDriver
	{
		float workDone;
		static readonly int workAmount = 2000;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(TargetA, job);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			if (TargetA != LocalTargetInfo.Invalid)
				this.FailOnDespawnedOrNull(TargetIndex.A);
			yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.InteractionCell);
			int EstimateWorkTime = Mathf.CeilToInt(workAmount / JobUtility.GetHackSpeedClamped(pawn));
			Toil hackIt = Toils_General.Wait(EstimateWorkTime, TargetA != LocalTargetInfo.Invalid ? TargetIndex.A : TargetIndex.None);
			hackIt.defaultCompleteMode = ToilCompleteMode.Delay;
			hackIt.initAction = delegate
			{
				workDone = 0;
			};
			hackIt.tickAction = delegate
			{
				workDone += JobUtility.GetHackSpeed(pawn);
			};
			hackIt.endConditions = new List<Func<JobCondition>>();
			hackIt.WithProgressBar(TargetIndex.A, () => workDone / workAmount);
			hackIt.WithEffect(EffecterDefOf.Research, TargetIndex.A);
			hackIt.AddFinishAction(delegate {
				if (workDone >= workAmount - JobUtility.WorkFinishThreshold && pawn.health.State == PawnHealthState.Mobile && TargetA.HasThing && !TargetA.Thing.DestroyedOrNull() && TargetA.Thing is Building)
				{
					((Building)TargetA.Thing).GetComp<CompBlackBoxConsole>().HackMe(pawn);
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
