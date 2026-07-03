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
		// Submods are known to allow player to reach far better TWRs for decently strong/large combat ships.
		// With dodge chance necghanics, it means that chance can go over roof and enemies 
		private float dodgeChanceSubmodScale = 1;

		// Bonus from Odyssey implant
		public const int pilotAssistantIntellectualBonus = 2;
		public float DodgeChanceSubmodScale
		{
			get
			{
				return dodgeChanceSubmodScale;
			}
			set
			{
				// Multipler is only intended for reducing dodge chance and also eliminating mechanoic isn't allowed.
				dodgeChanceSubmodScale = Mathf.Clamp(value, 0.1f, 1f);
			}
		}
		// Dodge chance based on target map TWR. Previusly it was TWR/3.5, linear
		private static readonly SimpleCurve DodgeChanceMultiplier = new SimpleCurve
		{
			new CurvePoint(21f, 1.7f ), // Archo thrusers or Spinal Engines submod, very high values get diminishing return
			new CurvePoint(7f, 1.5f),
			new CurvePoint(5f, 1.3f), // Was 1.43 at linear - above 3.5 TW is is diminishing return in dodge chance now
			new CurvePoint(3.5f, 1f),
			new CurvePoint(3f, 0.88f), // Was 0.86 at linear
			new CurvePoint(2f, 0.55f), // Was 0.57 at linear
			new CurvePoint(1f, 0.26f), // Was 0.29 at linear - the idea is dodge cance falls faster than linear as TWR goes vely low
			new CurvePoint(0.5f, 0.11f),
			new CurvePoint(0f, 0f)
		};
		// This migh to XML for adjustablity
		private const float magnitudeForDodge = 6.5f;

		private static readonly SimpleCurve DodgeAngleMultiplier = new SimpleCurve
		{
			new CurvePoint(21f, 6.0f * magnitudeForDodge), //  Archo thrusers or Spinal Engines submod, very high values get diminishing return
			new CurvePoint(7f, 5.1f * magnitudeForDodge),
			new CurvePoint(5f, 4.8f * magnitudeForDodge), // Diminishing return
			new CurvePoint(3f, 4f * magnitudeForDodge), // Dodge still grows fast with TWR
			new CurvePoint(2f, 1.8f * magnitudeForDodge), // It is hard to get 3 TWRS, so those gets great dodge. But 2 is achievable for strong medium ships, non-fighters, so much lower dodge
			new CurvePoint(1f, 1.5f * magnitudeForDodge),
			new CurvePoint(0.5f, 1.2f * magnitudeForDodge),
			new CurvePoint(0f, 1f * magnitudeForDodge)
		};
		// Counter to presious, spread decreased baseed on attacking map TWR, so that fast ship dodges large one,
		// but a battle of 2 small sips is not 95% misses.
		private const float magnitudeForDodgePenalty = 1f; // Magnitude might be adjusted relative to magnitudeForDodge
		private static readonly SimpleCurve DodgePenaltyMultiplier = new SimpleCurve
		{
			new CurvePoint(7f, 1/(4f * magnitudeForDodgePenalty)),
			new CurvePoint(5f, 1/(2.8f * magnitudeForDodgePenalty)),
			new CurvePoint(3f, 1/(2f * magnitudeForDodgePenalty)),
			new CurvePoint(2f, 1/(1.4f * magnitudeForDodgePenalty)),
			new CurvePoint(1f, 1/(1.15f * magnitudeForDodgePenalty)),
			new CurvePoint(0.5f, 1/(1.1f * magnitudeForDodgePenalty)),
			new CurvePoint(0f, 1/(1f * magnitudeForDodgePenalty))
		};
		private static readonly SimpleCurve DodgeMultiplierFromWeaponRange = new SimpleCurve
		{
			new CurvePoint(0f, 0.08f),
			new CurvePoint(50f, 0.1f),   // Small laser/cannon. These have small explosion radius and need heavy spread reduction
			new CurvePoint(100f, 0.24f),  // Large/spinal laser/cannon, small plasma
			new CurvePoint(150f, 0.7f),  // Large/spinal plasma, small rail
			new CurvePoint(250f, 1f),    // Large/spinal rail
			new CurvePoint(300f, 1.2f),  // No vanilla weapons with calculated accuracy at that range
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
			DodgeChanceSubmodScale = DodgeChanceSubmodScaleDef.GetEffectiveMultiplier();
		}
		public float DodgeCance(ShipCombatProjectile proj)
		{
			return GetDodgeCanceImpl(proj.turret.heatComp.Props.optRange);
		}

		public float ShortRangedWeaponDodgeChance
		{
			get
			{
				// Lasers/cannons have has otpimal 50 range
				return GetDodgeCanceImpl(LaserOptimalRange);
			}
		}
		public float AverageDodgeChance
		{
			get
			{
				// Plasma has 100 otpimal range
				return GetDodgeCanceImpl(PlasmaOptimalRange);
			}
		}
		public float LongRangedWeaponDodgeChance
		{
			get
			{
				// Rails have has 150 otpimal range
				return GetDodgeCanceImpl(RailOptimalRange);
			}
		}
		// Dodge chance multiplier based on shooter skill
		private static readonly SimpleCurve DodgeChanceMultiplierFromShooting = new SimpleCurve
		{
			new CurvePoint(0f, 1.5f),
			new CurvePoint(20f, 0.5f)
		};
		private static readonly SimpleCurve DodgeChanceMultiplierFromPiloting = new SimpleCurve
		{
			new CurvePoint(0f, 0.5f),
			new CurvePoint(20f, 1.5f),
			new CurvePoint(22f, 1.55f)    // Can go above 20 with skill bonuses, diminishing return in this case
		};
		private float GetDodgeCanceImpl(float weaponRange)
		{
			float baseChance = 0.3f;
			// Moodify base chance for weapon range
			if (weaponRange > (PlasmaOptimalRange + RailOptimalRange) / 2f)
				baseChance *= 1.5f;
			else if (weaponRange < (LaserOptimalRange + PlasmaOptimalRange) / 2f)
				baseChance *= 0.5f;
			// attacker tactician shooting skill
			float dodgeMultiplierFromShooting = DodgeChanceMultiplierFromShooting.Evaluate(SourceMapAccuracyBoost);
			dodgeMultiplierFromShooting = Mathf.LerpUnclamped(1, dodgeMultiplierFromShooting, (float)ModSettings_SoS.dodgeSkillImpact);
            // pilot skill
            float dodgeMultiplierFromPiloting = DodgeChanceMultiplierFromPiloting.Evaluate(ThisMapEvasionBoost);
            dodgeMultiplierFromPiloting = Mathf.LerpUnclamped(1, dodgeMultiplierFromPiloting, (float)ModSettings_SoS.dodgeSkillImpact);

            // Extra buildings
            float dodgeMultiplierFromBuildings = ThisMapEvasionScaleFromBuildings;
			// TWR
			float dodgeMultiplierFromTWR = DodgeChanceMultiplier.Evaluate(ThisMapComp.SlowestThrustRatio(out var blockingShip, out var slowestShip));
			float finalChance = baseChance * dodgeMultiplierFromShooting * dodgeMultiplierFromPiloting * dodgeMultiplierFromTWR *
				dodgeMultiplierFromBuildings * DodgeChanceSubmodScale;
			return Mathf.Clamp(finalChance, 0f, 0.9f);
		}

		private bool ShouldLogDataNow
		{
			// Check preventing excess log spam. 
			// Could be set to fisrst few projectiles or every X projectiles base on projectileCount
			get
			{
				return false;
			}
		}

		public bool PerformDodgeCheck(ShipCombatProjectile proj)
		{
			if(ShouldLogDataNow)
			{
				Log.Warning("Dodging, chance: " + DodgeCance(proj));
			}
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
			dodgeAngle = Mathf.Clamp(DodgeAngleMultiplier.Evaluate(ThisMapComp.SlowestThrustRatio(out var blockingShip, out var slowestShip)), 0f, 40f);
			if (ModSettings_SoS.debugMode)
			{
				Log.Warning("===Base DodgeAngle:" + dodgeAngle.ToString("F2"));
				Log.Warning("From TWR:" + ThisMapComp.SlowestThrustRatio(out var blockingShip2, out var slowestShip2));
				Log.Warning("For map:" + thisMap.Parent?.Label ?? "(no parent)");
			}
			// There can be orphan projectiles on the way after battle ends
			if (SourceMapComp.IsValid)
			{
				dodgeAngle *= Mathf.Clamp(DodgePenaltyMultiplier.Evaluate(SourceMapComp.SlowestThrustRatio(out var blockingShip3, out var slowestShip3)), 1f, 10f);
			}
			// Dodge angle reduced for short-ranged weapons
			dodgeAngle *= DodgeMultiplierFromWeaponRange.Evaluate(proj.turret.heatComp.Props.maxRange);
			// And for current range too
			dodgeAngle *= DodgeMultiplierFromCurrentRange.Evaluate(proj.range);
			//shooter adj 0-70% for miss angle
			float shooterMultiplierFoDodge = (100 - proj.accBoost * 2f) / 100;
			shooterMultiplierFoDodge = Mathf.LerpUnclamped(1, shooterMultiplierFoDodge, (float)ModSettings_SoS.dodgeSkillImpact);
            if (ModSettings_SoS.debugMode)
			{
				Log.Warning("shooterMultiplierFoDodge: " + shooterMultiplierFoDodge);
			}
			dodgeAngle *= shooterMultiplierFoDodge;

			if (ModSettings_SoS.debugMode)
			{
				Log.Warning("+CalculatedAngles: dodge: " + dodgeAngle.ToString("F2") + ", miss: " + missAngle.ToString("F2"));
			}
			float totalSpread = dodgeAngle + missAngle;
			const float maxTotalSpread = 60f;
			if(totalSpread > maxTotalSpread)
			{
				Log.Warning("TotalSpread too high:" + totalSpread.ToString("F2"));
			}
			totalSpread = Mathf.Clamp(totalSpread, 0f, maxTotalSpread);

			if (ShouldLogDataNow)
			{
				Log.Warning("TotalSpread: " + totalSpread.ToString("F3"));
			}
			return Rand.Range(-totalSpread, totalSpread);
		}

		public void ExposeData()
		{
			Scribe_References.Look<Map>(ref thisMap, "ThisMap");
			Scribe_References.Look<Map>(ref sourceMap, "SourceMap");
			Scribe_Values.Look<int>(ref projectileCount, "projectileCount");
			Scribe_Values.Look<int>(ref hitCount, "hitCount");
			Scribe_Values.Look<float>(ref dodgeChanceSubmodScale, "dodgeChanceSubmodScale");
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
						if (bridge.heatComp.myNet != null)
						{
							result = Mathf.Max(result, bridge.heatComp.myNet.AccuracyBoost);
						}
					}
				}
				return result;
			}
		}

		private float thisMapEvasionScaleFromBuildings = 0f;
		private int thisMapEvasionCalcTick = -GenDate.TicksPerQuadrum;

		public float ThisMapEvasionScaleFromBuildings
        {
            get
            {
				if (Find.TickManager.TicksGame < thisMapEvasionCalcTick + 60)
				{
					return thisMapEvasionScaleFromBuildings;
				}
				int gravEngineCount = 0;
				// Maximum number of buildings of certain type on map that will give bonus. 
				// Having more may be allowed, but bonus won't increase.
				const int maxGravEngines = 1;
				int subpersonaCount = 0;
				const int maxSubpersonas = 2;

				foreach (SpaceShipCache ship in ThisMapComp.ShipsOnMap.Values)
				{
					foreach (Building building in ship.Buildings)
					{
						if (ResourceBank.IsGravEngine(building.def))
						{
							gravEngineCount++;
						}
						if (building.def == ResourceBank.ThingDefOf.PilotSubpersonaCore)
						{
							subpersonaCount++;
						}
						if (subpersonaCount >= maxSubpersonas && gravEngineCount >= maxGravEngines)
						{
							// When found maximum allowed number of bonus buildings
							break;
						}
					}
				}
				gravEngineCount = Mathf.Min(gravEngineCount, maxGravEngines);
				subpersonaCount = Mathf.Min(subpersonaCount, maxSubpersonas);
				const float gravEngineMonus = 0.055f;
				const float subpersonaBonus = 0.01f;

				float result = 1f + gravEngineCount * gravEngineMonus + subpersonaCount * subpersonaBonus;
				thisMapEvasionScaleFromBuildings = result;
				thisMapEvasionCalcTick = Find.TickManager.TicksGame;
				return result;
			}
        }

		public int ThisMapEvasionBoost
		{
			get
			{
				int result = 0;
				foreach (SpaceShipCache ship in ThisMapComp.ShipsOnMap.Values)
				{
					foreach (Building_ShipBridge bridge in ship.Bridges)
					{
						if (bridge.mannableComp?.MannedNow ?? false)
						{
							Pawn pilot = bridge.mannableComp.ManningPawn;
							int skill = pilot.skills?.GetSkill(SkillDefOf.Intellectual)?.Level ?? 0;
							// Odyssey pilot assistant
							if (pilot.health.hediffSet.hediffs.Any((Hediff h) => h.def.defName == "PilotAssistant"))
							{
								skill += pilotAssistantIntellectualBonus;
							}
							result = Mathf.Max(result, skill);
						}
					}
					// AI core counst as intellectual 10
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
			const int loggingInterval = 40;
			if (projectileCount % loggingInterval == 0 && ModSettings_SoS.debugMode)
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

