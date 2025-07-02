using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace SaveOurShip2
{
	// Per-map ability of a ship/fleet to dodge incoming projectiles. Intended to be actuvate-able from ship bridge for the player ship.
	// Maybe enemies could use it later too
	public class MapDodgeAbility : IExposable
	{
		private Rot4 dodgeDirection = Rot4.Invalid;
		public Rot4 DodgeDirection
		{
			get
			{
				return dodgeDirection;
			}
		}
		// Negative default value to be off cooldown at any real game time, >=0 ticks
		const int neverActivatedTick = -60000;
		private int activationTick = neverActivatedTick;
		// need to cache evasion-affecting params not recalulate every tick
		private float evasionThrust;
		private int evasionPilotInt;
		public Map map;
		private ShipMapComp MapComp
		{
			get
			{
				return map.GetComponent<ShipMapComp>();
			}
		}

		// Full cooldown from prevuious activation to next activation
		// 43 seconds is less than spinal cooldown between bursts, so very liberal - allows dodging every spinal salvo
		const int cooldownCycle = 60 * 43;
		// 10 seconds active. Value tuned to be less demanding for players, no need to "hit" enemy salvo timing very precisely
		const int activeTime = 60 * 9;
		public MapDodgeAbility()
		{
		}

		public bool IsActive()
		{
			return dodgeDirection != Rot4.Invalid;
		}

		// Throw flex only part of active time,, as they will stay for a while
		public bool NeedThrowFlecks()
		{
			return IsActive() && (Find.TickManager.TicksGame - activationTick < activeTime / 2);
		}

		public AcceptanceReport CanActivate()
		{
			if (MapComp.SlowestThrustToWeight() <= Mathf.Epsilon)
			{
				return new AcceptanceReport(TranslatorFormattedStringExtensions.Translate("SoS.DodgeNoEnginePower"));
			}
			foreach (SpaceShipCache ship in MapComp.ShipsOnMap.Values)
			{
				if(!ship.HasRCS())
				{
					return new AcceptanceReport(TranslatorFormattedStringExtensions.Translate("SoS.DodgeNoRCSOnShip", ship.Name));
				}
			}
			int timeTillcooldown = activationTick + cooldownCycle - Find.TickManager.TicksGame;
			if (timeTillcooldown > 0)
			{
				return new AcceptanceReport(TranslatorFormattedStringExtensions.Translate("SoS.DodgeOnCooldown", Mathf.Ceil(timeTillcooldown/60)));
			}
			return AcceptanceReport.WasAccepted;
		}

		// Dodge left, relative to ship
		public void ActivateLeft()
		{
			Rot4 rotation = new Rot4(MapComp.EngineRot);
			rotation.Rotate(RotationDirection.Counterclockwise);
			Activate(rotation);
		}

		// Dodge right, relative to ship
		public void ActivateRight()
		{
			Rot4 rotation = new Rot4(MapComp.EngineRot);
			rotation.Rotate(RotationDirection.Clockwise);
			Activate(rotation);
		}

		private void Activate(Rot4 rotation)
		{
			activationTick = Find.TickManager.TicksGame;
			//evasionThrust = MapComp.SlowestThrustToWeight();
			float maxTWR = float.MaxValue;
			evasionPilotInt = 0;
			foreach (SpaceShipCache ship in MapComp.ShipsOnMap.Values)
			{
				float TWR = ship.ThrustRatio;
				if (TWR < maxTWR)
				{
					maxTWR = TWR;
				}
				int pilotInt = 0;
				IEnumerable<Building_ShipBridge> mannedBridges = ship.Bridges.Where(b => (b.mannableComp?.MannedNow ?? false));
				if (mannedBridges.Any())
				{
					pilotInt = mannedBridges.Max(b => (b.mannableComp.ManningPawn.skills?.GetSkill(SkillDefOf.Shooting)?.Level ?? 0));
				}
				const int coreIntLevel = 10;
				if( ship.AICores.Any())
				{
					pilotInt = Math.Max(pilotInt, coreIntLevel);
				}
				evasionPilotInt = Math.Max(evasionPilotInt, pilotInt);
			}
			if (maxTWR == float.MaxValue)
			{
				// Error case / emnpty map can't dodge
				maxTWR = 0;
			}
			evasionThrust = maxTWR;
			dodgeDirection = rotation;
		}

		public void Deactivate()
		{
			dodgeDirection = Rot4.Invalid;
			evasionThrust = 0f;
			// TODO 1.6: Remove flecks here
		}

		public void ExposeData()
		{
			Scribe_References.Look<Map>(ref map, "map");
			Scribe_Values.Look(ref dodgeDirection, "dodgeDirection", Rot4.Invalid);
			Scribe_Values.Look(ref activationTick, "activationTick", neverActivatedTick);
			Scribe_Values.Look(ref evasionThrust, "evasionThrust", 0f);
			Scribe_Values.Look(ref evasionPilotInt, "evasionPilotInt", 0);
		}

		static SimpleCurve evasionSpeedMultiplierCurve = new SimpleCurve()
		{
			new CurvePoint(0f, 0f),
			new CurvePoint(1f, 0.15f),
			new CurvePoint(2f, 0.5f),
			new CurvePoint(3f, 1f),
			new CurvePoint(4f, 1.2f),
			new CurvePoint(5f, 1.5f),
		};
		static SimpleCurve pilotMultiplierCurve = new SimpleCurve()
		{
			new CurvePoint(0f, 0.7f),
			new CurvePoint(20f, 1.3f),
		};
		public void CompTick()
		{
			if (IsActive())
			{
				if (Find.TickManager.TicksGame > activationTick + activeTime)
				{
					Deactivate();
					return;
				}
				// Ship with baseline dodger TWR = 3 has base evasion of 10 tiles per second. 0.8-1.1 seconds is ETA for projectiles shot at the small ship at the center of the map
				const float baseEvasionPerSecond = 22f;
				float evasionSpeedMultiplier = evasionSpeedMultiplierCurve.Evaluate(evasionThrust);
				float evasionPilotMultiplier = pilotMultiplierCurve.Evaluate(evasionPilotInt);
				float evasionOffset = baseEvasionPerSecond / 60f * evasionSpeedMultiplier * evasionPilotMultiplier;
				if (Find.TickManager.TicksGame % 15 == 0)
				{
					Log.Warning("Evasion offset:" + evasionOffset.ToString("F3"));
				}
				IntVec3 offsetVectorInt = IntVec3.North;
				// Projectiles move in the direction opposite to suppsed ship evasiondirection
				Vector3 offsetVector = offsetVectorInt.RotatedBy(dodgeDirection.Opposite).ToVector3() * evasionOffset;

				// Can't dodge lasers
				foreach (Projectile proj in MapComp.incomingProjectiles.Where(p => !(p is Projectile_ExplosiveShipLaser)))
				{
					// very small change in coordinates every tick. No redraw or so here, will be taken into account for next projectile tick
					proj.destination += offsetVector;
					proj.origin += offsetVector;
					proj.intendedTarget = new LocalTargetInfo(proj.destination.ToIntVec3());
				}
			}
		}
	}
}

