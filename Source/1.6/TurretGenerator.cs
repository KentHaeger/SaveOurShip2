using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Verse;

namespace SaveOurShip2
{
	// Procedurally-generated turrets near outer airlocks for enemy ships
	public class TurretGenerator
	{
		private Map map;
		ShipMapComp mapComp;
		Faction shipFaction;
		SpaceShipCache ship;
		public TurretGenerator(Map map, Faction shipFaction, SpaceShipCache ship)
		{
			this.map = map;
			this.mapComp = map.GetComponent<ShipMapComp>();
			this.shipFaction = shipFaction;
			this.ship = ship;
		}
		public bool TryAddturretNearOuterdoor(Building_ShipAirlock airlock)
		{
			// Adjacent to walls ajacent to outerdoors.
			// Or if docking clamps are adjacent to outerdoor, try skipping to next wall.
			List<IntVec3> rootTiles = new List<IntVec3>() { airlock.Position };
			List<IntVec3> adjacent = GenAdj.CellsAdjacentCardinal(airlock).ToList();
			foreach (IntVec3 tile in adjacent)
			{
				Thing clamp = map.GetThingOfDefAt(tile, ResourceBank.ThingDefOf.ShipAirlockBeam);
				if (clamp != null)
				{
					rootTiles.Add(tile);
				}
			}

			List<IntVec3> wallCandidates = new List<IntVec3>();
			foreach (IntVec3 tile in rootTiles)
			{
				wallCandidates.AddRange(GenAdj.CellsAdjacentCardinal(tile, Rot4.North, new IntVec2(1, 1)).ToList());
			}
			wallCandidates = wallCandidates.Where(t => null != map.GetThingOfDefAt(t, ResourceBank.HullDefs)).ToList();
			foreach (IntVec3 tile in wallCandidates)
			{
				IEnumerable<IntVec3> adjacentToWall = GenAdj.CellsAdjacentCardinal(tile, Rot4.North, new IntVec2(1, 1));
				foreach (IntVec3 turretTile in adjacentToWall)
				{
					if (mapComp.ShipIndexOnVec(turretTile) == -1)
					{
						SpawnTurretOnHardpoint(turretTile);
						Log.Message("SoS 2: Procgen turret spawned");
						return true;
					}
				}
			}
			// Fallback wave alghorithm
			IEnumerable<IntVec3> adjacentToAirlock = GenAdj.CellsAdjacentCardinal(airlock);
			foreach(IntVec3 fallbackRoot in adjacentToAirlock)
			{
				if(mapComp.ShipIndexOnVec(fallbackRoot) == -1)
				{
					IntVec3 turretTile = TryFindTurretTileNearWave(fallbackRoot);
					if (turretTile != IntVec3.Invalid)
					{
						SpawnTurretOnHardpoint(turretTile);
						Log.Message("SoS 2: Procgen turret spawned");
						return true;
					}
				}
			}
			Log.Message("SoS 2: Procgen turret location not found");
			return false;
		}
		private IntVec3 TryFindTurretTileNearWave(IntVec3 root)
		{
			// Decent nuber of steps to place turret near airlock
			const int steps = 18;
			HashSet<IntVec3> front = new HashSet<IntVec3>();
			front.Add(root);
			HashSet<IntVec3> visited = new HashSet<IntVec3>(front);
			for (int step = 0; step < steps; step++)
			{
				HashSet<IntVec3> nextFront = new HashSet<IntVec3>();
				foreach (IntVec3 cell in front)
				{
					foreach (IntVec3 adjacent in GenAdj.CellsAdjacentCardinal(cell, Rot4.North, IntVec2.One))
					{
						if (mapComp.ShipIndexOnVec(adjacent) == -1 && visited.Add(adjacent))
						{
							nextFront.Add(adjacent);
						}
					}
				}
				foreach (IntVec3 cell in nextFront)
				{
					if (IsAcceptableTurretTile(cell))
					{
						return cell;
					}
				}

				front = nextFront;
				if (front.Count == 0)
					break;
			}
			return IntVec3.Invalid;
		}
		private bool IsAcceptableTurretTile(IntVec3 tile)
		{
			if( mapComp.ShipIndexOnVec(tile) != -1)
			{
				return false;
			}
			List<IntVec3> neighbors = GenAdj.CellsAdjacentCardinal(tile, Rot4.North,IntVec2.One).ToList();

			bool hasAdjacentHull = false;
			foreach (IntVec3 adjacent in neighbors)
			{
				if(ContainsAirlock(adjacent))
				{
					return false;
				}
				hasAdjacentHull |= ContainsHull(adjacent);
			}
			return hasAdjacentHull;
		}
		private void SpawnTurretOnHardpoint(IntVec3 turretTile)
		{
			List<ThingDef> turretDefs = new List<ThingDef>() { ThingDefOf.Turret_MiniTurret };
			// Sheredder tech submod
			ThingDef shredderTurretDef = DefDatabase<ThingDef>.GetNamedSilentFail("EVA_Shredder_Turret_MiniTurret");
			if (shredderTurretDef != null)
			{
				turretDefs.Add(shredderTurretDef);
			}

			Thing hardpoint = ThingMaker.MakeThing(ResourceBank.ThingDefOf.ShipHardpointSmall);
			hardpoint.SetFactionDirect(shipFaction);
			GenSpawn.Spawn(hardpoint, turretTile, map);

			ThingDef turretDef = turretDefs.RandomElement();
			Thing turret;
			if (turretDef.MadeFromStuff)
			{
				ThingDef stuff = ship.Threat < ShipCatalog.CruiserCR ? GenStuff.DefaultStuffFor(ThingDefOf.Turret_MiniTurret)
					: ThingDefOf.Plasteel;
				turret = ThingMaker.MakeThing(turretDef, stuff);
			}
			else
			{
				turret = ThingMaker.MakeThing(turretDef);
			}
			turret.SetFactionDirect(shipFaction);
			GenSpawn.Spawn(turret, turretTile, map);
			
		}
		private bool ContainsAirlock(IntVec3 tile)
		{
			return ConatinsAny(tile, new List<ThingDef>()
			{
				ResourceBank.ThingDefOf.ShipAirlock,
				ResourceBank.ThingDefOf.ShipAirlockArchotech,
				ResourceBank.ThingDefOf.ShipAirlockMech,

			});
		}
		private bool ContainsHull(IntVec3 tile)
		{
			return ConatinsAny(tile, ResourceBank.HullDefs);
		}

		private bool ConatinsAny(IntVec3 tile, IEnumerable<ThingDef> thingDefs)
		{
			foreach (Thing t in map.thingGrid.ThingsAt(tile))
			{
				if(thingDefs.Contains(t.def))
				{
					return true;
				}
			}
			return false;
		}
	}
}

