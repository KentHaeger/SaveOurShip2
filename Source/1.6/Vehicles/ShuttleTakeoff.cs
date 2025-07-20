using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Vehicles;
using Verse;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using SmashTools.Targeting;
using static SaveOurShip2.ShipMapComp;

namespace SaveOurShip2.Vehicles
{
    class ShuttleTakeoff : VTOLTakeoff
    {
        public ShuttleMissionData TempMissionRef;

        public ShuttleTakeoff()
        {

        }

        public ShuttleTakeoff(VTOLTakeoff reference, VehiclePawn vehicle) : base(reference, vehicle)
        {

        }


        public override IEnumerable<ArrivalOption> GetArrivalOptions(GlobalTargetInfo target)
		{
            if (target.Tile == PlanetTile.Invalid)
			{
                foreach(ArrivalOption option in base.GetArrivalOptions(target))
				{
                    yield return option;
				}
                yield break;
            }
            //td not sure the limits we want on this, right now you could assault from anywhere, should it be only from ship-ship?
            var mp = Find.World.worldObjects.MapParentAt(target.Tile);
            if (mp != null && (mp.def == ResourceBank.WorldObjectDefOf.ShipOrbiting || mp.def == ResourceBank.WorldObjectDefOf.ShipEnemy)) //target is ship
			{
				var mapComp = mp.Map.GetComponent<ShipMapComp>();
				if (mapComp.ShipMapState == ShipMapState.inCombat) //target is in combat
				{
					if (mapComp.map != mapComp.ShipCombatOriginMap) //target is enemy ship
					{
						foreach (ArrivalOption giz in FloatMenuMissions(target.Tile, mapComp))
							yield return giz;
                        yield break;
					}
					else //target is player ship
					{
						yield return ArrivalOption_ReturnFromEnemy(target.Tile);
					}
				}
			}
            List<ArrivalOption> baseOptions = new List<ArrivalOption>(base.GetArrivalOptions(target));
            // In this case, framework only allows to form caravan at the tile with map parent, which is nether site, nor settlement
            bool vehicleCaravanCondition = WorldVehiclePathGrid.Instance.Passable(target.Tile, vehicle.VehicleDef) &&
                !Find.WorldObjects.AnySettlementBaseAt(target.Tile) && !Find.WorldObjects.AnySiteAt(target.Tile);
            // But when there is SOS 2 map parent, add option to land at that map
            if (mp != null && vehicleCaravanCondition)
            {
                yield return new ArrivalOption("LandInExistingMap".Translate(vehicle.Label),
                    continueWith: delegate (TargetData<GlobalTargetInfo> targetData)
                    {
                        Current.Game.CurrentMap = mp.Map;
                        CameraJumper.TryHideWorld();
                        LandingTargeter.Instance.BeginTargeting(vehicle,
                                action: delegate (LocalTargetInfo landingCell, Rot4 rot)
                        {
                            if (vehicle.Spawned)
                            {
                                vehicle.CompVehicleLauncher.Launch(targetData,
                                        new ArrivalAction_LandToCell(vehicle, mp, landingCell.Cell, rot));
                            }
                            else
                            {
                                AerialVehicleInFlight aerialVehicle = vehicle.GetOrMakeAerialVehicle();
                                List<FlightNode> nodes = targetData.targets.Select(tgt => new FlightNode(tgt)).ToList();
                                aerialVehicle.OrderFlyToTiles(nodes,
                                        new ArrivalAction_LandToCell(vehicle, mp, landingCell.Cell, rot));
                                vehicle.CompVehicleLauncher.inFlight = true;
                                CameraJumper.TryShowWorld();
                            }
                        }, allowRotating: vehicle.VehicleDef.rotatable,
                                targetValidator: targetInfo =>
                                    !Ext_Vehicles.IsRoofRestricted(vehicle.VehicleDef, targetInfo.Cell, mp.Map));
                    });
            }
            if (baseOptions.Count==0)
            {

            }
            else
            {
                foreach (ArrivalOption option in baseOptions)
                    yield return option;
            }
            
        }

