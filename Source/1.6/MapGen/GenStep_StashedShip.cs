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

namespace SaveOurShip2
{
	class GenStep_StashedShip : GenStep_Scatterer
	{

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

		private bool IsCloseToCorner(IntVec3 cell, CellRect rect)
		{
			foreach (IntVec3 corner in rect.Corners)
			{
				// hypotenuse of a right triangle with 4 and 2 sides is considered close  
				if (cell.DistanceToSquared(corner) <= 4*4 + 2*2)
				{
					return true;
				}
			}
			return false;
		}
		protected override void ScatterAt(IntVec3 c, Map map, GenStepParams stepparams, int stackCount = 1)
		{
			MapParent mapParent = map.Parent;
			// Retrieve ship def name from quest.
			List<Quest> quests = Find.QuestManager.QuestsListForReading;
			string shipDefName = "";
			foreach (Quest quest in quests)
			{
				if (quest.QuestLookTargets.Contains(mapParent))
				{
					shipDefName = quest.tags.NullOrEmpty() ? "" : quest.tags.First();
				}
			}

			// Fallback for ship def name not found
			if (shipDefName.NullOrEmpty())
			{
				shipDefName = "FastScout";
			}

			List<Building> cores = new List<Building>();
			ShipDef ship = DefDatabase<ShipDef>.GetNamed(shipDefName, errorOnFail: false);
			int offsetX = (map.Size.x - ship.sizeX) / 2;
			int offsetZ = (map.Size.z - ship.sizeZ) / 2;
			CellRect cleanRect = new CellRect(offsetX, offsetZ, ship.sizeX, ship.sizeZ);
			cleanRect = cleanRect.ExpandedBy(9);
			cleanRect.ClipInsideMap(map);
			bool foundImpassable = false;
			foreach(IntVec3 cell in cleanRect.Cells)
			{
				if(!cell.Walkable(map))
				{
					foundImpassable = true;
				}
				CleanGeysers(map, cell);
			}
			
			if (foundImpassable)
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
					if (IsCloseToCorner(cell, cleanRect))
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
					if (IsCloseToCorner(cell, cleanRect))
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

			ShipInteriorMod2.GenerateShip(ship, map, null, Faction.OfPlayer, null, out cores, false, true,
				wreckLevel: 0, offsetX: offsetX, offsetZ: offsetZ);
		}
	}
}

