using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using Vehicles;

namespace SaveOurShip2
{
	[StaticConstructorOnStartup]
	public class CompShipHeatSink : CompShipHeat
	{
		public static readonly float HeatPushMult = 10f; //bleed ratio modifier - inverse to Building_ShipVent AddHeatToNetwork
		public float heatStored; //used only when a HB is not on a net or during PostExposeData
		public float depletion; //ditto
		public bool inSpace;
		public CompPower powerComp;
		ShipMapComp mapComp;
		IntVec3 pos; //needed because no predestroy
		Map map; //needed because no predestroy

		// the backing field is updated in CompTick
		// used to keep CompInspectStringExtra faster
		public bool disabled;
		public bool Disabled
		{
			get
			{
				if (mapComp == null)
				{
					ShipMapComp parentMapComp = parent.Map?.GetComponent<ShipMapComp>();
					if (parentMapComp == null)
					{
						return false;
					}
					return parentMapComp?.Cloaks.Any(c => c.active) == true;
				}
				disabled = mapComp?.Cloaks.Any(c => c.active) == true;
				return disabled;
			}
		}
		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			if (this.parent is VehiclePawn)
				return;
			powerComp = parent.TryGetComp<CompPower>();
			inSpace = this.parent.Map.IsSpace();
			pos = this.parent.Position;
			map = this.parent.Map;
			mapComp = this.map.GetComponent<ShipMapComp>();
		}
		public override void PostDestroy(DestroyMode mode, Map previousMap)
		{
			if (myNet != null)
				PushHeat(0, Props.heatCapacity * myNet.RatioInNetworkRaw * HeatPushMult);
			base.PostDestroy(mode, previousMap);
		}
		public override void PostDeSpawn(Map map)
		{
			base.PostDeSpawn(map);
		}
		public override void PostExposeData()
		{
			base.PostExposeData();
			//save heat to sinks on save, value clamps
			if (myNet != null && Scribe.mode == LoadSaveMode.Saving)
			{
				heatStored = Props.heatCapacity * myNet.RatioInNetworkRaw;
				depletion = Props.heatCapacity * myNet.DepletionRatio;
			}
			Scribe_Values.Look<float>(ref depletion, "depletion");
			Scribe_Values.Look<float>(ref heatStored, "heatStored");
		}
		public override void CompTick()
		{
			base.CompTick();
			if (!parent.Spawned || parent.Destroyed || myNet == null || (map == null && !(parent is VehiclePawn)))
			{
				return;
			}
			if (this.parent.IsHashIntervalTick(120))
			{
				if (Props.heatVent > 0 && !Props.antiEntropic && !Disabled) //radiate to space
				{
					if (inSpace || parent is VehiclePawn)
						RemHeatFromNetwork(Props.heatVent);
					else
					{
						//higher outdoor temp, push less heat out
						float heat = Props.heatVent * GenMath.LerpDoubleClamped(0, 100, 2.5f, 0, map.mapTemperature.OutdoorTemp);
						//Log.Message("Remove heat:" + heat);
						RemHeatFromNetwork(heat);
					}

					if (myNet.venting)
					{
						if (!AddDepletionToNetwork(Props.heatVent))
							myNet.venting = false;
						if (!RemHeatFromNetwork(Props.heatVent * 5))
							myNet.venting = false;
					}
					else if (myNet.Depletion > 0 && mapComp.ShipMapState != ShipMapState.inCombat && !mapComp.Cloaks.Any(c => c.active))
						RemoveDepletionFromNetwork(Props.heatVent / 10f);
				}
				if (Props.antiEntropic)
				{
					if (myNet.Depletion > 0)
						RemoveDepletionFromNetwork(Props.heatVent * Props.antiEntropicRecoveryRate);
				}
				if (myNet.StorageUsed > 0)
				{
					float ratio = myNet.RatioInNetwork;
					if (ratio > 0.7f)
					{
						FleckMaker.ThrowHeatGlow(parent.Position, parent.Map, parent.DrawSize.x * 0.5f * Mathf.Pow(ratio, 3));
						if (ratio > 0.9f && !(parent is VehiclePawn))
							this.parent.TakeDamage(new DamageInfo(DamageDefOf.Burn, 10));
					}
					if (Props.antiEntropic) //convert heat
					{
						if (powerComp != null && powerComp.PowerNet != null && powerComp.PowerNet.batteryComps.Count > 0)
						{
							IEnumerable<CompPowerBattery> batteries = powerComp.PowerNet.batteryComps.Where(b => b.StoredEnergy <= b.Props.storedEnergyMax - 1);
							if (RemHeatFromNetwork(Props.heatVent) && batteries.Any())
							{
								batteries.RandomElement().AddEnergy(2);
							}
						}
						return;
					}
					PushHeat(ratio);
				}
			}
			if (myNet.venting && Props.heatVent > 0 && this.parent.IsHashIntervalTick(25)) //fx
			{
				Mote obj = (Mote)ThingMaker.MakeThing(ResourceBank.ThingDefOf.Mote_HeatsinkPurge);
				obj.exactPosition = parent.TrueCenter();
				obj.instanceColor = new Color(UnityEngine.Random.Range(0f,0.420f),0,UnityEngine.Random.Range(0.69f,1f));
				obj.rotationRate = 1.2f;
				if(Rand.Chance(0.2f))
					ResourceBank.SoundDefOf.ShipPurgeHiss.PlayOneShot(parent);
				GenSpawn.Spawn(obj, parent.Position, map);
			}
		}
		public void PushHeat(float ratio, float heat = 0) //bleed into or adjacent room
		{
			if (heat == 0)
				heat = Props.heatLoss * HeatPushMult * ratio;
			
			if (parent.def.passability != Traversability.Impassable) //tanks
			{
				TryPushHeat(pos, heat);
			}
			else //sinks are walls, check adjacent
			{
				foreach (IntVec3 vec in GenAdj.CellsAdjacent8Way(parent).ToList())
				{
					if (TryPushHeat(vec, heat))
						return;
				}
			}
		}
		private bool TryPushHeat(IntVec3 vec, float heat)
		{
			if (map == null)
				return false;
			//dont push to null, doors or space
			Room r = vec.GetRoom(map);
			if (r != null && !r.IsDoorway && !(inSpace && (r.OpenRoofCount > 0 || r.TouchesMapEdge)))
			{
				if (RemHeatFromNetwork(Props.heatLoss))
				{
					GenTemperature.PushHeat(vec, map, heat);
					//Log.Message("" + vec);
					return true;
				}
			}
			return false;
		}
		public override string CompInspectStringExtra()
		{
			string toReturn = base.CompInspectStringExtra();
			if (disabled)
			{
				toReturn += "\n<color=red>"+"SoSCantVentCloaked".Translate()+"</color>";
			}
			return toReturn;
		}
	}
}
