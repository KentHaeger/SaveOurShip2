using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using Vehicles;

namespace SaveOurShip2
{
	public class Projectile_ExplosiveShip : Projectile_Explosive
	{
		protected override void Impact(Thing hitThing, bool blockedByShield = false)
		{
			// Projectile position can be inaccurate by several tiles if it travels several tiles per tick, Verse issue
			// The projectile will be "behind" actual hit location along it's trajectory. Like it had a proximity fuse or something.
			// that affects ship weapon projectiles with high speed, but lower explosion radius.
			// To fix that, base.Position is changed as it is used as explosion center and ExactPosition is adjusted too - used in some visual/sound effects

			// Only apply fix to ship-to-ship weapons, as simplified hit check is used. Just not change things wthin map for now. 
			if (hitThing != null && Launcher != null && Launcher.Map == hitThing.Map)
			{
				base.Impact(hitThing, blockedByShield);
				return;
			}
			// Because this is an issue fix placed where it fits, simply excluding torpedoes so that their
			// proximity fuse is not affected
			if (this is Projectile_ExplosiveShipTorpedo)
			{
				base.Impact(hitThing, blockedByShield);
				return;
			}

			// for really small explosion radius, just move explosion into hit thing, because otherwise
			// they are not going to deal damage at all when esploding in adjacent cell
			if (def.projectile.explosionRadius < 1.1)
			{
				if (hitThing != null)
				{
					Position = hitThing.Position;
					base.Impact(hitThing, blockedByShield);
					return;
				}
			}

			// Base game uses 0.2 tile lenth advance along all projectile path,
			// smaller step when re-checking just last few tiles shuld be fine.
			const int stepsPerTile = 20;
			int advanceLength = (int)Math.Ceiling(def.projectile.SpeedTilesPerTick);
			Vector3 vecAdvance = (destination - origin).normalized/stepsPerTile;
			Vector3 currentPosition = Position.ToVector3();
			IntVec3 prevTile = currentPosition.ToIntVec3();
			// current tile = analyzed, may be occupied. Prev tile = unoccupied, but close proximity, diagonally or cardinally adjacent
			// tile even before previous is saved for larger explosions. 
			IntVec3 prevPrevTile = prevTile;
			ShipMapComp mapComp = base.Map.GetComponent<ShipMapComp>();
			for (int i = 0; i < advanceLength * stepsPerTile; i++)
			{
				IntVec3 newTile = (currentPosition + vecAdvance).ToIntVec3();
				if (newTile == prevTile)
				{
					currentPosition += vecAdvance;
					continue;
				}
				if (mapComp.ShipIndexOnVec(newTile) != -1)
				{
					break;
				}
				if (!newTile.InBounds(base.Map, 0))
				{
					break;
				}
				// Do not advance past destination cell
				if (currentPosition.ToIntVec3() == DestinationCell)
				{
					break;
				}

				List<Thing> thingList = newTile.GetThingList(base.Map);
				bool hitBuildingOrVehicle = false;
				foreach (Thing t in thingList)
				{
					// Simplified check here, with main purpose of not allowing pre-mature explosion that damages nothing
					// if there is something real, rather than sleeping spot etc, that something is ok to be hit.
					if ((t is Building b && !b.IsClearableFreeBuilding) || t is VehiclePawn)
					{
						hitBuildingOrVehicle = true;
						break;
					}
				}
				if (hitBuildingOrVehicle)
				{
					break;
				}
				currentPosition += vecAdvance;
				prevPrevTile = prevTile;
				prevTile = newTile;
			}
			// Overide position with latest position before hitting anythiong in loop above
			// Should not put new explosion poition into hit ship hull (likely), as it 
			// afffects efficiency of explosives with large radius. If pushed into hit thing,
			// they are going to destroy oly that tiler and 2 adjacent, despite much larger actual radius 
			if (def.projectile.explosionRadius < 2.9)
			{
				Position = currentPosition.ToIntVec3();
			}
			else
			{
				// For larger explosins, advancing to directly adjacent tile may be bad. 
				// For example, large plasma explosion, when advanced into tile directly adjacent to diagonal wall,
				// will damage only 2 walls - that's how explosion works
				// here go 2.9 and larger explosions - large enough radius to fully cover 5x5 square.
				Position = prevPrevTile;
			}

			base.Impact(hitThing, blockedByShield);
		}
		public override void Tick()
		{
			base.Tick();
			//Replaced by new Harmony shield patch
			/*if (this.Spawned)
			{
				foreach (CompShipHeatShield shield in this.Map.GetComponent<ShipMapComp>().Shields)
				{
					if (!shield.shutDown && Position.DistanceTo(shield.parent.Position) <= shield.radius)
					{
						shield.HitShield(this);
						break;
					}
				}
			}*/
			if (!(this is Projectile_ExplosiveShipPsychic))
			{
				if (this.Spawned && this.ExactPosition.ToIntVec3().GetThingList(this.Map).Any(t => t is Building b && (b.TryGetComp<CompShipCachePart>()?.Props.isPlating ?? false)))
				{
					Explode();
				}
			}
		}

		protected override void Explode()
		{
			this.Map.GetComponent<ShipMapComp>().AccuracyCalc?.RegisterExplosion(this);
			base.Explode();
		}

		public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
		{
			ShipMapComp mapComp = this.Map.GetComponent<ShipMapComp>();
			mapComp.AccuracyCalc?.RegisterDespawn(this);
			mapComp.incomingProjectiles.Remove(this);
			base.Destroy(mode);
		}

		/*
		public override Vector3 ExactPosition
		{
			get
			{
				Vector3 b = (destination - origin) * Mathf.Clamp01(1f - ((float)ticksToImpact + 5) / StartingTicksToImpact); //Proximity fuze!
				return origin + b + Vector3.up * def.Altitude;
			}
		}*/
	}
}
