using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Verse;
using System;

namespace SaveOurShip2
{
	// Handles possibility of collision, tick interval (cooldown) between collision, messages.
	public class ShipCollisionManager : IExposable
	{
		private int lastCollisionTick = -GenDate.TicksPerDay;
		private bool showedWarningMessage = false;
		private float previusRange = ShipMapComp.MaxBattleRange;
		private ShipMapComp originMapComp;
		private const float warningRange = ShipMapComp.PDWeaponRange / 2f;
		public ShipMapComp OriginMapcomp
		{
			get => originMapComp;
			set => originMapComp = value;
		}
		public void ExposeData()
		{
			Scribe_Values.Look<int>(ref lastCollisionTick, "lastCollisionTick", -GenDate.TicksPerDay);
			Scribe_Values.Look<bool>(ref showedWarningMessage, "showedWarningMessage", false);
			Scribe_Values.Look<float>(ref previusRange, "previusRange", ShipMapComp.MaxBattleRange);
		}

		public ShipCollisionManager()
		{
		}

		public void RangeUpdated()
		{
			Assert.IsNotNull(originMapComp);
			checkAndShowWarning();
			checkAndHandleCollision();
		}
		private void checkAndShowWarning()
		{
			// Shown once per battle via re-creation of this manger
			if (showedWarningMessage)
			{
				return;
			}
			if (previusRange > warningRange && originMapComp.Range < warningRange)
			{
				showedWarningMessage = true;
				Messages.Message("SoS.Collision.WarningMessage".Translate(), null, MessageTypeDefOf.CautionInput);
			}
		}
		private void checkAndHandleCollision()
		{
			// Sadly, Odyssey has very different ship impact feature. So this got to be locked behind DLC, just in case.
			if (!ModsConfig.OdysseyActive)
			{
				return;
			}
			// Ramming ships with a frequence of some power tool is not considered reasonable,
			// so there is a cooldown between collisions.
			const int collisionTickInterval = 600;
			if (Find.TickManager.TicksGame - lastCollisionTick < collisionTickInterval)
			{
				return;
			}
			lastCollisionTick = Find.TickManager.TicksGame;
			const float collisionEpsion = 10;
			if (Mathf.Abs(originMapComp.Range) < collisionEpsion)
			{
				doCollision();
			}
		}
		private void doCollision()
		{
			// For now, collsion isn't something that persists or has state
			// but rather insta-damage and insta-bounce off and that's it.
			Messages.Message("SoS.Collision.Message".Translate(), null, MessageTypeDefOf.CautionInput);
			originMapComp.Range = warningRange;

			ShipMapComp targetMapComp = originMapComp.TargetMapComp;
			SpaceShipCache playerShip = getCollidingShipOnMap(originMapComp);
			SpaceShipCache enemyShip = getCollidingShipOnMap(targetMapComp);

			// Extremeny unlikely to happen
			if (playerShip == null || enemyShip == null)
			{
				Log.ErrorOnce($"SoS 2: Coudn't find ships for collision. Player ship: {playerShip}, enemy ship: {enemyShip}", 94417531);
				return;
			}

			float playerTWR = originMapComp.SlowestThrustToWeightCached() * TWRMath.TWRLargeMultiplier;
			float enemyTWR = targetMapComp.SlowestThrustToWeightCached() * TWRMath.TWRLargeMultiplier;

			// As 1 means advance and -1 means retreat in heading, this is the relative speed
			float collisionSpeed = Mathf.Abs(playerTWR * originMapComp.Heading + enemyTWR * targetMapComp.Heading);

			inflictDamage(playerShip, originMapComp, enemyShip, targetMapComp, collisionSpeed);
		}
		// For balancing reasons, the initial iddea is to not let fast and heavy ships absolutely smash very small ships.
		private static readonly SimpleCurve damageMultiplerFromTargetWeight = new SimpleCurve
		{
			new CurvePoint(10f, 0.1f ),
			new CurvePoint(100f, 0.3f),
			new CurvePoint(300f, 0.6f),
			new CurvePoint(900f, 1f),
		};
		private void inflictDamage(SpaceShipCache rammingShip, ShipMapComp rammingMapComp, SpaceShipCache targetShip, ShipMapComp targetMapComp, float collisionSpeed)
		{
			// Initial damage scale, value is expected to be lower than mass.
			float damageScale = 0.7f;
			// This denies physics, but in order to make ramming favorable for the player, reflected damage done by larger and slower enemy
			// ship needs to be reduced
			float baseDamage = Mathf.Sqrt(rammingShip.MassActual) * Mathf.Sqrt(targetShip.MassActual) * damageScale;

			// Speed factor is good for the player, so allow squared speed here
			float speedFactor = collisionSpeed * collisionSpeed;

			const float maxDamage = 10000;

			float damage = Mathf.Clamp(baseDamage * speedFactor, 0, maxDamage);

			doImpactDamage(rammingShip, rammingMapComp, damage);
			doImpactDamage(targetShip, targetMapComp, damage);

		}
		private void doImpactDamage(SpaceShipCache targetShip, ShipMapComp targetMapComp, float damage)
		{
			Rot4 impactRot = Rot4.Invalid; 
			int engineRot = targetMapComp.engineRot;
			if (engineRot != -1)
			{
				// try to hit standing and retreating target into the rear part
				impactRot = new Rot4(engineRot);
				if (targetMapComp.Heading == -1 || targetMapComp.Heading == 0)
				{
					impactRot = impactRot.Opposite;
				}
			}
			if (impactRot == Rot4.Invalid)
			{
				// When no engines, hit into random side
				impactRot = Rot4.Random;
			}

			/*int impactX, impactZ;
			if (impactRot == Rot4.North || impactRot == Rot4.South)
			{
				impactX = targetShip.Center().x;
			}
			if (impactRot == Rot4.East || impactRot == Rot4.West)
			{
				impactZ = targetShip.Center().z;
			}*/

			IntVec3 impactPoint;

			if (impactRot == Rot4.North)
			{
				impactPoint = targetShip.Area.MaxBy(cell => cell.z);
			}
			else if (impactRot == Rot4.South)
			{
				impactPoint = targetShip.Area.MinBy(cell => cell.z);
			}
			else if (impactRot == Rot4.East)
			{
				impactPoint = targetShip.Area.MaxBy(cell => cell.x);
			}
			else // if (impactRot == Rot4.West)
			{
				impactPoint = targetShip.Area.MinBy(cell => cell.x);
			}
			doImpactDamage(targetMapComp, impactPoint, damage);

		}
		private static readonly SimpleCurve radiusCurve = new SimpleCurve
		{
			new CurvePoint(400f, 1.4f ),
			new CurvePoint(600f, 1.8f), // this is more than sqrt(2), so 3x3 tiles affected
			new CurvePoint(900f, 2f),
			new CurvePoint(2500f, 4f),
			new CurvePoint(10000f, 8f),
		};
		private void doImpactDamage(ShipMapComp mapComp, IntVec3 impactPoint, float damage)
		{
			float radius = radiusCurve.Evaluate(damage);
			CellRect explosionArea = CellRect.CenteredOn(impactPoint, Mathf.FloorToInt(radius));
			int radiusSquared = (int)(radius * radius);

			// Simple damage model for now
			List<Thing> halfDamageList = new List<Thing>();
			List<Thing> fullDamageList = new List<Thing>();
			foreach (IntVec3 cell in explosionArea.Where(x => x.DistanceToSquared(impactPoint) <= radiusSquared / 4))
			{
				fullDamageList.AddRange(cell.GetThingList(mapComp.map));
			}
			foreach (IntVec3 cell in explosionArea.Where(x => x.DistanceToSquared(impactPoint) <= radiusSquared && 
														      x.DistanceToSquared(impactPoint) > radiusSquared / 4))
			{
				halfDamageList.AddRange(cell.GetThingList(mapComp.map));
			}
			halfDamageList.RemoveAll(x => fullDamageList.Contains(x));
			doDamageToThingList(mapComp.map, fullDamageList, damage);
			doDamageToThingList(mapComp.map, halfDamageList, damage / 2f);

		}

		private void doDamageToThingList(Map map, IEnumerable<Thing> things, float damage)
		{
			foreach (Thing thing in things)
			{
				if (!thing.Destroyed)
				{
					thing.TakeDamage(new DamageInfo(DamageDefOf.Crush, damage));
					FleckMaker.ThrowDustPuff(thing.Position, map, 2f);
				}
			}
		}

		private SpaceShipCache getCollidingShipOnMap(ShipMapComp mapComp)
		{
			IEnumerable<SpaceShipCache> ships = mapComp.ShipsOnMap.Values.Where(s => !s.IsWreck);
			SpaceShipCache ship = ships.MaxBy(s => s.MassActual);
			if (ship == null)
			{
				// Fallback, allow wrecks
				IEnumerable<SpaceShipCache> shipsAndWrecks = mapComp.ShipsOnMap.Values;
				ship = shipsAndWrecks.MaxBy(s => s.MassActual);
			}
			return ship;
		}
	}
}

