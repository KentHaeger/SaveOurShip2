using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;

namespace SaveOurShip2
{
	class JobDriver_AdmireSpace : JobDriver
	{
		public Building telescope => (Building)base.TargetThingA;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(base.TargetA, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedOrNull(TargetIndex.A);
			this.FailOnSomeonePhysicallyInteracting(TargetIndex.A);
			this.FailOnChildLearningConditions();
			this.FailOn(() => telescope.IsForbidden(pawn));
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell).FailOn(() => telescope.IsForbidden(pawn));
			Toil toil = ToilMaker.MakeToil("MakeNewToils");
			toil.tickAction = delegate
			{
				pawn.rotationTracker.FaceTarget(base.TargetA);
				LearningUtility.LearningTickCheckEnd(pawn);
			};
			// toil.WithEffect(EffecterDefOf.Radiotalking, TargetIndex.A);
			toil.handlingFacing = true;
			toil.defaultCompleteMode = ToilCompleteMode.Never;
			yield return toil;
		}
	}
}
