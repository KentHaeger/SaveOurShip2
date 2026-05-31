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
		public const string FlipTagName = "flipShip";
		private const string fallbackShipDefName = "FastScout";
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
			Thing.allowDestroyNonDestroyable = true;
			try
			{
				foreach (Thing thing in cell.GetThingList(map).ToList())
				{
					if (thing.def == ThingDefOf.SteamGeyser)
					{
						thing.Destroy();
					}
				}
			}
			finally
			{
				Thing.allowDestroyNonDestroyable = false;
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

		private void CleanBuildingsAndUnfog(Map map, IntVec3 cell)
		{
			foreach (Thing thing in cell.GetThingList(map).ToList())
			{
				if (thing is Building)
				{
					thing.Destroy();
				}
			}
			map.fogGrid.FloodUnfogAdjacent(cell);
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
		private void CleanThingsAtAndUfog(Map map, IntVec3 cell)
		{
			foreach (Thing thing in cell.GetThingList(map).ToList())
			{
				if (thing.def.destroyable)
				{
					thing.Destroy();
				}
			}
			map.fogGrid.FloodUnfogAdjacent(cell);
		}
		// Special function for clearing ship landing ares, some tiles near corners will be excluiede, so that
		// cleared area looks more natural.
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

		private void SpawnDefendingForces(Map map, Faction faction, int threatPoints, CellRect shipRect)
		{
			if (threatPoints == 0)
			{
				return;
			}
			PawnGroupMakerParms parms = new PawnGroupMakerParms();
			parms.faction = faction != null ? faction : Faction.OfMechanoids;
			if (parms.faction == null)
			{
				Log.Error("SoS 2: No suitable faction found for ship defender forces. Mechanod faction shoul be in game in order to not harm SoS 2 experience");
				return;
			}
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
			LordMaker.MakeNewLord(parms.faction, new LordJob_AssaultColony(Faction.OfMechanoids), map, spawnedPawns);
		}

		// May return null, error will be handlled later
		private Faction GetDefendersFaction(Faction shipFaction)
		{
			Faction ancients = Faction.OfAncientsHostile;
			Faction bugs = Faction.OfInsects;
			// Intended to be truly random for testing purposes, not that much needed to lock players to specific
			// defender faction if they use save/load.
			Rand.PushState();
			Rand.Seed = Find.TickManager.TicksGame;
			float random = Rand.Value;
			Rand.PopState();
			// It is assumed that mech faction shouldn't be removed in order to not break SOS 2
			float mechanoidChance = 0.15f;
			float ancientsChance = ancients != null ? 0.15f : 0f;
			float bugsCahnce = bugs != null ? 0.1f : 0f;
			// Limit to to pre-set factions if no specific faction is given
			Log.Message($"random : {random}");
			if (shipFaction == null)
			{
				random *= mechanoidChance + ancientsChance + bugsCahnce;
			}
			if (random <= mechanoidChance)
			{
				return Find.FactionManager.FirstFactionOfDef(FactionDefOf.Mechanoid);
			}
			else if (random <= mechanoidChance + ancientsChance)
			{
				return ancients;
			}
			else if (random <= mechanoidChance + ancientsChance + bugsCahnce)
			{
				return bugs;
			}
			else
			{
				return shipFaction; 
			}
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
						shipDefName = fallbackShipDefName;
					}
					else
					{
						shipDefName = shipDefTag.Split(':').Last() ?? "";
						string tagValue = threatTag.Split(':').Last() ?? threatPoints.ToString();
						if (!int.TryParse(tagValue, out threatPoints))
						{
							Log.Error("SoS 2: error parsing threat tag for stashed ship:" + threatTag);
							threatPoints = 0;
						}
					}
				}
			}

			// Fallback for ship def name not found
			if (DefDatabase<ShipDef>.GetNamedSilentFail(shipDefName) == null)
			{
				Log.Error($"SoS 2: Ship def name {shipDefName} not found for stashed ship quest. Default ship will be used");
				shipDefName = fallbackShipDefName;
				if( DefDatabase<ShipDef>.GetNamedSilentFail(shipDefName) == null)
				{
					Log.Error("SoS 2: coulnd't find default ship def for stashed ship in def database");
					return;
				}
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
				CleanBuildingsAndUnfog(map, cell);
				CleanFilth(map, cell);
			}
			
			if (needFixPassabilityAndTerrain)
			{
				TerrainDef fillerTerrain = null;
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
				if (fillerTerrain == null)
				{
					ThingDef rockType = Find.World.NaturalRockTypesIn(map.Tile).RandomElementWithFallback();
					fillerTerrain = ResourceBank.GetTerrainFromRockType(rockType);
				}
				// Final fallback for terrain
				if (fillerTerrain == null)
				{
					fillerTerrain = ResourceBank.TerrainDefOf.Granite_Rough;
				}

				foreach (IntVec3 cell in cleanRect.Cells)
				{
					if (IsCloseToCornerRandomized(cell, cleanRect))
					{
						continue;
					}
					if (!cell.Walkable(map)) 
					{
						CleanThingsAtAndUfog(map, cell);
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
							CleanThingsAtAndUfog(map, cell);
							map.fogGrid.FloodUnfogAdjacent(cell);
						}
					}
				}

			}

			try
			{
				ShipInteriorMod2.GenerateShip(ship, map, null, null, null, out cores, false, true,
					wreckLevel: 0, offsetX: offsetX, offsetZ: offsetZ, flipShip: true, preventPawnSpawn: true);
			}
			catch (Exception ex)
			{
				Log.Error("SOS 2: Error during stashed ship generation:" + ex.Message);
			}

			// Turrets can be claimed by Vanilla rules, so prevent them from firing soon after arrival,
			// not allowing stashed ship to defeat defenders on it's own.
			foreach (Building b in map.listerBuildings.allBuildingsColonist.ToList().Concat(map.listerBuildings.allBuildingsNonColonist))
			{
				if (b is Building_ShipTurret turret)
				{
					turret.burstCooldownTicksLeft = GenDate.TicksPerHour * 3;
				}
			}

			// Ship is set to null faction so that defenders actually fight player party, nut not start destroying ship.
			// But vehicles have to be set to player owned because there is no standard way to claim them yet.
			foreach (Pawn p in map.mapPawns.AllPawns)
			{
				if(p is VehiclePawn vehicle)
				{
					if (SoS2VehicleUtility.IsSOS2Shuttle(vehicle))
					{
						vehicle.SetFaction(Faction.OfPlayer);
					}
				}				
			}

			Faction shipFaction = ship.GetHostileFactionFromCrew();
			Faction defendersFaction = GetDefendersFaction(shipFaction);

			Log.Message($"SoS 2: Ship defenders faction: {defendersFaction.Name}");

			SpawnDefendingForces(map, defendersFaction, threatPoints, new CellRect(offsetX, offsetZ, ship.sizeX, ship.sizeZ));
		}
	}
}

