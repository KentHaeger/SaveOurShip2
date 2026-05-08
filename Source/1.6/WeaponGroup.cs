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

		public void Select()
		{
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
	}
}
