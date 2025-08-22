﻿using SmashTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vehicles;
using Verse;

namespace SaveOurShip2.Vehicles
{
    class ShuttleShieldUpgrade : Upgrade
    {
        public CompProps_ShipHeat shield;

        public override bool UnlockOnLoad => true;

        public override void Refund(VehiclePawn vehicle)
        {
            CompShipHeatShield shield = vehicle.GetComp<CompShipHeatShield>();
            vehicle.comps.Remove(shield);
            ShipMapComp mapComp = vehicle.Map.GetComponent<ShipMapComp>();
            if (mapComp != null && mapComp.Shields.Contains(shield))
                mapComp.Shields.Remove(shield);
            VehicleComponent shieldGenerator = vehicle.statHandler.components.First(comp => comp.props.key == "shieldGenerator");
            shieldGenerator.SetHealthModifier = 1;
            shieldGenerator.health = 1;
            if (vehicle.GetComp<CompShipHeat>() == null)
                vehicle.comps.Remove(vehicle.GetComp<CompVehicleHeatNet>());
            else
                vehicle.GetComp<CompVehicleHeatNet>().RebuildHeatNet();
        }

        public override void Unlock(VehiclePawn vehicle, bool unlockingAfterLoad)
        {
            //Check if we've already unlocked this... unlocking on load is unpredictable at times
            if (vehicle.GetComp<CompShipHeatShield>() != null)
            {
                Log.Warning("[SoS2] Huh. Tried to unlock a shuttle shield upgrade twice. You should probably report this.");
                return;
            }
            CompVehicleHeatNet net = vehicle.GetComp<CompVehicleHeatNet>();
            if (net == null)
            {
                net = new CompVehicleHeatNet();
                net.parent = vehicle;
                vehicle.AddComp(net);
            }
            VehicleComponent shieldGenerator = vehicle.statHandler.componentsByKeys["shieldGenerator"];
            shieldGenerator.SetHealthModifier = 50;
            shieldGenerator.health = 50;
            CompShipHeatShield myShield = new CompShipHeatShield();
            myShield.parent = vehicle;
            myShield.Initialize(shield);
            vehicle.AddComp(myShield);
            net.RebuildHeatNet();
            if (!unlockingAfterLoad)
            {
                if (vehicle.Spawned)
                {
                    myShield.PostSpawnSetup(unlockingAfterLoad);
                    ShipMapComp mapComp = vehicle.Map.GetComponent<ShipMapComp>();
                    if (mapComp != null)
                        mapComp.Shields.Add(myShield);
                }
            }
        }
    }
}
