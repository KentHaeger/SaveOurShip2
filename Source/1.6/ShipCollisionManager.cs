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
		private Map originMap;
		public ShipMapComp OriginMapComp
		{
			get
			{
				Assert.IsNotNull(originMap);
				return originMap.GetComponent<ShipMapComp>();
			}
			set
			{
				originMap = value.map;
			}
		}
		public void ExposeData()
		{
			Scribe_Values.Look<int>(ref lastCollisionTick, "lastCollisionTick", -GenDate.TicksPerDay);
			Scribe_Values.Look<bool>(ref showedWarningMessage, "showedWarningMessage", false);
			Scribe_Values.Look<float>(ref previusRange, "previusRange", ShipMapComp.MaxBattleRange);
			Scribe_References.Look<Map>(ref originMap, "originMap");
		}
		public ShipCollisionManager()
		{
		}

		// Tunable parameters
		private const float warningRange = ShipMapComp.PDWeaponRange / 2f;
		// Initial damage scale, value is expected to be lower than mass.
		private const float damageScale = 0.45f;
		private const float pirateFactionRammingCahnce = 0.35f;
		private const float maxDamage = 7000;
		// This implements the idea "Impact is impact". If ramming ship is just barely faster than retreating ship,
		// damage will hit this lower cap and won't reduce to alsmost nothing.
		private const float minCollisionSpeed = 0.3f;
		// Ramming ships with a frequence of some power tool is not considered reasonable,
		// so there is a cooldown between collisions.
		private const int collisionTickInterval = 720;
		private static readonly SimpleCurve radiusFromDamage = new SimpleCurve
		{
			new CurvePoint(300f, 1.4f ),
			new CurvePoint(500f, 1.8f), // this is more than sqrt(2), so 3x3 tiles affected
			new CurvePoint(900f, 5f),   // 5 radius for 900 dmg is not much, as that damage doesn't even destroy normal 1000HP hull at the impact point
			new CurvePoint(2500f, 8f),  // subject to manual tuning
			new CurvePoint(7000f, 12f),
		};
		// Damage decrease multiplir from distance from distance, 0 distance means impact point, 1 is at max distance
		private static readonly SimpleCurve damageFromDistance = new SimpleCurve
		{
			new CurvePoint(0f, 1f),          // full damage at center
			new CurvePoint(0.25f, 0.56f),    // rapid falloff if close to the center, (1-0.25)^2 = 0.5625
			new CurvePoint(0.5f, 0.25f),     // fallof gradually sloweres further from center, still according to square funtion at this point
			new CurvePoint(0.75f, 0.07f),    // manually tuned to be a little hinger than square funtion in above entries.
			new CurvePoint(1f, 0.04f),     			
		};
		public void RangeUpdated()
		{
			Assert.IsNotNull(OriginMapComp);
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
			if (previusRange > warningRange && OriginMapComp.Range < warningRange)
			{
				showedWarningMessage = true;
				Messages.Message("SoS.Collision.WarningMessage".Translate(), null, MessageTypeDefOf.CautionInput);
			}
		}
		private void checkAndHandleCollision()
		{
			// Sadly, Odyssey already has very differently implemented ship impact/damage feature. So this got to be locked behind DLC, just in case.
			// This comment must not be interpreted as a confirmation of similarity between Odyssey ship damahe on landing and this feature.
			// In fact, it states the contrary, features are absolutely different: Odyssey feature is ship-ground impact, SOS 2 feature is ship-ship impact,
			// determined damage with parts destruction vs random, mostly cosmetic area damage. Only very general and broad idea is simialr.
			if (!ModsConfig.OdysseyActive)
			{
				return;
			}
			if (Find.TickManager.TicksGame - lastCollisionTick < collisionTickInterval)
			{
				return;
			}
			lastCollisionTick = Find.TickManager.TicksGame;
			const float collisionEpsion = 10;
			if (Mathf.Abs(OriginMapComp.Range) < collisionEpsion)
			{
				doCollision();
			}
		}
		private void doCollision()
		{
			// For now, collsion isn't something that persists or has state
			// but rather insta-damage and insta-bounce off and that's it.
			Messages.Message("SoS.Collision.Message".Translate(), null, MessageTypeDefOf.CautionInput);
			OriginMapComp.Range = warningRange;

			ShipMapComp targetMapComp = OriginMapComp.TargetMapComp;
			SpaceShipCache playerShip = getCollidingShipOnMap(OriginMapComp);
			SpaceShipCache enemyShip = getCollidingShipOnMap(targetMapComp);

			// Extremeny unlikely to happen
			if (playerShip == null || enemyShip == null)
			{
				Log.ErrorOnce($"SoS 2: Coudn't find ships for collision. Player ship: {playerShip}, enemy ship: {enemyShip}", 94417531);
				return;
			}

			float playerTWR = OriginMapComp.SlowestThrustToWeightCached() * TWRMath.TWRLargeMultiplier;
			float enemyTWR = targetMapComp.SlowestThrustToWeightCached() * TWRMath.TWRLargeMultiplier;

			// As 1 means advance and -1 means retreat in heading, this is the relative speed
			float collisionSpeed = Mathf.Abs(playerTWR * OriginMapComp.Heading + enemyTWR * targetMapComp.Heading);

			collisionSpeed = Mathf.Max(collisionSpeed, minCollisionSpeed);

			inflictDamage(playerShip, OriginMapComp, enemyShip, targetMapComp, collisionSpeed);
		}
		private void inflictDamage(SpaceShipCache rammingShip, ShipMapComp rammingMapComp, SpaceShipCache targetShip, ShipMapComp targetMapComp, float collisionSpeed)
		{
			// This denies physics, but in order to make ramming favorable for the player, reflected damage done by larger and slower enemy
			// ship needs to be reduced
			float baseDamage = Mathf.Sqrt(rammingShip.MassActual) * Mathf.Sqrt(targetShip.MassActual) * damageScale;

			// Speed factor is good for the player, so allow faster than linear grow here
			float speedFactor = collisionSpeed * Mathf.Sqrt(collisionSpeed);

			float damage = Mathf.Clamp(baseDamage * speedFactor, 0, maxDamage);

			doImpactDamageToShip(rammingShip, rammingMapComp, damage);
			doImpactDamageToShip(targetShip, targetMapComp, damage);

		}
		private void doImpactDamageToShip(SpaceShipCache targetShip, ShipMapComp targetMapComp, float damage)
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

			IntVec3 impactPoint;

			// Hullfoam is intended to be soft and not intended to be exploitable in repeating collisions, so ignore it
			// for impacct point detection.
			HashSet<IntVec3> durableArea = new HashSet<IntVec3>();
			// Buildings with current HP below this threshold are ignored for the purpose of impact point clculation (too soft)
			// This will exclude standalone hull plating and hullfoam plating/wall, but 1x1 corners will be included.
			const int durableThreshold = 400;
			foreach (IntVec3 tile in targetShip.Area)
			{
				if(targetMapComp.map.listerBuildings.allBuildingsColonist.Any(b => b.HitPoints >= durableThreshold))
				{
					durableArea.Add(tile);
				}
				else if (targetMapComp.map.listerBuildings.allBuildingsNonColonist.Any(b => b.HitPoints >= durableThreshold))
				{
					durableArea.Add(tile);
				}
			}

			if (impactRot == Rot4.North)
			{
				impactPoint = durableArea.MaxBy(cell => cell.z);
			}
			else if (impactRot == Rot4.South)
			{
				impactPoint = durableArea.MinBy(cell => cell.z);
			}
			else if (impactRot == Rot4.East)
			{
				impactPoint = durableArea.MaxBy(cell => cell.x);
			}
			else // if (impactRot == Rot4.West)
			{
				impactPoint = durableArea.MinBy(cell => cell.x);
			}
			doImpactExplosion(targetMapComp, impactPoint, damage);
		}
		private void doImpactExplosion(ShipMapComp mapComp, IntVec3 impactPoint, float damage)
		{
			float radius = radiusFromDamage.Evaluate(damage);
			CellRect explosionArea = CellRect.CenteredOn(impactPoint, Mathf.FloorToInt(radius));
			explosionArea.ClipInsideMap(mapComp.map);
			int radiusSquared = (int)(radius * radius);

			// Simple damage model for now
			Dictionary<Thing, float> thingsDamage = new Dictionary<Thing, float>();
			List<Thing> halfDamageList = new List<Thing>();
			List<Thing> fullDamageList = new List<Thing>();
			foreach (IntVec3 cell in explosionArea.Where(x => x.DistanceToSquared(impactPoint) <= radiusSquared))
			{
				float distance = cell.DistanceTo(impactPoint);
				float currentDamage = damageFromDistance.Evaluate(distance/radius) * damage;
				foreach (Thing t in cell.GetThingList(mapComp.map))
				{
					if (!thingsDamage.ContainsKey(t))
					{
						thingsDamage.Add(t, currentDamage);
					}
					else
					{
						// Using closest tile rather than center for damage calculation. So that direct impact into engine = max damage,
						// not damage reduced based on engine size.
						thingsDamage[t] = Mathf.Max(thingsDamage[t], currentDamage); 
					}
				}
			}
			int destroyedBuildings = 0;
			// Have to cache this as actually apllying damage could end battle and enemy map could become graveyard 
			string mapName = mapComp.map.GetNameForLogs();
			applyDamage(thingsDamage, out destroyedBuildings);
			Log.Message($"Destroyed buildings on {mapName}: {destroyedBuildings}");			
		}
		private void applyDamage(Dictionary<Thing, float> thingsDamage, out int destroyedBuildings)
		{
			destroyedBuildings = 0;
			foreach (Thing t in thingsDamage.Keys)
			{
				if (!t.Destroyed)
				{
					t.TakeDamage(new DamageInfo(DamageDefOf.Crush, thingsDamage[t]));
					if (t is Building && t.Destroyed)
					{
						++destroyedBuildings;
					}
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
		public static bool PirateFactionWantsRamming(Faction faction)
		{
			return IsPirateFactionForRamming(faction) && Rand.Chance(pirateFactionRammingCahnce);
		}
		private static bool IsPirateFactionForRamming(Faction faction)
		{
			if (faction == Faction.OfPirates)
			{
				return true;
			}
			// Hav to dig up pirate-specific details here due to def inheritance not known at runtime
			if (!faction.def.backstoryFilters.NullOrEmpty())
			{
				foreach(BackstoryCategoryFilter filter in faction.def.backstoryFilters)
				{
					if (filter.categories?.Contains("Pirate") ?? false)
					{
						return true;
					}
				}
			}
			return false;
		}
	}
}

