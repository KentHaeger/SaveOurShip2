using SaveOurShip2.Vehicles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vehicles;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
    public static class VehicleEventMethods
    {
        public static void ShuttleLaunched(DefaultTakeoff takeoff)
        {
            CompVehicleHeatNet net = takeoff.vehicle.TryGetComp<CompVehicleHeatNet>();
            net?.RebuildHeatNet();
            if (((ShuttleTakeoff)takeoff).TempMissionRef != null)
            {
                ((ShuttleTakeoff)takeoff).TempMissionRef.liftedOffYet = true;
                ((ShuttleTakeoff)takeoff).TempMissionRef = null;
            }
        }

        public static void ShuttleLanded(DefaultTakeoff landing)
        {
            VehiclePawn vehicle = landing.vehicle; 
            CompVehicleHeatNet net = vehicle.TryGetComp<CompVehicleHeatNet>();
            landing.vehicle.ignition.Drafted = false;
            net?.RebuildHeatNet();
            if (vehicle.Faction == Faction.OfPlayer && vehicle.Spawned)
            {
                // Failsafe - unfog from vehicle location.
                FloodFillerFog.FloodUnfog(vehicle.Position, vehicle.Map);
                // When landing on ship bay that is already unfogged (bay feature) within fogged room need to unfog adjacent.
                Building bay = (Building)vehicle.Position.GetThingList(vehicle.Map).Where(t => ((t as ThingWithComps)?.TryGetComp<CompShipBay>() ?? null) != null).DefaultIfEmpty(null).First();
                if (bay != null)
                {
                    foreach (IntVec3 cellToUnfog in GenAdj.CellsAdjacentCardinal(bay).Where(cell => !cell.Impassable(vehicle.Map)))
                    {
                        FloodFillerFog.FloodUnfog(cellToUnfog, vehicle.Map);
                    }
                }
            }
        }
    }
}
