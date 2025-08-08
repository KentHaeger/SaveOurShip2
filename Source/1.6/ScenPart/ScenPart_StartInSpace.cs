using RimWorld.Planet;

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace SaveOurShip2
{
	[Flags]
	public enum ShipStartFlags
	{
		None = 0,
		Ship = 1,
		Station = 2,
		LoadShip = 4 //if this can be merged?
	}
	class ScenPart_StartInSpace : ScenPart
	{

		//ship selection - not sure how much of this is actually needed for this to work, also a bit convoluted random option
		public ShipDef spaceShipDef;
		public bool damageStart;
		public ShipStartFlags startType;
		public override bool CanCoexistWith(ScenPart other) //not working in menu
		{
			return !(other is ScenPart_AfterlifeVault || other is ScenPart_LoadShip);
		}
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Defs.Look<ShipDef>(ref spaceShipDef, "spaceShipDef");
			Scribe_Values.Look<bool>(ref damageStart, "damageStart");
			Scribe_Values.Look<ShipStartFlags>(ref startType, "startType");
		}

		// These ships/stations were manually picked as not interesting (not actual combat ship)/not intended to start with.
		// To make ship list easier to use, it's huge
		private static readonly string excludedShipsString = "AfterlifeVaultStart,AbandonedMiningStation,ContainerFurniture,ContainerLoot,ContainerMech,ContainerSecure," +
			"ContainerSecurity,ContainerTV,DefenseInstallation,DefenseInstallation4,DefenseInstallation6,MechanoidMoonBase,SatelliteLarge2," +
			"SatelliteLarge2Eng,SatelliteLarge2Eng2,SatelliteLarge3Eng,SatelliteLarge3Eng2,SatelliteLarge4,SatelliteSmall2,SatelliteSmall2Eng," +
			"SatelliteSmall2Eng2,SatelliteSmall3,SatelliteSmall3Eng,SatelliteSmall3Eng2,SatelliteSmall4,SmallSatellite,StarshipBowDungeon,StartSiteAsteroidA," +
			"StartSiteAsteroidB,StartSiteAsteroidC,StartSiteAsteroidD,StartSiteAsteroidE,StartSiteAsteroidMech,StartSiteEmpire,StartSiteMoonA,StartSiteMoonB," +
			"StartSiteShipyard,StartSiteSkylab,StationAgri01,StationAgri01D,StationAgri02,StationAgri03,StationAgri03D,StationArchotechGarden,StationPrison01," +
			"StationPrison02,TribalVillageIsNotAShip";
		private static List<string> excludedShipDefs = excludedShipsString.Split(',').ToList();

		private bool ShipUnlockedAsStartup(ShipDef def, ShipStartFlags startType)
		{
			// Unlocks only apply to start on ship
			if (GlobalUnlockDef.AllShipsUnlocked() && startType == ShipStartFlags.Ship)
			{
				return !excludedShipDefs.Contains(def.defName);
			}
			else
			{
				return (startType == ShipStartFlags.Ship && def.startingShip == true && def.startingDungeon == false) ||
					(startType == ShipStartFlags.Station && def.startingShip == true && def.startingDungeon == true);
			}
		}
		public override void DoEditInterface(Listing_ScenEdit listing)
		{
			Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight * 3f);
			Rect rect1 = new Rect(scenPartRect.x, scenPartRect.y, scenPartRect.width, scenPartRect.height / 3f);
			Rect rect2 = new Rect(scenPartRect.x, scenPartRect.y + scenPartRect.height / 3f, scenPartRect.width, scenPartRect.height / 3f);
			Rect rect3 = new Rect(scenPartRect.x, scenPartRect.y + 2 * scenPartRect.height / 3f, scenPartRect.width, scenPartRect.height / 3f);
			//selection 1
			if (Widgets.ButtonText(rect1, TranslatorFormattedStringExtensions.Translate("SoS.SpaceStart.StartOn", startType.ToString()), true, true, true))
			{
				List<FloatMenuOption> toggleType = new List<FloatMenuOption>
				{
					new FloatMenuOption(TranslatorFormattedStringExtensions.Translate("SoS.SpaceStart.StartOn", ShipStartFlags.Ship.ToString()), delegate ()
					{
						startType = ShipStartFlags.Ship;
						spaceShipDef = DefDatabase<ShipDef>.GetNamed("0");
					}, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
					new FloatMenuOption(TranslatorFormattedStringExtensions.Translate("SoS.SpaceStart.StartOn", ShipStartFlags.Station.ToString()), delegate ()
					{
						startType = ShipStartFlags.Station;
						spaceShipDef = DefDatabase<ShipDef>.GetNamed("0");
					}, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0)
				};
				Find.WindowStack.Add(new FloatMenu(toggleType));

			}
			//selection 2
			if (Widgets.ButtonText(rect2, spaceShipDef.label, true, true, true))
			{
				List<FloatMenuOption> list = new List<FloatMenuOption>();
				foreach (ShipDef localTd2 in DefDatabase<ShipDef>.AllDefs.Where(t => t.defName == "0" || ShipUnlockedAsStartup(t, startType)).OrderBy(t => t.defName))
				{
					ShipDef localTd = localTd2;
					string CRString = "";
					if (GlobalUnlockDef.AllShipsUnlocked())
					{
						// 00A0 is non-breakable space
						// This is not main SOS2 content, so no translation fon now
						CRString = " CR:\u00A0" + localTd2.combatPoints;
					}
					list.Add(new FloatMenuOption(localTd.label + " (" + localTd.defName + ")" + CRString, delegate ()
					{
						spaceShipDef = localTd;
					}, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
				}
				Find.WindowStack.Add(new FloatMenu(list));
			}
			//selection 3
			if (startType == ShipStartFlags.Ship && Widgets.ButtonText(rect3, TranslatorFormattedStringExtensions.Translate(
				"SoS.SpaceStart.DamageShip", damageStart.ToString()), true, true, true))
			{
				List<FloatMenuOption> toggleDamage = new List<FloatMenuOption>();
				toggleDamage.Add(new FloatMenuOption(TranslatorFormattedStringExtensions.Translate("SoS.SpaceStart.DamageShip", true.ToString()), delegate ()
				{
					damageStart = true;
				}, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
				toggleDamage.Add(new FloatMenuOption(TranslatorFormattedStringExtensions.Translate("SoS.SpaceStart.DamageShip", false.ToString()), delegate ()
				{
					damageStart = false;
				}, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
				Find.WindowStack.Add(new FloatMenu(toggleDamage));

			}
		}
		public override string Summary(Scenario scen)
		{
			return "ScenPart_PlayerFaction".Translate(spaceShipDef.label); // Core\ScenParts.xml
		}

		public override void Randomize()
		{
			spaceShipDef = DefDatabase<ShipDef>.AllDefs.Where(def => def.startingShip == true && def.startingDungeon == true).RandomElement();
		}
		public override bool HasNullDefs()
		{
			return base.HasNullDefs() || spaceShipDef == null;
		}
		public override IEnumerable<string> ConfigErrors()
		{
			if (spaceShipDef == null)
			{
				yield return "thingDef is null";
			}
			yield break;
		}
		//ship selection end

		public void DoEarlyInit() //Scenario.GetFirstConfigPage call via patch 
		{
			ShipInteriorMod2.StartShipFlag = true;
		}

		public static Map GenerateShipSpaceMap() //MapGenerator.GenerateMap override via patch
		{
			ScenPart_StartInSpace scen = (ScenPart_StartInSpace)Current.Game.Scenario.parts.FirstOrDefault(s => s is ScenPart_StartInSpace);

			if (scen.startType == ShipStartFlags.Station && scen.spaceShipDef.defName == "0") //random dungeon
			{
				scen.spaceShipDef = DefDatabase<ShipDef>.AllDefs.Where(def => def.startingShip == true && def.startingDungeon == true).RandomElement();
				scen.damageStart = false;
			}
			else if (scen.spaceShipDef.defName == "0") //random ship, damage lvl 1
			{
				scen.spaceShipDef = DefDatabase<ShipDef>.AllDefs.Where(def => def.startingShip == true && def.startingDungeon == false && def.defName != "0").RandomElement();
			}

			IntVec3 mapSize = new IntVec3(Find.GameInitData.mapSize, 1, Find.GameInitData.mapSize);
			// As moon base starts are whole 250x250 maps saved, they got to have map size locked for now to avoid having empty space around
			if (mapSize.x < 250 || mapSize.z < 250 || scen.spaceShipDef.defName == "StartSiteMoonA" || scen.spaceShipDef.defName == "StartSiteMoonB") 
				mapSize = new IntVec3(250, 1, 250);

			Map spaceMap = ShipInteriorMod2.GeneratePlayerShipMap(mapSize);
			Current.ProgramState = ProgramState.MapInitializing;

			List<Building> cores = new List<Building>();
			ShipInteriorMod2.GenerateShip(scen.spaceShipDef, spaceMap, null, Faction.OfPlayer, null, out cores, false, false, scen.damageStart ? 1 : 0, (spaceMap.Size.x - scen.spaceShipDef.sizeX) / 2, (spaceMap.Size.z - scen.spaceShipDef.sizeZ) / 2);

			Current.ProgramState = ProgramState.Playing;
			IntVec2 secs = spaceMap.mapDrawer.SectionCount;
			Section[,] secArray = new Section[secs.x, secs.z];
			spaceMap.mapDrawer.sections = secArray;
			for (int i = 0; i < secs.x; i++)
			{
				for (int j = 0; j < secs.z; j++)
				{
					if (secArray[i, j] == null)
					{
						secArray[i, j] = new Section(new IntVec3(i, 0, j), spaceMap);
					}
				}
			}

			CameraJumper.TryJump(spaceMap.Center, spaceMap);
			spaceMap.weatherManager.curWeather = ResourceBank.WeatherDefOf.OuterSpaceWeather;
			spaceMap.weatherManager.lastWeather = ResourceBank.WeatherDefOf.OuterSpaceWeather;
			spaceMap.Parent.SetFaction(Faction.OfPlayer);
			Find.MapUI.Notify_SwitchedMap();
			spaceMap.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
			foreach (Room r in spaceMap.regionGrid.allRooms)
				r.Temperature = 21;
			try //do post game start?
			{
				if (scen.startType == ShipStartFlags.Ship) //defog and homezone ships
				{
					spaceMap.fogGrid.ClearAllFog();
					foreach (Building b in spaceMap.listerThings.AllThings.Where(t => t is Building))
					{
						foreach (IntVec3 v in b.OccupiedRect())
						{
							spaceMap.areaManager.Home[v] = true;
						}
						// Fix to add ship buildings to listerBuildings* lists
						// This is not allowed as early as during ship creation
						if (b.Faction == Faction.OfPlayer)
						{
							b.SetFaction(Faction.OfPlayer);
						}
					}
				}
			}
			catch (Exception e)
			{
				Log.Warning(e.Message + "\n" + e.StackTrace);
			}
			AccessExtensions.Utility.RecacheSpaceMaps();
			return spaceMap;
		}
		public override void PostGameStart() //spawn pawns, things
		{
			ScenPart_StartInSpace scen = (ScenPart_StartInSpace)Current.Game.Scenario.parts.FirstOrDefault(s => s is ScenPart_StartInSpace);
			Map spaceMap = Find.CurrentMap;
			List<List<Thing>> list = new List<List<Thing>>();
			List<Thing> list3 = new List<Thing>();
			foreach (Pawn startingAndOptionalPawn in Find.GameInitData.startingAndOptionalPawns)
			{
				List<Thing> list2 = new List<Thing>{ startingAndOptionalPawn };
				list.Add(list2);
				foreach (ThingDefCount posession in Find.GameInitData.startingPossessions[startingAndOptionalPawn])
				{
					Thing thing = ThingMaker.MakeThing(posession.ThingDef, GenStuff.RandomStuffFor(posession.ThingDef));
					if (posession.ThingDef.IsIngestible && posession.ThingDef.ingestible.IsMeal)
					{
						FoodUtility.GenerateGoodIngredients(thing, Faction.OfPlayer.ideos.PrimaryIdeo);
					}
					if (thing.def.Minifiable)
					{
						thing = thing.MakeMinified();
					}
					thing.stackCount = posession.Count;
					list3.Add(thing);
				}
			}
			
			foreach (ScenPart allPart in Find.Scenario.AllParts)
			{
				list3.AddRange(allPart.PlayerStartingThings());
			}
			int num = 0;
			foreach (Thing item in list3)
			{
				if (!(item is Pawn) || ((item is Pawn p) && (p.RaceProps?.IsMechanoid ?? false)))
				{
					if (item.def.CanHaveFaction)
					{
						item.SetFactionDirect(Faction.OfPlayer);
					}
					list[num].Add(item);
					num++;
					if (num >= list.Count)
					{
						num = 0;
					}
				}
			}
			List<Building> spawners = new List<Building>();
			List<IntVec3> spawnPos = GetSpawnCells(spaceMap, out spawners);
			foreach (List<Thing> thingies in list)
			{
				IntVec3 nextPos = spaceMap.Center;
				nextPos = spawnPos.RandomElement();
				spawnPos.Remove(nextPos);
				if (spawnPos.Count == 0)
					spawnPos = GetSpawnCells(spaceMap, out spawners); //reuse spawns

				foreach (Thing thingy in thingies)
				{
					thingy.SetForbidden(true, false);
					GenPlace.TryPlaceThing(thingy, nextPos, spaceMap, ThingPlaceMode.Near);
				}
				if (scen.startType == ShipStartFlags.Station)
					FloodFillerFog.FloodUnfog(nextPos, spaceMap);
			}
			foreach (Building b in spawners.Where(b => !b.Destroyed)) //remove spawn points
			{
				b.Destroy();
			}
			spawners.Clear();
			spawnPos.Clear();
			if (spaceShipDef.defName == "StartSiteAsteroidMech")
			{
				AssignMechAIForMechanoidBase();
			}
		}

		private void AssignMechAIForMechanoidBase()
		{
			// Divide map into parts, assign mech in each part a separate lord, so that they don't aggro all at once.
			Map map = Find.CurrentMap;
			int partCount = 6;
			int mapWidth = map.Size.x;
			int mapHeight = map.Size.z;
			int regionWidth = mapWidth / partCount;
			int regionHeight = mapHeight / partCount;
			List<Pawn> allEnemyMechs = map.mapPawns.AllPawnsSpawned.Where(p => p.GetLord() == null && p.Faction.HostileTo(Faction.OfPlayer)).ToList();
			if (!allEnemyMechs.Any())
			{
				Log.Warning("Detected asteroid mech base start with no mechs");
				return;
			}
			Faction mechFaction = allEnemyMechs.First().Faction;
			for (int xPart = 0; xPart < partCount; xPart++)
			{
				for (int zPart = 0; zPart < partCount; zPart++)
				{
					CellRect part = new CellRect();
					part.minX = xPart * mapWidth / partCount;
					part.maxX = (xPart + 1) * mapWidth / partCount - 1;
					part.minZ = zPart * mapHeight / partCount;
					part.maxZ = (zPart + 1) * mapHeight / partCount - 1;
					IEnumerable<Pawn> mechsInRegion = allEnemyMechs.Where(p => part.Contains(p.Position));
					Lord groupLord;
					if (mechsInRegion.Any())
					{
						groupLord = LordMaker.MakeNewLord(mechFaction, new LordJob_DefendShip(mechFaction, part.CenterCell), Find.CurrentMap);
						foreach (Pawn p in mechsInRegion)
						{
							groupLord.AddPawn(p);
						}
					}
				}
			}
		}

		static List<IntVec3> GetSpawnCells(Map spaceMap, out List<Building> spawners) //spawn placer > crypto > salvbay > bridge
		{
			spawners = new List<Building>();
			List<IntVec3> spawncells = new List<IntVec3>();
			foreach (Building b in spaceMap.listerThings.AllThings.Where(b => b is Building && b.def.defName.Equals("PawnSpawnerStart")))
			{
				spawncells.Add(b.Position);
				spawners.Add(b);
			}
			if (spawncells.Any())
			{
				return spawncells;
			}
			//backups
			List<IntVec3> salvBayCells = new List<IntVec3>();
			List<IntVec3> bridgeCells = new List<IntVec3>();
			foreach (Building b in spaceMap.listerThings.AllThings.Where(b => b is Building))
			{
				if (b is Building_CryptosleepCasket c && !c.HasAnyContents)
				{
					spawncells.Add(b.InteractionCell);
				}
				else if (b.TryGetComp<CompShipBaySalvage>() != null)
				{
					salvBayCells.AddRange(b.OccupiedRect().ToList());
				}
				else if (b is Building_ShipBridge && b.def.hasInteractionCell && b.GetRoom() != null)
				{
					bridgeCells.AddRange(b.GetRoom().Cells.ToList());
				}
			}
			if (spawncells.Any())
				return spawncells;
			else if (salvBayCells.Any())
				return salvBayCells;
			else if (bridgeCells.Any())
				return bridgeCells;
			spawncells.Add(spaceMap.Center);
			return spawncells;
		}
	}
}
