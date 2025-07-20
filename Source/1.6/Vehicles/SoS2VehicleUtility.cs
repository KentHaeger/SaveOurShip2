using System;
using System.Linq;
using SaveOurShip2.Vehicles;
using Vehicles;
using Verse;

namespace SaveOurShip2
{
    public class SoS2VehicleUtility
    {
        private static string[] upgradeableShuttleNames = { "SoS2_Shuttle", "SoS2_Shuttle_Heavy", "SoS2_Shuttle_Superheavy" };
        private static string personalShuttleName = "SoS2_Shuttle_Personal";
        public static bool IsPersonalShuttle(VehiclePawn vehicle)
		{
            if (vehicle == null)
			{
                return false;
			}
            return vehicle.def.defName == personalShuttleName;
        }

        public static bool IsUpgradeableShuttle(VehiclePawn vehicle)
        {
            if (vehicle == null)
            {
                return false;
            }
            return upgradeableShuttleNames.Contains(vehicle.def.defName);
        }

        public static bool IsShuttle(VehiclePawn vehicle)
		{
            return IsPersonalShuttle(vehicle) || IsUpgradeableShuttle(vehicle);

        }
    }
}
