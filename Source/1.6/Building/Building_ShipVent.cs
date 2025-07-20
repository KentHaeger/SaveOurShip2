﻿using UnityEngine;
using Verse;
using System.Collections.Generic;
using RimWorld;
using Verse.Sound;
using System;
using System.Linq;

namespace SaveOurShip2
{
	public class Building_ShipVent : Building_TempControl
	{
		private const float heatpipeTemp = 60; //higher = more diff to push heat to net
		public bool heatWithPower=true;
		private CompShipHeat heatComp;
		public IntVec3 ventTo;

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			heatComp = this.TryGetComp<CompShipHeat>();
			ventTo = Position + IntVec3.North.RotatedBy(Rotation);
		}

		public override void TickRare()
		{
			if (this.compPowerTrader.PowerOn)
			{
				bool flag = false; //operating at high power
				float energyLimit;
				float tempChange;
				float conductance;
				Room room = ventTo.GetRoom(Map);
				if (room != null && room.ProperRoom && room.OpenRoofCount <= 0 && !room.UsesOutdoorTemperature)
				{
					float roomTemp = ventTo.GetTemperature(Map);
					if (roomTemp < this.compTempControl.targetTemperature - 3)
					{
						//heat room up to target temp if enough heat available, else use power (same as vanilla heater)
						if (roomTemp < 20f)
							conductance = 1f;
						else if (roomTemp > 120f)
							conductance = 0f;
						else
							conductance = Mathf.InverseLerp(120f, 20f, roomTemp);

						energyLimit = this.compTempControl.Props.energyPerSecond * conductance * -1.367f; //-64*-1.3672 = 21*4.1667
						tempChange = GenTemperature.ControlTemperatureTempChange(ventTo, Map, energyLimit, this.compTempControl.targetTemperature);
						if (heatComp.RemHeatFromNetwork(energyLimit * 0.02f))
						{
							//Log.Message("Rem heat:" + energyLimit * 0.02f + " TC:" + - tempChange);
							room.Temperature += tempChange;
						}
						else if (heatWithPower)
						{
							flag = !Mathf.Approximately(tempChange, 0f);
							if (flag)
							{
								room.Temperature += tempChange;
							}
						}
					}
					else
					{
						//cool room: check if heatnet is there and not maxed, push heat to it (same as vanilla cooler)
						float num1 = heatpipeTemp - roomTemp;
						if (heatpipeTemp - 40.0 > num1)
							num1 = heatpipeTemp - 40f;
						conductance = 1f - num1 * 0.0077f;
						if (conductance < 0.0)
							conductance = 0.0f;
						energyLimit = this.compTempControl.Props.energyPerSecond * conductance * 4.167f;
						tempChange = GenTemperature.ControlTemperatureTempChange(ventTo, Map, energyLimit, this.compTempControl.targetTemperature);
						flag = !Mathf.Approximately(tempChange, 0.0f);
						if (flag && heatComp.AddHeatToNetwork(-energyLimit * 0.02f))
						{
							//Log.Message("Add heat:" + -energyLimit * 0.02f + " TC:" + tempChange);
							room.Temperature += tempChange;
						}
						else flag = false;
					}
				}
				CompProperties_Power props = this.compPowerTrader.Props;
				if (flag)
					this.compPowerTrader.PowerOutput = -props.PowerConsumption;
				else
					this.compPowerTrader.PowerOutput = -props.PowerConsumption * this.compTempControl.Props.lowPowerConsumptionFactor;
				this.compTempControl.operatingAtHighPower = flag;
			}
		}
		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo gizmo in base.GetGizmos())
			{
				yield return gizmo;
			}
			Command_Toggle toggleHeatWithPower = new Command_Toggle
			{
				toggleAction = delegate
				{
					heatWithPower = !heatWithPower;
				},
				defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.HeatWithPower"),
				defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.HeatWithPowerDesc"),
				isActive = () => heatWithPower
			};
			toggleHeatWithPower.icon = ContentFinder<Texture2D>.Get("Things/Building/Misc/TempControl/Heater");
			yield return toggleHeatWithPower;

			if (Map.IsSpace())
			{
				Room room = ventTo.GetRoom(Map);
				ShipMapComp comp = Map.GetComponent<ShipMapComp>();
				SpaceShipCache ship = comp.ShipsOnMap[comp.ShipIndexOnVec(Position)];
				int pointsOfDamage = 0;
				if (ship.LifeSupports.Count > 0)
				{
					// Minimun damage disallows free heat dumping
					pointsOfDamage = Math.Max(2, room.CellCount / ship.LifeSupports.Count / 2);
				}
				Command_Action expelSuperheatedAir = new Command_Action
				{
					action = delegate
					{
						foreach (IntVec3 cell in room.Cells)
							FleckMaker.ThrowDustPuff(cell, Map, 1);
						foreach (Fire fire in room.ContainedThings<Fire>())
							fire.Destroy();
						foreach (Pawn pawn in room.ContainedThings<Pawn>())
						{
							if (pawn.DecompressionResistance() < 1)
								WeatherEvent_VacuumDamage.DoPawnDecompressionDamage(pawn, 10);
						}
						room.Temperature = 0f;
						Log.Message("Dealing " + pointsOfDamage + " to each of " + ship.LifeSupports.Count + " life supports.");
						foreach (CompShipLifeSupport lifeSupport in ship.LifeSupports.ToList()) //Curse you, collection modification bugs!
							lifeSupport.parent.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, pointsOfDamage));
						DefDatabase<SoundDef>.GetNamed("Explosion_Smoke").PlayOneShot(this);
					},
					defaultLabel = "SoS.ExpelSuperheatedAir".Translate(),
					defaultDesc = "SoS.ExpelSuperheatedAirDesc".Translate("<color=\"red\">"+pointsOfDamage+"</color>"),
					icon = ContentFinder<Texture2D>.Get("UI/VacuumSuckSuckSuck")
				};
				if (ventTo.GetRoom(Map).Temperature < 100)
				{
					expelSuperheatedAir.disabled = true;
					expelSuperheatedAir.disabledReason = "SoS.AirNotSuperheated".Translate();
				}
				else
				{
					if (!ship.LifeSupports.Any())
					{
						expelSuperheatedAir.disabled = true;
						expelSuperheatedAir.disabledReason = "SoS.NoLifeSupport".Translate();
					}
				}
				yield return expelSuperheatedAir;
			}
		}
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref heatWithPower, "heatWithPower",true);
		}
	}
}
