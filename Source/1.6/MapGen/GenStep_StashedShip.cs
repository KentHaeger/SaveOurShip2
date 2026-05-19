using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using RimWorld.BaseGen;
using RimWorld.QuestGen;
using Vehicles;

namespace SaveOurShip2
{
	class GenStep_StashedShip : GenStep_Scatterer
	{
		public const string ShipDefTagName = "shipDefName";
		public const string ThreatTagName = "threatPoints";
		public override int SeedPart
		{
			get
			{
				return 694201737;
			}
		}

		protected override bool CanScatterAt(IntVec3 c, Map map)
		{
			return true;
		}
		private void MakeTerrainPassable(Map map, IntVec3 cell)
		{
			if (map.terrainGrid.TerrainAt(cell).passability == Traversability.Impassable)
			{
				map.terrainGrid.SetTerrain(cell, ResourceBank.TerrainDefOf.Granite_Rough);
			}
		}

		private void CleanGeysers(Map map, IntVec3 cell)
		{
			foreach (Thing thing in cell.GetThingList(map).ToList())
			{
				if (thing.def == ThingDefOf.SteamGeyser)
				{
					thing.Destroy();
				}
			}
		}

		private void CleanChunks(Map map, IntVec3 cell)
		{
			foreach (Thing thing in cell.GetThingList(map).ToList())
			{
				if ((thing.def.thingCategories?.Contains(ThingCategoryDefOf.StoneChunks) ?? false) ||
					(thing.def.thingCategories?.Contains(ThingCategoryDefOf.Chunks) ?? false))
				{
					thing.Destroy();
				}
			}
		}

		private void CleanFilth(Map map, IntVec3 cell)
		{
			foreach (Thing thing in cell.GetThingList(map).ToList())
			{
				if (thing.def.category == ThingCategory.Filth)
				{
					thing.Destroy();
				}
			}
		}
		private void CleanThingsAt(Map map, IntVec3 cell)
		{
			foreach (Thing thing in cell.GetThingList(map).ToList())
			{
				if (thing.def.destroyable)
				{
					thing.Destroy();
				}
			}
		}

		private bool IsCloseToCornerRandomized(IntVec3 cell, CellRect rect)
		{
			const int maxOrthogonalDistance = 6;
			foreach (IntVec3 corner in rect.Corners)
			{
				if (Math.Abs(cell.x - corner.x) + Math.Abs(cell.z - corner.z) + Rand.RangeInclusive(-1, 1) <= maxOrthogonalDistance)
				{
					return true;
				}
			}
			return false;
		}

