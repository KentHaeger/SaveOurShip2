using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace SaveOurShip2
{
	public class LearningGiver_AdmiringSpace : LearningGiver
	{
		// Cannot work properly because context is not passed to this function.
		// So will just return false, but this desire will be added manually in harmony patch?
		public override bool CanGiveDesire
		{
			get
			{
				return false;
			}
		}

		private bool TryFindTelescope(Pawn pawn, out Thing telescopeResult)
		{
			Thing spaceTelecope = null;
			Thing telecope = null;
			if (pawn != null)
			{
				spaceTelecope = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(ThingDef.Named("TelescopeSpace")), PathEndMode.InteractionCell, TraverseParms.For(pawn), 9999f, (Thing t) => t is Building b && pawn.CanReserve(b) && !b.IsForbidden(pawn));
				telecope = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(ThingDef.Named("Telescope")), PathEndMode.InteractionCell, TraverseParms.For(pawn), 9999f, (Thing t) => t is Building b && pawn.CanReserve(b) && !b.IsForbidden(pawn));
			}
			if (spaceTelecope != null)
			{
				telescopeResult = spaceTelecope;
			}
			else
			{
				telescopeResult = telecope;
			}
			return telescopeResult != null;
		}

		public override bool CanDo(Pawn pawn)
		{
			if (!base.CanDo(pawn))
			{
				return false;
			}
			return TryFindTelescope(pawn, out var telecope);
		}

		public override Job TryGiveJob(Pawn pawn)
		{
			if (!TryFindTelescope(pawn, out var telesope))
			{
				return null;
			}
			return JobMaker.MakeJob(def.jobDef, telesope);
		}
	}
}