using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	public class WeaponGroup : IExposable
	{
		private HashSet<Building_ShipTurret> turrets = new HashSet<Building_ShipTurret>();
		public virtual void ExposeData()
		{
			RemoveDestroyed();
			Scribe_Collections.Look<Building_ShipTurret>(ref turrets, "turrets", LookMode.Reference);
			if (turrets == null)
			{
				turrets = new HashSet<Building_ShipTurret>();
			}
		}

		public void Add(Building_ShipTurret turret)
		{
			turrets.Add(turret);
		}

		public void Remove(Building_ShipTurret turret) 
		{
			turrets.Remove(turret);
		}

		public bool Contains(Building_ShipTurret turret)
		{
			return turrets.Contains(turret);
		}

		public int Count
		{
			get
			{
				return turrets.Count;
			}
		}

		public void Select()
		{
			RemoveDestroyed();
			Map playerShipMap = ShipInteriorMod2.FindPlayerShipMap();
			if (playerShipMap == null)
			{
				return;
			}

			PreventCameraJump.Enabled = true;
			PreventCurrentMapSwitch.Enabled = true;
			Find.Selector.ClearSelection();
			foreach (Building_ShipTurret turret in turrets)
			{
				// Ideally, command should be available from both player and enemy ship map, maybe via persistent UI.
				// And to avoid any complications related to weapons selected on different maps, limited to player ship map only.
				if(turret.Spawned && turret.Map == playerShipMap && turret.Faction == Faction.OfPlayer)
				{
					Find.Selector.Select(turret);
				}				
			}
			PreventCameraJump.Enabled = false;
			PreventCurrentMapSwitch.Enabled = false;
		}

		public void ToggleTurret(Building_ShipTurret turret)
		{
			if (Contains(turret))
			{
				Remove(turret);
			}
			else
			{
				Add(turret);
			}
		}

		private int ColorUpdateTick = -1;
		private Color cachedColor;
		// Just faster than every second, but nowhere neatr as fast as every tick
		private const int ColorUpdateInterval = 20;

		private void RemoveDestroyed()
		{
			turrets.RemoveWhere(t => t.Destroyed);
		}

		// Turret count required to be considered dominant in the group
		private int CountToBeDominant()
		{
			return Count / 2 + 1;
		}
		
		public bool IsPDDominant()
		{
			return turrets.Where(t => t.heatComp?.Props?.pointDefense ?? false).Count() >= CountToBeDominant();
		}

		public bool IsSpinalDominant()
		{
			return turrets.Where(t => t.spinalComp != null).Count() >= CountToBeDominant();
		}
		public Color GetColor()
		{
			if (Find.TickManager.TicksGame + ColorUpdateInterval > ColorUpdateTick)
			{
				RemoveDestroyed();
				ColorUpdateTick = Find.TickManager.TicksGame;
				if (turrets.NullOrEmpty())
				{
					cachedColor = Color.black;
					return cachedColor;
				}
				// Default to gray color for modded turrets that can't be automatically recognized by def name
				cachedColor = Color.gray;
				// Identifying turrets by searching substring in their def names just works for Vanilla SOS 2 turrets.
				// Implementing some strict identifying system is theoretically desired, but waay too much effort for now.
				List<string> turretNames = new List<string>{ "Kinetic", "Plasma", "Laser", "ACI", "Torpedo" };
				List<Color> turretColors = new List<Color> { Color.blue, Color.green, Color.red, Color.yellow, Color.magenta };
				List<int> turretCounts = new List<int>(new int[turretNames.Count]);
				for (int i = 0; i < turretNames.Count; i++)
				{
					turretCounts[i] = turrets.Where(t => t.def.defName.IndexOf(turretNames[i], StringComparison.CurrentCultureIgnoreCase) > 0).Count();
				}
				int maxTurretCount = turretCounts.Max();
				for (int i = 0; i < turretNames.Count; i++)
				{
					if (turretCounts[i] == maxTurretCount)
					{
						cachedColor = turretColors[i];
						break;
					}
				}
			}
			return cachedColor;
		}
	}
}
