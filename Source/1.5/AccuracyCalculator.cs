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
			dodgeAngle = DodgeChanceMultiplier.Evaluate(ThisMapComp.MapEnginePower);
			// There can be orphan projectiles on the way after battle ends
			if (SourceMapComp.IsValid)
			{
				dodgeAngle *= DodgePenaltyMultiplier.Evaluate(SourceMapComp.MapEnginePower);
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
	}
}

