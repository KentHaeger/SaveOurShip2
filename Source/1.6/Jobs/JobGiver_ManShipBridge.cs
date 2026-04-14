using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using RimWorld;

namespace SaveOurShip2
{
	public class JobGiver_ManShipBridge : ThinkNode_JobGiver
	{

		public float maxDistFromPoint = -1f;

		public override ThinkNode DeepCopy(bool resolve = true)
		{
			JobGiver_ManShipBridge obj = (JobGiver_ManShipBridge)base.DeepCopy(resolve);
			obj.maxDistFromPoint = maxDistFromPoint;
			return obj;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			// Leaving legacy check in place
			if (pawn.Map == null)
			{
				return null;
			}
			ShipMapComp mapComp = pawn.Map.GetComponent<ShipMapComp>();
            if (pawn.Faction != Faction.OfPlayer && mapComp.ShipMapState == ShipMapState.isGraveyard)
			{
				// No point in manning bridge by non-player pawns at graveyards (but player can man bridge there to capture trophy)
				return null;
			}

			Predicate<Thing> validator = delegate (Thing t)
			{
				if (t.def.hasInteractionCell && t.def.HasComp(typeof(CompMannable)) && t.Faction == pawn.Faction && pawn.CanReserve(t))
					return true;
				return false;
			};
			// Optimization, doing search using existing data structures, could obviusly write shorter
            Thing closestBridge = null;
			int closestDistanceSquared = int.MaxValue;
			foreach (SpaceShipCache ship in mapComp.ShipsOnMap.Values)
			{
				foreach(Building_ShipBridge bridge in ship.Bridges)
				{
					int distanceSquared = (pawn.Position - bridge.Position).LengthHorizontalSquared;
					if (distanceSquared < closestDistanceSquared && validator(bridge))
					{
						closestDistanceSquared = distanceSquared;
						closestBridge = bridge;
					}
				}
			}
			/*if (closestBridge == null)
			{
				// This fallback is really slow with the number of buildings large enemy ships have
				closestBridge = GenClosest.ClosestThingReachable(GetRoot(pawn), pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.InteractionCell, TraverseParms.For(pawn), maxDistFromPoint, validator);
			}*/
			if (closestBridge != null)
			{
				Job job = JobMaker.MakeJob(ResourceBank.JobDefOf.ManShipBridge, closestBridge);
				job.expiryInterval = 2000;
				job.checkOverrideOnExpire = true;
				return job;
			}
			return null;
		}

		protected IntVec3 GetRoot(Pawn pawn)
		{
			return pawn.Position;
		}
	}
}