        public IEnumerable<ArrivalOption> FloatMenuMissions(int tile, ShipMapComp mapComp)
		{
			yield return ArrivalOption_Board(tile, mapComp);
			//samey in CompShuttleLauncher.CompGetGizmosExtra
			if (vehicle.CompUpgradeTree != null)
			{
				bool hasLaser = ShipInteriorMod2.ShuttleHasLaser(vehicle);
				if (hasLaser)
					yield return ArrivalOption_Intercept(tile);
				if (hasLaser || ShipInteriorMod2.ShuttleHasPlasma(vehicle))
					yield return ArrivalOption_Strafe(tile);
				if (ShipInteriorMod2.ShuttleHasTorp(vehicle))
					yield return ArrivalOption_Bomb(tile);
			}
        }

        ArrivalOption ArrivalOption_Board(int tile, ShipMapComp mapComp)
        {
            string text = "SoS.ShuttleMissionFloatBoardWarn".Translate();
            if (ShipInteriorMod2.ShuttleShouldBoard(mapComp, vehicle))
                text = "SoS.ShuttleMissionFloatBoard".Translate();
            return new ArrivalOption(text, delegate { LaunchShuttleToCombatManager(vehicle, ShuttleMission.BOARD); });
        }

        ArrivalOption ArrivalOption_Intercept(int tile)
        {
            return new ArrivalOption("SoS.ShuttleMissionFloatIntercept".Translate(), delegate { LaunchShuttleToCombatManager(vehicle, ShuttleMission.INTERCEPT); });
        }

        ArrivalOption ArrivalOption_Strafe(int tile)
        {
            return new ArrivalOption("SoS.ShuttleMissionFloatStrafe".Translate(), delegate { LaunchShuttleToCombatManager(vehicle, ShuttleMission.STRAFE); });
        }

        ArrivalOption ArrivalOption_Bomb(int tile)
        {
            return new ArrivalOption("SoS.ShuttleMissionFloatTorpedo".Translate(), delegate { LaunchShuttleToCombatManager(vehicle, ShuttleMission.BOMB); });
        }

        ArrivalOption ArrivalOption_ReturnFromEnemy(int tile)
        {
            return new ArrivalOption("SoS.ShuttleMissionFloatReturn".Translate(), delegate { LaunchShuttleToCombatManager(vehicle, ShuttleMission.BOARD); });
        }

        public static void LaunchShuttleToCombatManager(VehiclePawn vehicle, ShuttleMission mission, bool fromEnemy=false)
        {
            vehicle.CompVehicleLauncher.inFlight = true;
            vehicle.CompVehicleLauncher.launchProtocol.OrderProtocol(LaunchProtocol.LaunchType.Takeoff);
            VehicleSkyfaller_Leaving vehicleSkyfaller_Leaving = (VehicleSkyfaller_Leaving)VehicleSkyfallerMaker.MakeSkyfaller(vehicle.CompVehicleLauncher.Props.skyfallerLeaving, vehicle);
            vehicleSkyfaller_Leaving.vehicle = vehicle;
            vehicleSkyfaller_Leaving.createWorldObject = false;
            GenSpawn.Spawn(vehicleSkyfaller_Leaving, vehicle.Position, vehicle.Map, vehicle.CompVehicleLauncher.launchProtocol.CurAnimationProperties.forcedRotation ?? vehicle.Rotation);
            Map map;
            if (fromEnemy)
                map = vehicle.Map;
            else
                map = ShipInteriorMod2.FindPlayerShipMap();
            ((ShuttleTakeoff)vehicle.CompVehicleLauncher.launchProtocol).TempMissionRef = map.GetComponent<ShipMapComp>().RegisterShuttleMission(vehicle, mission);
            CameraJumper.TryHideWorld();
            vehicle.EventRegistry[VehicleEventDefOf.AerialVehicleLaunch].ExecuteEvents();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<ShuttleMissionData>(ref TempMissionRef, "missionRef");
        }
    }
}
