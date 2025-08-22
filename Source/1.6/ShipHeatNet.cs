﻿using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	public class ShipHeatNet
	{
		public HashSet<CompShipHeat> Connectors = new HashSet<CompShipHeat>();
		public HashSet<CompShipHeatSource> Sources = new HashSet<CompShipHeatSource>();
		public HashSet<CompShipHeatSink> Sinks = new HashSet<CompShipHeatSink>();
		public HashSet<CompShipHeatPurge> HeatPurges = new HashSet<CompShipHeatPurge>();
		public HashSet<CompShipHeatShield> Shields = new HashSet<CompShipHeatShield>();
		public HashSet<CompShipHeat> Turrets = new HashSet<CompShipHeat>();
		public HashSet<CompShipHeat> Cloaks = new HashSet<CompShipHeat>();
		public HashSet<Building_ShipBridge> AICores = new HashSet<Building_ShipBridge>();
		public HashSet<Building_ShipBridge> TacCons = new HashSet<Building_ShipBridge>();
		public HashSet<Building_ShipBridge> PilCons = new HashSet<Building_ShipBridge>();
		public int GridID;
		// CE Compatibility for turrets added to heat net
		private static Type turretCEtype = null;
		private static MethodInfo turretCastMetod = null;
		private static bool turretCEinitialized = false;
		// For unknown buildings
		public static Building_ShipTurret CESafeTryCastToTurret(ThingWithComps thing)
		{
			if (thing is Building_ShipTurret)
			{
				return thing as Building_ShipTurret;
			}
			else if (IsCompatibleCETurret(thing))
			{
				return (Building_ShipTurret)turretCastMetod.Invoke(thing, new object[] { });
			}
			else
			{
				return null;
			}
		}
		// For cases when turret is added to list of turrets and guaranteed to be either Building_ShipTurret or Building_ShipTurretCE
		public static Building_ShipTurret CESafeCastToTurret(ThingWithComps thing)
		{
			if (IsCompatibleCETurret(thing))
			{
				return (Building_ShipTurret)turretCastMetod.Invoke(thing, new object[] { });
			}
			else
			{
				return (Building_ShipTurret)thing;
			}
		}
		public static bool IsCompatibleCETurret(ThingWithComps thing)
		{
			if (thing == null)
			{
				return false;
			}
			if (!turretCEinitialized)
			{
				try
				{
					if (ModIntegration.IsCEEnabled())
					{
						turretCEtype = Type.GetType("CombatExtended.Compatibility.SOS2Compat.Building_ShipTurretCE, SOS2Compat", false);
						turretCastMetod = turretCEtype?.GetMethod("ToBuilding_ShipTurret") ?? null;
					}
					turretCEinitialized = true;
				}
				catch (Exception e)
				{
					turretCEtype = null;
					turretCEinitialized = true;
				}
			}
			if (turretCEtype == null || turretCastMetod == null)
			{
				return false;
			}
			return turretCEtype.IsAssignableFrom(thing.GetType());
		}
		// Accuracy boost from tactical console is per-heat-net.
		// However, dodge chance boost for completel dodging projectiles is map-wide 
		public int AccuracyBoost
		{
			get
			{
				int result = 0;
				if (TacCons.Any(b => b.mannableComp.MannedNow))
					result = TacCons.Where(b => b.mannableComp.MannedNow).Max(b => b.mannableComp.ManningPawn.skills.GetSkill(SkillDefOf.Shooting).Level);
				if (result < 10 && AICores.Any())
					result = 10;
				return result;
			}
		}
					
		public float StorageCapacity //usable capacity (minus depletion)
		{ 
			get
			{
				return StorageCapacityRaw * (1 - DepletionRatio);
			}
		}
		public float StorageCapacityRaw //max capacity
		{
			get;
			private set;
		}
		public float StorageAvailable => StorageCapacity - StorageUsed;
		public float StorageUsed { get; private set; }
		public float Depletion { get; private set; }
		public bool venting;

		private bool ratioDirty = true; //if we add/rem heat, etc
		private bool depletionDirty = true; //Depletion has been added/removed
		private float depletionRatio = 0;
		private float ratioInNetwork = 0; //ratio in usable capacity
		public float RatioInNetworkRaw = 0; //ratio from max
		public float RatioInNetwork
		{
			get
			{
				if (ratioDirty)
				{
					if (float.IsNaN(StorageUsed))
					{
						Log.Warning("NaN prevented in RatioInNetwork!");
						StorageUsed = 0;
					}
					if (StorageCapacityRaw <= 0)
					{
						ratioDirty = false;
						return StorageCapacityRaw = 0;
					}
					ratioInNetwork = Mathf.Clamp(StorageUsed / StorageCapacity, 0, 1);
					RatioInNetworkRaw = Mathf.Clamp(StorageUsed / StorageCapacityRaw, 0, 1);
					ratioDirty = false;
				}
				return ratioInNetwork;
			}
		}
		public float DepletionRatio
		{
			get
			{
				if (depletionDirty)
				{
					if (float.IsNaN(Depletion))
					{
						Log.Warning("NaN prevented in DepletionRatio!");
						Depletion = 0;
					}
					if (Depletion <= 0)
					{
						depletionDirty = false;
						return Depletion = 0;
					}
					depletionRatio = Mathf.Clamp(Depletion / StorageCapacityRaw, 0, 1);
					depletionDirty = false;
				}
				return depletionRatio;
			}
		}

		public void Register(CompShipHeat comp)
		{
			if (comp is CompShipHeatSink sink)
			{
				if (Sinks.Add(sink))
				{
					//add to net
					//Log.Message("grid: " + GridID + " add:" + sink.heatStored + " Total:" + StorageUsed + "/" + StorageCapacity + " depletion:" + sink.depletion + " Total:" + Depletion);
					StorageCapacityRaw += sink.Props.heatCapacity;
					StorageUsed += sink.heatStored;
					Depletion += sink.depletion;
					sink.heatStored = 0;
					sink.depletion = 0;
					//Log.Message("grid: "+ GridID +" add:"+ sink.heatStored + " Total:" + StorageUsed +"/"+ StorageCapacity + " depletion:" + sink.depletion + " Total:" + Depletion);
					ratioDirty = true;
					depletionDirty = true;
					if (comp is CompShipHeatPurge purge)
					{
						HeatPurges.Add(purge);
					}
				}
			}
			else if (comp.parent is Building_ShipTurret || IsCompatibleCETurret(comp.parent))
			{
				Turrets.Add(comp);
				ThingDef parentDef = comp.parent.def;
			}
			else if (comp is CompShipHeatSource source)
			{
				if (Sources.Add(source))
				{
					if (source.parent is Building_ShipCloakingDevice)
						Cloaks.Add(source);
				}
			}
			else if (comp is CompShipHeatShield shield)
				Shields.Add(shield);
			else if (comp.parent is Building_ShipBridge br)
			{
				if (br.mannableComp == null)
					AICores.Add(br);
				else if (br.TacCon)
					TacCons.Add(br);
				else
					PilCons.Add(br);
			}
			Connectors.Add(comp);
			//Log.Message(Sinks.Count + " " + HeatPurges.Count +" " + Turrets.Count + " " + Sources.Count + " " + Cloaks.Count + " " + Shields.Count + " " + AICores.Count + " " + TacCons.Count + " " + PilCons.Count);
		}
		public void DeRegister(CompShipHeat comp)
		{
			if (comp is CompShipHeatSink sink)
			{
				//rem from net with a factor
				//Log.Message("grid: " + GridID + " rem:" + sink.heatStored + " Total:" + StorageUsed + "/" + StorageCapacity + " depletion:" + sink.depletion + " Total:" + Depletion);
				if (float.IsNaN(StorageUsed))
				{
					Log.Warning("NaN prevented in DeRegister!");
					StorageUsed = 0;
				}
				if (StorageCapacity <= 0)
					sink.heatStored = 0;
				else
					sink.heatStored = Mathf.Clamp(StorageUsed * sink.Props.heatCapacity / StorageCapacityRaw, 0, sink.Props.heatCapacity);
				if (Depletion <= 0)
					sink.depletion = 0;
				else
					sink.depletion = Mathf.Clamp(Depletion * sink.Props.heatCapacity / StorageCapacityRaw, 0, sink.Props.heatCapacity);
				RemoveHeat(sink.heatStored);
				RemoveDepletion(sink.depletion);
				StorageCapacityRaw -= sink.Props.heatCapacity;
				//Log.Message("grid: " + GridID + " rem:" + sink.heatStored + " Total:" + StorageUsed + "/" + StorageCapacity + " depletion:" + sink.depletion + " Total:"+Depletion);
				Sinks.Remove(sink);
				ratioDirty = true;
				depletionDirty = true;
				if (comp is CompShipHeatPurge purge)
					HeatPurges.Remove(purge);
			}
			else if (comp.parent is Building_ShipTurret)
				Turrets.Remove(comp);
			else if (comp is CompShipHeatSource source)
			{
				Sources.Remove(source);
				if (source.parent is Building_ShipCloakingDevice)
					Cloaks.Remove(source);
			}
			else if (comp is CompShipHeatShield shield)
				Shields.Remove(shield);
			else if (comp.parent is Building_ShipBridge br)
			{
				if (br.mannableComp == null)
					AICores.Remove(br);
				else if (br.TacCon)
					TacCons.Remove(br);
				else
					PilCons.Remove(br);
			}
			Connectors.Remove(comp);
		}
		public void AddHeat(float amount)
		{
			StorageUsed += amount;
			ratioDirty = true;
		}
		public void RemoveHeat(float amount)
		{
			StorageUsed -= amount;
			if (float.IsNaN(StorageUsed))
			{
				Log.Warning("NaN prevented in RemoveHeat!");
				StorageUsed = 0;
			}
			if (StorageUsed < 0)
				StorageUsed = 0;
			ratioDirty = true;
		}
		public void AddDepletion(float amount)
		{
			if (amount > StorageAvailable) //remove heat to add depletion
				RemoveHeat(amount - StorageAvailable);
			Depletion += amount;
			depletionDirty = true;
			//ratioDirty = true;
		}
		public void RemoveDepletion(float amount)
		{
			Depletion -= amount;
			if (float.IsNaN(Depletion))
			{
				Log.Warning("NaN prevented in RemoveDepletion!");
				Depletion = 0;
			}
			if (Depletion < 0)
				Depletion = 0;
			depletionDirty = true;
		}
		public bool AnyShieldOn()
		{
			return Shields.Any(s => s.flickComp.SwitchIsOn == true);
		}
		public bool AnyCloakOn()
		{
			return Cloaks.Any(c => c.parent.TryGetComp<CompFlickable>().SwitchIsOn == true);
		}
		public void ShieldsOn()
		{
			foreach (var shield in Shields)
			{
				shield.flickComp.SwitchIsOn = true;
			}
		}
		public void ShieldsOff()
		{
			foreach (var shield in Shields)
			{
				shield.flickComp.SwitchIsOn = false;
			}
		}
		public void TurretsOff()
		{
			foreach (var turret in Turrets)
			{
				((Building_ShipTurret)turret.parent).ResetForcedTarget();
			}
		}

		public void StartVent()
		{
			venting = true;
		}
	}
}
