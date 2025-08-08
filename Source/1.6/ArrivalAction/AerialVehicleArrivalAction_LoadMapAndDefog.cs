using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vehicles;
using Vehicles.World;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace SaveOurShip2.Vehicles
{
    class AerialVehicleArrivalAction_LoadMapAndDefog : ArrivalAction_LoadMap
    {
        public AerialVehicleArrivalAction_LoadMapAndDefog() : base()
        {

        }

        public AerialVehicleArrivalAction_LoadMapAndDefog(VehiclePawn vehicle, LaunchProtocol launchProtocol, int tile, AerialVehicleArrivalModeDef arrivalModeDef)
        : base(vehicle, arrivalModeDef)
        {

        }

        public override void Arrived(GlobalTargetInfo target)
        {
            int tile = target.Tile;
            LongEventHandler.QueueLongEvent((Action)delegate
            {
                Map map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, null);
                SOS2MapUtility.TryLinkMapToWorldObject(map, tile);
                // CHANGE 1.6 - new hasMap parameter
                MapLoaded(map, true);
                FloodFillerFog.FloodUnfog(CellFinderLoose.TryFindCentralCell(map, 7, 10, (IntVec3 x) => !x.Roofed(map)), map);
                ExecuteEvents();
                GenStep_Fog.UnfogMapFromEdge(map);
                arrivalModeDef.Worker.VehicleArrived(vehicle, vehicle.CompVehicleLauncher.launchProtocol, map);
            }, "GeneratingMap", false, null, true, false);
        }
    }
}
