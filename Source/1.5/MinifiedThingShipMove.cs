﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace SaveOurShip2
{
	class MinifiedThingShipMove : MinifiedThing
	{
		public Building shipRoot;
		public IntVec3 bottomLeftPos;
		public byte shipRotNum;
		public bool includeRock = false;
		public Map originMap = null;
		public Map targetMap = null;
		public bool atmospheric = false;
		public float fuelPaidByTarget = 0;
		public Faction fac = null;

		public override void Tick()
		{
			base.Tick();
			if (Find.Selector.SelectedObjects.Count > 1 || !Find.Selector.SelectedObjects.Contains(this))
			{
				if (InstallBlueprintUtility.ExistingBlueprintFor(this) != null)
				{
					if (atmospheric) //transit from/to space, pick landing site and set vars instead of moving
					{
						//ShipInteriorMod2.LaunchShip counterpart
						bool originIsSpace = originMap.IsSpace();
						var mapComp = originMap.GetComponent<ShipMapComp>();
						var ship = mapComp.ShipsOnMap[((Building_ShipBridge)shipRoot).ShipIndex];
						IntVec3 adj = IntVec3.Zero;
						WorldObjectOrbitingShip mapPar; //origin might not be WOS
						
						foreach(Building building in ship.Buildings)
                        {
							if (building is Building_ShipAirlock airlock && airlock.Outerdoor())
								airlock.SetForbidden(true);
                        }
						
						if (!originIsSpace || (originIsSpace && (mapComp.ShipsOnMap.Count > 1 || originMap.mapPawns.AllPawns.Any(p => !mapComp.MapShipCells.ContainsKey(p.Position))))) //to either with temp map
						{
							//spawn new WO and map
							WorldObjectOrbitingShip transit = (WorldObjectOrbitingShip)WorldObjectMaker.MakeWorldObject(ResourceBank.WorldObjectDefOf.WreckSpace);
							transit.drawPos = originMap.Parent.DrawPos;
							transit.SetFaction(Faction.OfPlayer);
							transit.Tile = ShipInteriorMod2.FindWorldTile();
							Find.WorldObjects.Add(transit);
							Map newMap = MapGenerator.GenerateMap(originMap.Size, transit, transit.MapGeneratorDef);
							newMap.fogGrid.ClearAllFog();
							mapComp = newMap.GetComponent<ShipMapComp>();
							mapPar = transit;

							//set vecs //td
							//adj = ship.CenterShipOnMap();
							//mapComp.MoveToVec = adj.Inverse();

							//move
							ShipInteriorMod2.MoveShip(shipRoot, newMap, adj, fac, shipRotNum, includeRock);
							if (!originIsSpace)
								newMap.weatherManager.TransitionTo(ResourceBank.WeatherDefOf.OuterSpaceWeather);
						}
						else //to ground with originMap - spacehome
						{
							mapPar = (WorldObjectOrbitingShip)originMap.Parent;
						}
						mapComp.MoveToVec = InstallBlueprintUtility.ExistingBlueprintFor(this).Position - bottomLeftPos;
						mapComp.MoveToMap = targetMap;
						mapComp.MoveToTile = targetMap.Tile;

						//vars1
						mapPar.originDrawPos = originMap.Parent.DrawPos;
						if (originIsSpace) //to ground either
						{
							mapPar.targetDrawPos = targetMap.Parent.DrawPos;
							mapComp.Heading = -1;
							mapComp.Altitude = mapComp.Altitude - 1;
							mapComp.Takeoff = false;
						}
						else //to space with temp map
						{
							mapPar.targetDrawPos = ShipInteriorMod2.FindPlayerShipMap().Parent.DrawPos;
							mapComp.Heading = 1;
							mapComp.Altitude = ShipInteriorMod2.altitudeLand; //startup altitude
							mapComp.Takeoff = true;
						}

						//vars2
						mapComp.BurnTimer = Find.TickManager.TicksGame;
						mapComp.PrevMap = originMap;
						mapComp.PrevTile = originMap.Tile;
						mapComp.EnginesOn = true;
						mapComp.ShipMapState = ShipMapState.inTransit;
						CameraJumper.TryJump(mapComp.MapRootListAll.FirstOrDefault().Position, originMap);
					}
					else //normal move to target map, claim moved ships for player
					{
						if (fuelPaidByTarget > 0) //moved with bay, paid by target map
						{
							Log.Message("SOS2 fuelPaidByTarget: " + fuelPaidByTarget);
							var targetMapComp = targetMap.GetComponent<ShipMapComp>();
							List<CompEngineTrail> engines = new List<CompEngineTrail>();
							List<SpaceShipCache> ships = new List<SpaceShipCache>();
							float fuel = 0;
							foreach (SpaceShipCache ship in targetMapComp.ShipsOnMap.Values)
							{
								if (ship.CanFire() && ship.HasMannedBridge() && ship.HasRCS())
								{
									ships.Add(ship);
									foreach (CompEngineTrail engine in ship.Engines.Where(e => e.FuelUse > 0))
									{
										fuel += engine.refuelComp.Fuel;
										if (engine.PodFueled)
											fuel += engine.refuelComp.Fuel;
										engines.Add(engine);
									}
								}
							}
							foreach (SpaceShipCache ship in ships)
							{
								foreach (CompEngineTrail engine in engines)
								{
									float consume = fuelPaidByTarget * engine.refuelComp.Fuel / fuel;
									if (engine.PodFueled)
										consume *= 0.5f;
									engine.refuelComp.ConsumeFuel(Mathf.Min(consume, engine.refuelComp.Fuel));
								}
							}
						}
						ShipInteriorMod2.MoveShip(shipRoot, targetMap, InstallBlueprintUtility.ExistingBlueprintFor(this).Position - bottomLeftPos, fac, shipRotNum, includeRock);
					}
				}
				if (!Destroyed)
					Destroy(DestroyMode.Vanish);
			}
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			if (Graphic is Graphic_Single)
			{
				Graphic.Draw(drawLoc, Rot4.North, this, 0f);
				return;
			}
			Graphic.Draw(drawLoc, Rot4.South, this, 0f);
		}
	}
}
