using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;

namespace SaveOurShip2.Vehicles
{
	public class AerialVehicleArrivalAction_ShipAssault : ArrivalAction_LandToCell
	{
		public AerialVehicleArrivalAction_ShipAssault(VehiclePawn vehicle, MapParent mapParent)
		{
			this.mapParent = mapParent;
			this.vehicle = vehicle;
			this.landingCell = FindTargetForShuttle(mapParent.Map);
		}
		public IntVec3 FindTargetForShuttle(Map map)
		{
			//prioritize active non wreck ships
			List<IntVec3> targetCells = new List<IntVec3>();
			List<IntVec3> validCells = new List<IntVec3>();
			var mapComp = map.GetComponent<ShipMapComp>();
			foreach (SpaceShipCache ship in mapComp.ShipsOnMap.Values.Where(s => !s.IsWreck))
			{
				validCells.AddRange(ship.OuterCells());
			}
			if (targetCells.NullOrEmpty())
			{
				foreach (SpaceShipCache ship in mapComp.ShipsOnMap.Values.Where(s => s.IsWreck))
				{
					validCells.AddRange(ship.OuterCells());
				}
			}
			return validCells.RandomElement();
		}
	}
}
