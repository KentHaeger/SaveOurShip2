using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vehicles;

namespace SaveOurShip2.Vehicles
{
    class ShuttleSignalJammerUpgrade : Upgrade
    {
        public override bool UnlockOnLoad => true;

        public override void Refund(VehiclePawn vehicle)
        {
            vehicle.CompVehicleLauncher.signalJammer = false;
        }

        public override void Unlock(VehiclePawn vehicle, bool unlockingAfterLoad)
        {
            vehicle.CompVehicleLauncher.signalJammer = true;
        }
    }
}