		private void SpawnDefendingMechanoids(Map map, int threatPoints, CellRect shipRect)
		{
			PawnGroupMakerParms parms = new PawnGroupMakerParms();
			parms.faction = Faction.OfMechanoids;
			parms.points = threatPoints;
			parms.tile = map.Tile;
			parms.groupKind = PawnGroupKindDefOf.Combat;

			IEnumerable<Pawn> pawns = PawnGroupMakerUtility.GeneratePawns(parms);

			CellRect expandedRect = shipRect.ExpandedBy(3);
			IntVec3 rootCell = expandedRect.EdgeCells.Where(c => c.Standable(map)).RandomElementWithFallback(IntVec3.Invalid);
			if (rootCell == IntVec3.Invalid)
			{
				return;
			}

			List<Pawn> spawnedPawns = new List<Pawn>();
			foreach (Pawn pawn in pawns)
			{
				if (!SiteGenStepUtility.TryFindSpawnCellAroundOrNear(shipRect, rootCell, map, out var spawnCell))
				{
					Find.WorldPawns.PassToWorld(pawn);
					break;
				}
				GenSpawn.Spawn(pawn, spawnCell, map);
				spawnedPawns.Add(pawn);
			}
			LordMaker.MakeNewLord(Faction.OfMechanoids, new LordJob_AssaultColony(Faction.OfMechanoids), map, spawnedPawns);
		}
		protected override void ScatterAt(IntVec3 c, Map map, GenStepParams stepparams, int stackCount = 1)
		{
			MapParent mapParent = map.Parent;
			// Retrieve ship def name from quest.
			List<Quest> quests = Find.QuestManager.QuestsListForReading;
			string shipDefName = "";
			int threatPoints = 0;
			foreach (Quest quest in quests)
			{
				if (quest.QuestLookTargets.Contains(mapParent))
				{
					string shipDefTag = quest.tags.Where(s => s.StartsWith(ShipDefTagName)).First();
					string threatTag = quest.tags.Where(s => s.StartsWith(ThreatTagName)).First();
					if (shipDefTag == null || threatTag == null)
					{
						// Fallback for quest version that was released without proper tags
						shipDefName = "FastScout";
					}
					else
					{
						shipDefName = shipDefTag.Split(':').Last() ?? "";
						string tagValue = threatTag.Split(':').Last() ?? threatPoints.ToString();
						if (!int.TryParse(tagValue, out threatPoints))
						{
							Log.Error("SOS 2: error parding threat tag for stashed ship:" + threatTag);
						}
					}
				}
			}

			// Fallback for ship def name not found
			if (DefDatabase<ShipDef>.GetNamedSilentFail(shipDefName) == null)
			{
				Log.Error($"SOS 2: Ship def name {shipDefName} not found for stashed ship quest. Default ship will be used");
				shipDefName = "FastScout";
			}

			List<Building> cores = new List<Building>();
			ShipDef ship = DefDatabase<ShipDef>.GetNamed(shipDefName, errorOnFail: false);
			int offsetX = (map.Size.x - ship.sizeX) / 2;
			int offsetZ = (map.Size.z - ship.sizeZ) / 2;
			CellRect cleanRect = new CellRect(offsetX, offsetZ, ship.sizeX, ship.sizeZ);
			cleanRect = cleanRect.ExpandedBy(9);
			cleanRect.ClipInsideMap(map);
			bool needFixPassabilityAndTerrain = false;
			foreach(IntVec3 cell in cleanRect.Cells)
			{
				if(!cell.Walkable(map))
				{
					needFixPassabilityAndTerrain = true;
				}
				CleanGeysers(map, cell);
				CleanChunks(map, cell);
				CleanFilth(map, cell);
			}
			
			if (needFixPassabilityAndTerrain)
			{
				TerrainDef fillerTerrain = ResourceBank.TerrainDefOf.Granite_Rough;
				foreach (IntVec3 cell in cleanRect.Cells)
				{
					TerrainDef terrain = map.terrainGrid.TerrainAt(cell);
					if (terrain == ResourceBank.TerrainDefOf.Granite_Rough || terrain == ResourceBank.TerrainDefOf.Slate_Rough ||
						terrain == ResourceBank.TerrainDefOf.Marble_Rough || terrain == ResourceBank.TerrainDefOf.Sandstone_Rough)
					{
						fillerTerrain = terrain;
						break;
					}
				}
				foreach (IntVec3 cell in cleanRect.Cells)
				{
					if (IsCloseToCornerRandomized(cell, cleanRect))
					{
						continue;
					}
					if (!cell.Walkable(map)) 
					{
						CleanThingsAt(map, cell);
						map.fogGrid.FloodUnfogAdjacent(cell);
					}
					if(map.terrainGrid.TerrainAt(cell).passability != Traversability.Standable)
					{
						map.terrainGrid.SetTerrain(cell, fillerTerrain);
					}
					map.roofGrid.SetRoof(cell, null);
				}
				// Give cleaned area more natural look by randomly cleaning mountains and other impassables around it
				cleanRect = cleanRect.ExpandedBy(1);
				cleanRect.ClipInsideMap(map);
				foreach (IntVec3 cell in cleanRect.EdgeCells)
				{
					if (IsCloseToCornerRandomized(cell, cleanRect))
					{
						continue;
					}
					if (Rand.Chance(0.5f))
					{
						if (!cell.Walkable(map))
						{
							CleanThingsAt(map, cell);
							map.fogGrid.FloodUnfogAdjacent(cell);
						}
					}
				}

			}

			ShipInteriorMod2.GenerateShip(ship, map, null, null, null, out cores, false, true,
				wreckLevel: 0, offsetX: offsetX, offsetZ: offsetZ);

			// Ship is set to null faction so that defenders actually fight player party, nut not start destroying ship.
			// But vehicles have to be set to player owned because there is no easy and standard way to claim them yet.
			foreach(Pawn p in map.mapPawns.AllPawns)
			{
				if(p is VehiclePawn vehicle)
				{
					if (SoS2VehicleUtility.IsSOS2Shuttle(vehicle))
					{
						vehicle.SetFaction(Faction.OfPlayer);
					}
				}				
			}

			SpawnDefendingMechanoids(map, threatPoints, new CellRect(offsetX, offsetZ, ship.sizeX, ship.sizeZ));
		}
	}
}

