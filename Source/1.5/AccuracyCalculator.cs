using System;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace SaveOurShip2
{
	public class AccuracyCalculator : IExposable
	{
		// Math stuff
		// Weapon spread increased based on target map TWR
		// This goes to XML likely
		private const float multiplierForDodge = 3f;
		// Maybe better curve - this is not so proven
		private static readonly SimpleCurve DodgeChanceMultiplier = new SimpleCurve
		{
			new CurvePoint(7f, 7f * multiplierForDodge),
			new CurvePoint(5f, 5f * multiplierForDodge),
			new CurvePoint(3f, 4f * multiplierForDodge),
			new CurvePoint(2f, 1.8f * multiplierForDodge),
			new CurvePoint(1f, 1.5f * multiplierForDodge),
			new CurvePoint(0f, 1f * multiplierForDodge)
		};

		private static readonly SimpleCurve DodgeMultiplierFromWeaponRange = new SimpleCurve
		{
			new CurvePoint(0f, 0.10f),
			new CurvePoint(50f, 0.15f),   // Small laser/cannon
			new CurvePoint(100f, 0.28f),  // Large/spinal laser/cannon, small plasma
			new CurvePoint(150f, 0.72f),  // Large/spinal plasma, small rail
			new CurvePoint(250f, 1f),    // Large/spinal rail
			new CurvePoint(300f, 1.2f),
		};
		private static readonly SimpleCurve DodgeMultiplierFromCurrentRange = new SimpleCurve
		{
			new CurvePoint(0f, 0.7f),
			new CurvePoint(50f, 0.75f),   // Small laser/cannon
			new CurvePoint(100f, 0.8f),  // Large/spinal laser/cannon, small plasma
			new CurvePoint(150f, 0.9f),  // Large/spinal plasma, small rail
			new CurvePoint(250f, 1f),    // Large/spinal rail
			new CurvePoint(300f, 1f),
		};
		// Counter to presious, spread decreased baseed on attacking map TWR, so that fast ship dodges large one,
		// but a battle of 2 small sips is not 95% misses.
		private const float multiplierForDodgePenalty = 3.2f;
		private static readonly SimpleCurve DodgePenaltyMultiplier = new SimpleCurve
		{
			new CurvePoint(7f, 4f * multiplierForDodgePenalty),
			new CurvePoint(5f, 2.5f * multiplierForDodgePenalty),
			new CurvePoint(3f, 1.8f * multiplierForDodgePenalty),
			new CurvePoint(2f, 1.3f * multiplierForDodgePenalty),
			new CurvePoint(1f, 1.1f * multiplierForDodgePenalty),
			new CurvePoint(0f, 1f * multiplierForDodgePenalty)
		};
		public const float LaserOptimalRange = 50;
		public const float PlasmaOptimalRange = 100;
		public const float RailOptimalRange = 150;

		// temporaty - just saved between different ExposeData() calls. 
		private Map thisMap;
		private Map sourceMap;

		// Statistics
		private int hitCount = 0;
		private int projectileCount = 0;

		private ShipMapComp ThisMapComp
		{
			get
			{
				return thisMap.GetComponent<ShipMapComp>();
			}
		}
		private ShipMapComp SourceMapComp
		{
			get
			{
				return sourceMap.GetComponent<ShipMapComp>();
			}
		}
		public AccuracyCalculator()
		{
		}
		public AccuracyCalculator(ShipMapComp thisMapComp, ShipMapComp sourceMapComp)			
		{
			thisMap = thisMapComp.map;
			sourceMap = sourceMapComp.map;
		}
		public float DodgeCance(ShipCombatProjectile proj)
		{
			return GetDodgeCanceImpl(proj.turret.heatComp.Props.optRange);
		}

		public float ShortRangedWeaponDodgeChance
		{
			get
			{
				// Lasers/cannons has has 50 range
				return GetDodgeCanceImpl(LaserOptimalRange);
			}
		}
		public float AverageDodgeChance
		{
			get
			{
				// Plasma has 100 range
				return GetDodgeCanceImpl(PlasmaOptimalRange);
			}
		}
		public float LongRangedWeaponDodgeChance
		{
			get
			{
				// Lasers/cannons has has 50 range
				return GetDodgeCanceImpl(RailOptimalRange);
			}
		}
		// Dodge chance multiplier based on shooter skill
		private static readonly SimpleCurve DodgeChanceMultiplierFromShooting = new SimpleCurve
		{
			new CurvePoint(0f, 1.6f),
			new CurvePoint(20f, 0.4f)
		};
		private static readonly SimpleCurve DodgeChanceMultiplierFromPiloting = new SimpleCurve
		{
			new CurvePoint(0f, 1.6f),
			new CurvePoint(20f, 0.4f)
		};
		private float GetDodgeCanceImpl(float weaponRange)
		{
			float baseChance = 0.3f;
			// Moodify base chance for weapon range
			if (weaponRange > (PlasmaOptimalRange + RailOptimalRange) / 2f)
				baseChance *= 1.5f;
			else if (weaponRange < (LaserOptimalRange + PlasmaOptimalRange) / 2f)
				baseChance *= 0.5f;
			// lower chances for lower TWR, higher chances for higher TWR
			// Complete/critical miss system is mainly for fighters, so firhter TWR is baseline
			const float baselineTWR = 3.5f;
			// attacker tactician shooting skill
			float dodgeMultiplierFromShooting = DodgeChanceMultiplierFromShooting.Evaluate(SourceMapAccuracyBoost);
			// pilot skill
			float dodgeMultiplierFromPiloting = DodgeChanceMultiplierFromPiloting.Evaluate(ThisMapEvasionBoost);
			float finalChance = baseChance * dodgeMultiplierFromShooting * dodgeMultiplierFromPiloting * ThisMapComp.SlowestThrustRatio() / baselineTWR;
			return finalChance;
		}

		public bool PerformDodgeCheck(ShipCombatProjectile proj)
		{
			return Rand.Chance(DodgeCance(proj));
		}
		// Initial calc function, that can catch up things from old code work
		public float GetMissAngle(ShipCombatProjectile proj)
		{
			// New system - not picking initial ranfom value. Rather calculating all miss angles, adding up and get random result wiithin given angle at the last step
			float missAngle = proj.missRadius; //base miss from xml
			float dodgeAngle = 0f;
			float rng = proj.range - proj.turret.heatComp.Props.optRange;
			if (rng > 0)
			{
				// miss angle due to shooting from above optimal range
				float missAngleFromOverRange = (float)Math.Sqrt(rng); //-20 - 20
													//Log.Message("angle: " + angle + ", missangle: " + missAngle);
													// For railguns, even less accuracy, but more accuracy for lasers and cannons 
				if (proj.turret.heatComp.Props.optRange - Mathf.Epsilon <= LaserOptimalRange)
				{
					missAngleFromOverRange *= 0.5f;
				}
				else if (proj.turret.heatComp.Props.optRange - Mathf.Epsilon <= PlasmaOptimalRange)
				{
					missAngleFromOverRange *= 0.75f;
				}
				// Earlier this was decently large, but once you get into optimal range - super accurate.
				// That changess to somewhat lager miss angle for railguns in general that can't be countered by entering optimal range.
				// In order to not make above optimal range shooting useless, this is clamped.
				// Unclamped typical max is 10 or even higher when during shooting target movedfurther than maximum range
				missAngleFromOverRange = Mathf.Clamp(missAngleFromOverRange, 0, 7f);
				missAngle *= missAngleFromOverRange;
			}
			//shooter adj 0-50%
			missAngle *= (100 - proj.accBoost * 2.5f) / 100;
			// Use reasonable clamp when working with MapEnginePower
			dodgeAngle = Mathf.Clamp(DodgeChanceMultiplier.Evaluate(ThisMapComp.SlowestThrustRatio()), 0f, 40f);
			// There can be orphan projectiles on the way after battle ends
			if (SourceMapComp.IsValid)
			{
				dodgeAngle *= Mathf.Clamp(DodgePenaltyMultiplier.Evaluate(SourceMapComp.SlowestThrustRatio()), 0.05f, 20f);
			}
			// Dodge angle reduced for short-ranged weapons
			dodgeAngle *= DodgeMultiplierFromWeaponRange.Evaluate(proj.turret.heatComp.Props.maxRange);
			// And for current range too
			dodgeAngle *= DodgeMultiplierFromCurrentRange.Evaluate(proj.range);
			//shooter adj 0-70% for miss angle
			dodgeAngle *= (100 - proj.accBoost * 2f) / 100;


			if (ModSettings_SoS.debugMode)
			{
				Log.Warning("+CalculatedAngles: dodge: " + dodgeAngle.ToString("F2") + ", miss: " + missAngle.ToString("F2"));
			}
			float totalSpread = dodgeAngle + missAngle;
			const float maxTotalSpread = 35f;
			if(totalSpread > maxTotalSpread)
			{
				Log.Warning("TotalSpread too high:" + totalSpread.ToString("F2"));
			}
			totalSpread = Mathf.Clamp(totalSpread, 0f, maxTotalSpread);
			return Rand.Range(-totalSpread, totalSpread);
		}

		public void ExposeData()
		{
			Scribe_References.Look<Map>(ref thisMap, "ThisMap");
			Scribe_References.Look<Map>(ref sourceMap, "SourceMap");
		}

		public bool IsValid
		{
			get
			{
				return (SourceMapComp?.IsValid ?? false) && (ThisMapComp?.IsValid ?? false);
			}
		}

		// Map-wide accuracy boost by intellectual skill
		public int SourceMapAccuracyBoost
		{
			get
			{
				int result = 0;
				foreach (SpaceShipCache ship in SourceMapComp.ShipsOnMap.Values)
				{
					foreach (Building_ShipBridge bridge in ship.Bridges)
					{
						result = Mathf.Max(result, bridge.heatComp.myNet.AccuracyBoost);
					}
				}
				return result;
			}
		}

		public int ThisMapEvasionBoost
		{
			get
			{
				int result = 0;
				foreach (SpaceShipCache ship in SourceMapComp.ShipsOnMap.Values)
				{
					foreach (Building_ShipBridge bridge in ship.Bridges)
					{
						if (bridge.mannableComp.MannedNow)
						{
							int skill = bridge.mannableComp.ManningPawn.skills?.GetSkill(SkillDefOf.Shooting).Level ?? 0;
							result = Mathf.Max(result, skill);
						}
					}
					// AI core counst as shooting 10
					if (ship.AICores.Any())
					{
						result = Mathf.Max(result, 10);
					}
				}
				return result;
			}
		}

		// Statistics
		public void RegisterDespawn(Projectile proj)
		{
			projectileCount++;
			const int loggingInterval = 20;
			if (projectileCount % loggingInterval == 0)
			{
				Log.Warning("Hit rate: " + ((float)hitCount/projectileCount).ToString("F2"));
			}
		}
		public void RegisterExplosion(Projectile proj)
		{
			hitCount++;
		}
	}
}

