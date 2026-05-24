using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using HarmonyLib;

// As Harmony patches are hard to write, it would be handy to keep inactive/obsolete patches in source files,
// available for search, as a reference.

namespace SaveOurShip2
{
	// This is for fast mode
	/*[HarmonyPatch(typeof(SteadyEnvironmentEffects), "SteadyEnvironmentEffectsTick")]
    public static class FastModeSteadyEnvironmentEffects
    {
        public static bool Prefix(SteadyEnvironmentEffects __instance)
        {
			return !__instance.map.IsSpace();
        }
    }*/

	/*
    [HarmonyPatch(typeof(GenThing), "TrueCenter", new Type[] { typeof(Thing) })]
    [HarmonyPriority(10)]
    public static class FastTrueCenter
    {
		[HarmonyPriority(10)]
		public static bool Prefix(Thing t, ref Vector3 __result)
        {
            if (t.def.category == ThingCategory.Item && t.Spawned)
            {
                __result = GenThing.ItemCenterAt(t);
                return false;
            }
            __result = GenThing.TrueCenter(t.Position, t.Rotation, t.def.size, t.def.Altitude);
            return false;
        }
    }
	*/

	/*[HarmonyPatch(typeof(Patch_Rendering), "TrueCenterVehicle")]
    public static class FastTrueCenter
	{
		public static bool Prefix(Thing t, ref Vector3 __result)
		{
            if (t.def.category == ThingCategory.Item && t.Spawned)
            {
                __result = GenThing.ItemCenterAt(t);
				return false;
            }
            __result = GenThing.TrueCenter(t.Position, t.Rotation, t.def.size, t.def.Altitude);
            return false;
        }
	}*/

	/*[HarmonyPatch(typeof(ActiveDropPod),"PodOpen")]
	public static class ActivePodFix{
		public static bool Prefix (ref ActiveDropPod __instance)
		{
			if(__instance.def.defName.Equals("ActiveShuttle"))
			{
				ThingOwner stuffInPod = ((ActiveDropPodInfo)typeof(ActiveDropPod).GetField ("contents", BindingFlags.Instance | BindingFlags.NonPublic).GetValue (__instance)).innerContainer;
				Pawn shuttleLanded = null;
				List<Thing> fillTheShuttle = new List<Thing> ();
				for (int i = stuffInPod.Count - 1; i >= 0; i--)
				{
					Thing thing = stuffInPod[i];
					if (thing is Pawn) {
						Pawn pawn = (Pawn)thing;
						GenPlace.TryPlaceThing (thing, __instance.Position, __instance.Map, ThingPlaceMode.Near);
						if (thing.TryGetComp<CompBecomeBuilding> () != null)
							shuttleLanded = pawn;
						if (pawn.RaceProps.Humanlike) {
							TaleRecorder.RecordTale (TaleDefOf.LandedInPod, new object[] {
								pawn
							});
						}
						if (pawn.IsColonist && pawn.Spawned && !__instance.Map.IsPlayerHome) {
							pawn.drafter.Drafted = true;
						}
					} else
						fillTheShuttle.Add (thing);
				}
				if (shuttleLanded != null) {
					ThingOwner shuttleInventory = shuttleLanded.inventory.innerContainer;
					foreach (Thing thing in fillTheShuttle) {
						stuffInPod.Remove (thing);
						shuttleInventory.TryAdd (thing);
					}
				}
				stuffInPod.ClearAndDestroyContents(DestroyMode.Vanish);
				SoundDef.Named("DropPodOpen").PlayOneShot(new TargetInfo(__instance.Position, __instance.Map, false));
				__instance.Destroy(DestroyMode.Vanish);
				return false;
			}
			return true;
		}
	}*/
	/*[HarmonyPatch(typeof(Pawn))]
	[HarmonyPatch("IsColonist",MethodType.Getter)]
	public static class GizmoFix{
		public static void Postfix(Pawn __instance, ref bool __result)
		{
			if (__instance.TryGetComp<CompBecomeBuilding> () != null && !System.Environment.StackTrace.Contains("AllMapsCaravansAndTravelingTransportPods_Colonists")) {
				__result=true;
				if (__instance.drafter == null) {
					__instance.drafter = new Pawn_DraftController (__instance);
				}
				if (__instance.equipment == null) {
					__instance.equipment = new Pawn_EquipmentTracker (__instance);
				}
			}
		}
	}*/

	/*No longer necessary in 1.4
	[HarmonyPatch(typeof(Pawn), "GetGizmos")]
	public static class AnimalsHaveGizmosToo
	{
		public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
		{
			if (__instance.TryGetComp<CompArcholife>() != null)
			{
				List<Gizmo> giz = new List<Gizmo>();
				giz.AddRange(__result);
				giz.AddRange(__instance.TryGetComp<CompArcholife>().CompGetGizmosExtra());
				__result = giz;
			}
		}
	}*/
	/*[HarmonyPatch(typeof(TileFinder), "TryFindNewSiteTile")] //changed destructive patch, unsure if this is even needed anymore
	public static class NoQuestsNearTileZero
	{
		public static bool Prefix(out int tile, int minDist, int maxDist, bool allowCaravans,
			TileFinderMode tileFinderMode, int nearThisTile, ref bool __result)
		{
			tile = -1;
			if (ShipInteriorMod2.FindPlayerShipMap() == null)
				return true;

			Func<int, int> findTile = delegate (int root) {
				int minDist2 = minDist;
				int maxDist2 = maxDist;
				Predicate<int> validator = (int x) =>
					!Find.WorldObjects.AnyWorldObjectAt(x) && TileFinder.IsValidTileForNewSettlement(x, null);
				int result;
				if (TileFinder.TryFindPassableTileWithTraversalDistance(root, minDist2, maxDist2, out result,
					validator: validator, ignoreFirstTilePassability: false, tileFinderMode, false))
				{
					return result;
				}

				return -1;
			};
			int arg;
			if (nearThisTile != -1)
			{
				arg = nearThisTile;
			}
			else if (!TileFinder.TryFindRandomPlayerTile(out arg, allowCaravans,
				(int x) => findTile(x) != -1 && (Find.World.worldObjects.MapParentAt(x) == null ||
													!(Find.World.worldObjects.MapParentAt(x) is WorldObjectOrbitingShip))))
			{
				tile = -1;
				__result = false;
				return false;
			}

			tile = findTile(arg);
			__result = (tile != -1);
			return false;
		}
	}*/

	/*[HarmonyPatch(typeof(CompShipPart),"PostSpawnSetup")]
	public static class RemoveVacuum{
		public static void Postfix (CompShipPart __instance)
		{
			if (__instance.parent.Map.terrainGrid.TerrainAt (__instance.parent.Position).defName.Equals ("EmptySpace"))
				__instance.parent.Map.terrainGrid.SetTerrain (__instance.parent.Position,TerrainDef.Named("FakeFloorInsideShip"));
		}
	}*/
	/*[HarmonyPatch(typeof(GenConstruct), "BlocksConstruction")]
	public static class HullTilesDontWipe
	{
		public static void Postfix(Thing constructible, Thing t, ref bool __result)
		{
			if (constructible.def.defName.Contains("ShipHullTile") ^ t.def.defName.Contains("ShipHullTile"))
				__result = false;
		}
	}

	[HarmonyPatch(typeof(TravelingTransportPods))]
	[HarmonyPatch("TraveledPctStepPerTick", MethodType.Getter)]
	public static class InstantShuttleArrival
	{
		public static void Postfix(int ___initialTile, TravelingTransportPods __instance, ref float __result)
		{
			if (Find.TickManager.TicksGame % 60 == 0)
			{
				var mapComp = Find.WorldObjects.MapParentAt(___initialTile).Map.GetComponent<ShipHeatMapComp>();
				if ((mapComp.InCombat && (__instance.destinationTile == mapComp.ShipCombatOriginMap.Tile ||
					__instance.destinationTile == mapComp.ShipCombatMasterMap.Tile)) || 
					__instance.arrivalAction is TransportPodsArrivalAction_MoonBase)
				{
					__result = 1f;
				}
			}

		}
	}*/

	//Space crib - disabled, good transpiler example
	/*[HarmonyPatch(typeof(GenTemperature), "TryGetTemperatureForCell")]
	public static class BabiesAreSafeInSpaceCaskets
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var editor = new CodeMatcher(instructions);
			// --------------------------ORIGINAL--------------------------
			//for (int i = 0; i < list.Count; i++)
			//{
			//if (list[i].def.passability == Traversability.Impassable)
			editor.Start().MatchStartForward(
				new CodeMatch(OpCodes.Ldloc_0),
				new CodeMatch(OpCodes.Ldloc_1),
				new CodeMatch(OpCodes.Callvirt),
				//Jump point...
				new CodeMatch(OpCodes.Ldfld),
				new CodeMatch(OpCodes.Ldfld),
				new CodeMatch(OpCodes.Ldc_I4_2),
				new CodeMatch(OpCodes.Bne_Un_S)
			);

			var thing = generator.DeclareLocal(typeof(Thing)); //Store the list[i] into here
			var label = generator.DefineLabel(); //Prepare a new label
			var codeWithLabel = new CodeInstruction(OpCodes.Ldloc_S, thing); //This will be injected into the "Jump point" above.
			codeWithLabel.labels.Add(label); //Record its label position for the return to go to.

			if (!editor.IsInvalid)
			{
				// --------------------------MODIFIED--------------------------
				//for (int i = 0; i < list.Count; i++)
				//{
				//var item = list[i];
				//if (AdjustTemperatureForCrib(item, ref tempResult) return true;)
				//if (item.def.passability == Traversability.Impassable)
				return editor
				.Advance(3)
				.InsertAndAdvance(new CodeInstruction(OpCodes.Stloc_S, thing)) //Store the thing as a new variable
				.InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, thing)) //thing
				.InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_2)) //float tempResult
				.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BabiesAreSafeInSpaceCaskets), nameof(AdjustTemperatureForCrib))))
				.InsertAndAdvance(new CodeInstruction(OpCodes.Brfalse_S, label)) //If it's false, move onto the next part of the loop like normal
				.InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_1)) //Otherwise push a true and return
				.InsertAndAdvance(new CodeInstruction(OpCodes.Ret))
				.Insert(codeWithLabel)
				.InstructionEnumeration();
			}

			Log.Error("[SoS2] BabiesAreSafeInSpaceCaskets transpiler failed to find its target. Did RimWorld update?");
			return editor.InstructionEnumeration();	
		}

		public static bool AdjustTemperatureForCrib(Thing thing, ref float tempResult)
		{
			if (thing is Building_SpaceCrib)
			{
				tempResult = 21f;
				return true;
			}
			return false;
		}
	}*/

	// explosion patch disabled till fixed
	/*[HarmonyPatch(typeof(DamageWorker))]
	[HarmonyPatch("ExplosionCellsToHit", new Type[] { typeof(IntVec3), typeof(Map), typeof(float), typeof(IntVec3), typeof(IntVec3) })]
	public static class FasterExplosions
	{
		public static bool Prefix(Map map, float radius)
		{
			return !map.GetComponent<ShipHeatMapComp>().InCombat || radius > 25; //Ludicrously large explosions cause a stack overflow
		}

		public static void Postfix(ref IEnumerable<IntVec3> __result, DamageWorker __instance, IntVec3 center, Map map, float radius)
		{
			if (map.GetComponent<ShipHeatMapComp>().InCombat && radius <= 25)
			{
				HashSet<IntVec3> cells = new HashSet<IntVec3>();
				List<ExplosionCell> cellsToRun = new List<ExplosionCell>();
				cellsToRun.Add(new ExplosionCell(center, new bool[4], 0));
				ExplosionCell curCell;
				while (cellsToRun.Count > 0)
				{
					curCell = cellsToRun.Pop();
					cells.Add(curCell.pos);
					if (curCell.dist <= radius)
					{
						Building edifice = null;
						if (curCell.pos.InBounds(map))
							edifice = curCell.pos.GetEdifice(map);
						if (edifice != null && edifice.HitPoints >= __instance.def.defaultDamage / 2)
							continue;
						if (!curCell.checkedDir[0]) //up
						{
							bool[] newDir = (bool[])curCell.checkedDir.Clone();
							newDir[1] = true;
							cellsToRun.Add(new ExplosionCell(curCell.pos + new IntVec3(0, 0, 1), newDir, curCell.dist + 1));
						}
						if (!curCell.checkedDir[1]) //down
						{
							bool[] newDir = (bool[])curCell.checkedDir.Clone();
							newDir[0] = true;
							cellsToRun.Add(new ExplosionCell(curCell.pos + new IntVec3(0, 0, -1), newDir, curCell.dist + 1));
						}
						if (!curCell.checkedDir[2]) //right
						{
							bool[] newDir = (bool[])curCell.checkedDir.Clone();
							newDir[3] = true;
							cellsToRun.Add(new ExplosionCell(curCell.pos + new IntVec3(1, 0, 0), newDir, curCell.dist + 1));
						}
						if (!curCell.checkedDir[3]) //left
						{
							bool[] newDir = (bool[])curCell.checkedDir.Clone();
							newDir[2] = true;
							cellsToRun.Add(new ExplosionCell(curCell.pos + new IntVec3(-1, 0, 0), newDir, curCell.dist + 1));
						}
					}
				}
				__result = cells;
			}
		}

		public struct ExplosionCell
		{
			public IntVec3 pos;
			public bool[] checkedDir;
			public int dist;

			public ExplosionCell(IntVec3 myPos, bool[] myCheckedDir, int myDist)
			{
				checkedDir = myCheckedDir;
				pos = myPos;
				dist = myDist;
			}
		}
	}
	*/
	/*[HarmonyPatch(typeof(Building), "Destroy")] //obs by newcache
	public static class NotifyCombatManager
	{
		public static bool Prefix(Building __instance, DestroyMode mode, out Tuple<IntVec3, Faction, Map> __state)
		{
			__state = null;
			//only print or foam if destroyed normally
			if (!(mode == DestroyMode.KillFinalize || mode == DestroyMode.KillFinalizeLeavingsOnly))
				return true;
			if (!__instance.def.CanHaveFaction || __instance is Frame)
				return true;
			var mapComp = __instance.Map.GetComponent<ShipHeatMapComp>();
			int shipIndex = mapComp.ShipIndexOnVec(__instance.Position);
			if (shipIndex != -1) //is this on a ship
			{
				var shipPart = __instance.TryGetComp<CompSoShipPart>();
				var ship = mapComp.ShipsOnMapNew[shipIndex];
				if (ship.FoamDistributors.Any() && (shipPart.Props.isHull || shipPart.Props.isPlating))
				{
					foreach (CompHullFoamDistributor dist in ship.FoamDistributors)
					{
						if (dist.parent.TryGetComp<CompRefuelable>().Fuel > 0 && dist.parent.TryGetComp<CompPowerTrader>().PowerOn)
						{
							dist.parent.TryGetComp<CompRefuelable>().ConsumeFuel(1);
							__state = new Tuple<IntVec3, Faction, Map>(__instance.Position, __instance.Faction, __instance.Map);
							return true;
						}
					}
				}
				//move to post, add ship area
				//if (__instance.Faction == Faction.OfPlayer && __instance.def.blueprintDef != null && __instance.def.researchPrerequisites.All(r => r.IsFinished)) //place blueprints
				//GenConstruct.PlaceBlueprintForBuild(__instance.def, __instance.Position, __instance.Map, __instance.Rotation, Faction.OfPlayer, __instance.Stuff);
			}
			return true;
		}
		public static void Postfix(Tuple<IntVec3, Faction, Map> __state)
		{
			if (__state != null)
			{
				Thing newWall = ThingMaker.MakeThing(ThingDef.Named("HullFoamWall"));
				newWall.SetFaction(__state.Item2);
				GenPlace.TryPlaceThing(newWall, __state.Item1, __state.Item3, ThingPlaceMode.Direct);
			}
		}
	}*/
	/*vacuum pathfinding - disabled, not working
	[HarmonyPatch(typeof(PathFinder), "FindPath", typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms),
		typeof(PathEndMode), typeof(PathFinderCostTuning))]
	public static class H_Vacuum_PathFinder
	{
		private const int SpaceTileCostUnsuited = 10000;
		private const int SpaceTileCostSuited = 100;

		// The purpose of this transpiler is to add the pathfinding costs for space into the pathfinding code
		// We're looking for a line at the end of the calculation of the cost of a tile that looks like:
		//	 int num15 = num14 + PathFinder.calcGrid[index3].knownCost;
		// We want to patch our pathfinding cost right above that line
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var patched = false;
			var gotIndex = false;
			var gotCost = false;

			var indexOperand = new object();
			var costOperand = new object();

			CodeInstruction lastCode = null;

			var blueprintField = AccessTools.Field(typeof(PathFinder), "blueprintGrid");
			var signalField = AccessTools.Field(typeof(PathFinder), "calcGrid");

			foreach (var code in instructions)
			{
				// Need to get some operands - specifically, the operands for index5 (cell location) and
				// num14 (cell cost)

				// Retrieve num14 (cell cost) operand from a const addition above our injection point
				if (!gotCost && lastCode?.opcode == OpCodes.Ldloc_S && code.LoadsConstant(600))
				{
					costOperand = lastCode.operand;
					gotCost = true;
				}

				// Retrieve index5 (cell location) operand from blueprint grid just above injection point
				if (!gotIndex && code.opcode == OpCodes.Ldloc_S && lastCode.LoadsField(blueprintField))
				{
					indexOperand = code.operand;
					gotIndex = true;
				}

				// Our injection point is the first access to PathFinder.calcGrid directly after num14 is loaded
				// Note that the total cell cost (num14) is already loaded onto the stack by now, which is fine because
				// we need to add to it anyway
				if (!patched && lastCode?.opcode == OpCodes.Ldloc_S && (lastCode?.OperandIs(costOperand) ?? false) &&
					code.LoadsField(signalField))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0); // Load this
					var mapField = AccessTools.Field(typeof(PathFinder), "map");
					yield return new CodeInstruction(OpCodes.Ldfld, mapField); // Load map
					yield return new CodeInstruction(OpCodes.Ldarg_3); // Load TraverseParms
					yield return new CodeInstruction(OpCodes.Ldloc_S, indexOperand); // Load tile index
					var costMethod = AccessTools.Method(typeof(H_Vacuum_PathFinder), nameof(AdditionalPathCost));
					yield return new CodeInstruction(OpCodes.Call, costMethod); // Call method to get tile cost
					yield return new CodeInstruction(OpCodes.Add); // Add num14 and our cost
					yield return new CodeInstruction(OpCodes.Stloc_S, costOperand); // Store updated tile cost
					yield return new CodeInstruction(OpCodes.Ldloc_S, costOperand); // Load cost to replace one we took

					patched = true;
				}

				lastCode = code;
				yield return code;
			}
		}

		// Generate additional pathfinding costs for tiles that are in space
		public static int AdditionalPathCost(Map map, TraverseParms parms, int index)
		{
			// Only run in space, and if pawn doesn't have a space suit
			if (!map.IsSpace() || (!SaveOurShip2.ModSettings_SoS.useVacuumPathfinding && parms.pawn.Faction.IsPlayer)) return 0;

			// Find tile room
			var room = map.cellIndices.IndexToCell(index).GetRoom(map);

			// If room isn't space, zero extra cost
			if (!room?.IsSpace() ?? true) return 0;

			// If room is space, cost depending on whether pawn is suited or not
			return ShipInteriorMod2.EVAlevel(parms.pawn) > 6 ? SpaceTileCostSuited : SpaceTileCostUnsuited;
		}
	}
	[HarmonyPatch(typeof(Region), "DangerFor")]
	public static class H_Vacuum_Region_Danger
	{

		// The purpose of this transpiler is to increase the danger of vacuum regions
		// We're looking for a line right before the danger is cached and returned that looks like:
		//	 if (Current.ProgramState == ProgramState.Playing)
		// We want to patch our additional danger into that if statement
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var patched = false;

			CodeInstruction lastLastCode = null;
			CodeInstruction lastCode = null;

			var signalMethod = AccessTools.Method(typeof(Current), "get_ProgramState");

			foreach (var code in instructions)
			{
				// Our injection point is after the call to program state right after danger (local variable 1) is
				// stored (essentially, in the middle of an if statement, but need to dodge labels)
				if (!patched && (lastLastCode?.opcode == OpCodes.Stloc_1) && (lastCode?.Calls(signalMethod) ?? false))
				{
					yield return new CodeInstruction(OpCodes.Ldloc_1); // Load danger
					yield return new CodeInstruction(OpCodes.Ldarg_0); // Load this
					var roomProperty = AccessTools.Method(typeof(Region), "get_Room");
					yield return new CodeInstruction(OpCodes.Call, roomProperty); // Load room
					yield return new CodeInstruction(OpCodes.Ldarg_1); // Load pawn
					yield return new CodeInstruction(OpCodes.Ldarg_0); // Load this
					var mapProperty = AccessTools.Method(typeof(Region), "get_Map");
					yield return new CodeInstruction(OpCodes.Call, mapProperty); // Load map
					var addDangerMethod = AccessTools.Method(typeof(VacuumExtensions),
						nameof(VacuumExtensions.ExtraDangerFor));
					yield return new CodeInstruction(OpCodes.Call, addDangerMethod); // Call method to get danger
					yield return new CodeInstruction(OpCodes.Stloc_1); // Store updated danger

					patched = true;
				}

				lastLastCode = lastCode;
				lastCode = code;
				yield return code;
			}
		}
	}
	public static class VacuumExtensions
	{
		public static Danger ExtraDangerFor(Danger original, Room room, Pawn p, Map map)
		{
			// Always pass through deadly, if tile or map isn't space, return normal danger
			if (original == Danger.Deadly || !map.IsSpace() || (!SaveOurShip2.ModSettings_SoS.useVacuumPathfinding && p.Faction.IsPlayer) || (!room?.IsSpace() ?? true))
				return original;

			return ShipInteriorMod2.EVAlevel(p) > 3 ? Danger.Some : Danger.Deadly;
		}

		public static bool IsSpace(this Room room)
		{
			return room.FirstRegion.type != RegionType.Portal && (room.OpenRoofCount > 0 || room.TouchesMapEdge);
		}
	}*/

	//OBSOLETE - shuttle patches
	/*[HarmonyPatch(typeof(FlyShipLeaving), "LeaveMap")]
	public static class LeavingPodFix
	{
		public static bool Prefix(ref FlyShipLeaving __instance)
		{
			if (__instance.def.defName.Equals("PersonalShuttleSkyfaller") || __instance.def.defName.Equals("CargoShuttleSkyfaller") || __instance.def.defName.Equals("HeavyCargoShuttleSkyfaller") || __instance.def.defName.Equals("DropshipShuttleSkyfaller"))
			{
				if ((bool)typeof(FlyShipLeaving).GetField("alreadyLeft", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance))
				{
					__instance.Destroy(DestroyMode.Vanish);
					return false;
				}
				if (__instance.groupID < 0)
				{
					Log.Error("Drop pod left the map, but its group ID is " + __instance.groupID);
					__instance.Destroy(DestroyMode.Vanish);
					return false;
				}
				if (__instance.destinationTile < 0)
				{
					Log.Error("Drop pod left the map, but its destination tile is " + __instance.destinationTile);
					__instance.Destroy(DestroyMode.Vanish);
					return false;
				}
				Lord lord = TransporterUtility.FindLord(__instance.groupID, __instance.Map);
				if (lord != null)
				{
					__instance.Map.lordManager.RemoveLord(lord);
				}
				TravelingTransportPods travelingTransportPods;
				if (__instance.def.defName.Equals("PersonalShuttleSkyfaller"))
					travelingTransportPods = (TravelingTransportPods)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("TravelingShuttlesPersonal"));
				else if (__instance.def.defName.Equals("CargoShuttleSkyfaller"))
					travelingTransportPods = (TravelingTransportPods)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("TravelingShuttlesCargo"));
				else if (__instance.def.defName.Equals("HeavyCargoShuttleSkyfaller"))
					travelingTransportPods = (TravelingTransportPods)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("TravelingShuttlesHeavy"));
				else
					travelingTransportPods = (TravelingTransportPods)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("TravelingShuttlesDropship"));
				travelingTransportPods.Tile = __instance.Map.Tile;

				Thing t = __instance.Contents.innerContainer.Where(p => p is Pawn).FirstOrDefault();
				if (__instance.Map.GetComponent<ShipMapComp>().ShipMapState == ShipMapState.inCombat && t != null)
					travelingTransportPods.SetFaction(t.Faction);
				else
					travelingTransportPods.SetFaction(Faction.OfPlayer);
				travelingTransportPods.destinationTile = __instance.destinationTile;
				travelingTransportPods.arrivalAction = __instance.arrivalAction;
				Find.WorldObjects.Add(travelingTransportPods);

				List<Thing> pods = new List<Thing>();
				pods.AddRange(__instance.Map.listerThings.ThingsInGroup(ThingRequestGroup.ActiveDropPod));
				for (int i = 0; i < pods.Count; i++)
				{
					FlyShipLeaving dropPodLeaving = pods[i] as FlyShipLeaving;
					if (dropPodLeaving != null && dropPodLeaving.groupID == __instance.groupID)
					{
						typeof(FlyShipLeaving).GetField("alreadyLeft", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(dropPodLeaving, true);
						travelingTransportPods.AddPod(dropPodLeaving.Contents, true);
						dropPodLeaving.Contents = null;
						dropPodLeaving.Destroy(DestroyMode.Vanish);
					}
				}
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(DropPodUtility), "MakeDropPodAt")]
	public static class TravelingPodFix
	{
		public static bool Prefix(IntVec3 c, Map map, ActiveDropPodInfo info)
		{
			bool hasShuttle = false;
			//ThingDef shuttleDef = null;
			ThingDef skyfaller = null;
			Thing foundShuttle = null;
			foreach (Thing t in info.innerContainer)
			{
				if (t.TryGetComp<CompBecomeBuilding>() != null)
				{
					hasShuttle = true;
					//shuttleDef = t.def;
					skyfaller = t.TryGetComp<CompBecomeBuilding>().Props.skyfaller;
					foundShuttle = t;
					break;
				}
			}
			if (hasShuttle)
			{
				ActiveDropPod activeDropPod = (ActiveDropPod)ThingMaker.MakeThing(ThingDefOf.ActiveDropPod, null);
				activeDropPod.Contents = info;
				Skyfaller theShuttle = SkyfallerMaker.SpawnSkyfaller(skyfaller, activeDropPod, c, map);
				if (foundShuttle.TryGetComp<CompShuttleCosmetics>() != null)
				{
					Graphic_Single graphic = new Graphic_Single();
					CompProps_ShuttleCosmetics Props = foundShuttle.TryGetComp<CompShuttleCosmetics>().Props;
					int whichVersion = foundShuttle.TryGetComp<CompShuttleCosmetics>().whichVersion;
					GraphicRequest req = new GraphicRequest(typeof(Graphic_Single), Props.graphicsHover[whichVersion].texPath + "_south", ShaderDatabase.Cutout, Props.graphics[whichVersion].drawSize, Color.white, Color.white, Props.graphics[whichVersion], 0, null, "");
					graphic.Init(req);
					typeof(Thing).GetField("graphicInt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(theShuttle, graphic);
				}
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(DropPodIncoming), "Impact")]
	public static class IncomingPodFix
	{
		public static bool Prefix(ref DropPodIncoming __instance)
		{
			//spawns pawns and shuttle at location
			if (__instance.def.defName.Equals("ShuttleIncomingPersonal") || __instance.def.defName.Equals("ShuttleIncomingCargo") || __instance.def.defName.Equals("ShuttleIncomingHeavy") || __instance.def.defName.Equals("ShuttleIncomingDropship"))
			{
				for (int i = 0; i < 6; i++)
				{
					Vector3 loc = __instance.Position.ToVector3Shifted() + Gen.RandomHorizontalVector(1f);
					FleckMaker.ThrowDustPuff(loc, __instance.Map, 1.2f);
				}
				FleckMaker.ThrowLightningGlow(__instance.Position.ToVector3Shifted(), __instance.Map, 2f);

				Pawn myShuttle = null;
				ThingOwner container = ((ActiveDropPod)__instance.innerContainer[0]).Contents.innerContainer;

				for (int i = container.Count - 1; i >= 0; i--)
				{
					if (container[i] is Pawn && container[i].TryGetComp<CompBecomeBuilding>() != null)
						myShuttle = (Pawn)container[i];
				}
				var mapComp = __instance.Map.GetComponent<ShipMapComp>().ShipCombatOriginMap;
				ShipMapComp playerMapComp = null;
				if (mapComp != null)
					playerMapComp = mapComp.GetComponent<ShipMapComp>();
				for (int i = container.Count - 1; i >= 0; i--)
				{
					if (container[i] is Pawn)
					{
						GenPlace.TryPlaceThing(container[i], __instance.Position, __instance.Map, ThingPlaceMode.Near, delegate (Thing thing, int count) {
							PawnUtility.RecoverFromUnwalkablePositionOrKill(thing.Position, thing.Map);
							if (thing.Faction != Faction.OfPlayer && playerMapComp != null && playerMapComp.ShipLord != null)
								playerMapComp.ShipLord.AddPawn((Pawn)thing);
							/*if (thing.TryGetComp<CompShuttleCosmetics>() != null)
								CompShuttleCosmetics.ChangeShipGraphics((Pawn)thing, ((Pawn)thing).TryGetComp<CompShuttleCosmetics>().Props);*//*
						});
					}
					else if (myShuttle != null)
						myShuttle.inventory.innerContainer.TryAddOrTransfer(container[i]);
				}

				__instance.innerContainer.ClearAndDestroyContents(DestroyMode.Vanish);
				CellRect cellRect = __instance.OccupiedRect();

				for (int j = 0; j < cellRect.Area * __instance.def.skyfaller.motesPerCell; j++)
				{
					FleckMaker.ThrowDustPuff(cellRect.RandomVector3, __instance.Map, 2f);
				}
				if (__instance.def.skyfaller.cameraShake > 0f && __instance.Map == Find.CurrentMap)
				{
					Find.CameraDriver.shaker.DoShake(__instance.def.skyfaller.cameraShake);
				}
				if (__instance.def.skyfaller.impactSound != null)
				{
					__instance.def.skyfaller.impactSound.PlayOneShot(SoundInfo.InMap(new TargetInfo(__instance.Position, __instance.Map, false), MaintenanceType.None));
				}
				__instance.Destroy(DestroyMode.Vanish);

				if (myShuttle.Faction != Faction.OfPlayer)
				{
					if (myShuttle.Position.Roofed(myShuttle.Map) && Rand.Chance(0.5f))
					{
						Traverse.Create(myShuttle.TryGetComp<CompRefuelable>()).Field("fuel").SetValue(0);
						myShuttle.Destroy();
					}
					else
						myShuttle.GetComp<CompBecomeBuilding>().transform();
				}
				else if (myShuttle.Position.Fogged(myShuttle.Map))
					FloodFillerFog.FloodUnfog(myShuttle.Position, myShuttle.Map);
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Pawn), "GetGizmos")]
	public static class ShuttleGizmoFix
	{
		public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
		{
			if (__instance == null || __result == null)
				return;
			if (__instance.TryGetComp<CompBecomeBuilding>() != null)
			{
				List<Gizmo> newList = new List<Gizmo>();
				foreach (Gizmo g in __result)
				{
					newList.Add(g);
				}
				if (__instance.drafter == null)
				{
					__instance.drafter = new Pawn_DraftController(__instance);
					__instance.equipment = new Pawn_EquipmentTracker(__instance);
				}
				IEnumerable<Gizmo> draftGizmos = (IEnumerable<Gizmo>)typeof(Pawn_DraftController).GetMethod("GetGizmos", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance.drafter, new object[] { });
				foreach (Gizmo c2 in draftGizmos)
				{
					newList.Add(c2);
				}
				foreach (ThingComp comp in __instance.AllComps)
				{
					foreach (Gizmo com in comp.CompGetGizmosExtra())
					{
						newList.Add(com);
					}
				}
				__result = newList;
			}
		}
	}

	[HarmonyPatch(typeof(Pawn), "IsColonyMech", MethodType.Getter)] //1.4
	public static class MechGizmoFix
	{
		public static bool Postfix(bool __result, Pawn __instance)
		{
			if (AccessExtensions.Utility.shuttleCache.Contains(__instance)) return false;
			return __result;
		}
	}

	[HarmonyPatch(typeof(Pawn_DraftController), "ShowDraftGizmo", MethodType.Getter)] //1.4
	public static class GizmoFix
	{
		public static void Postfix(Pawn_DraftController __instance, ref bool __result)
		{
			if (__instance.pawn.TryGetComp<CompBecomeBuilding>() != null)
				__result = true;
		}
	}

	[HarmonyPatch(typeof(FloatMenuMakerMap), "CanTakeOrder")]
	public static class OrderFix
	{
		public static void Postfix(Pawn pawn, ref bool __result)
		{
			if (pawn.TryGetComp<CompBecomeBuilding>() != null)
				__result = true;
		}
	}

	[HarmonyPatch(typeof(Caravan), "GetGizmos")]
	public static class OtherGizmoFix
	{
		public static void Postfix(Caravan __instance, ref IEnumerable<Gizmo> __result)
		{
			if (__instance == null || __result == null)
				return;

			List<Gizmo> newList = new List<Gizmo>();
			foreach (Gizmo g in __result)
			{
				newList.Add(g);
			}

			float shuttleCarryWeight = 0;
			float pawnWeight = 0;
			float minRange = float.MaxValue;
			bool allFullyFueled = true;
			List<Pawn> shuttlesToRefuel = new List<Pawn>();
			List<Thing> CaravanThings = CaravanInventoryUtility.AllInventoryItems(__instance);
			foreach (Pawn p in __instance.pawns)
			{
				if (p.TryGetComp<CompBecomeBuilding>() != null)
				{
					shuttleCarryWeight += p.TryGetComp<CompBecomeBuilding>().Props.buildingDef.GetCompProperties<CompProperties_Transporter>().massCapacity;
					if (p.TryGetComp<CompRefuelable>() != null && p.TryGetComp<CompRefuelable>().Fuel / p.TryGetComp<CompBecomeBuilding>().Props.buildingDef.GetCompProperties<CompProps_ShuttleLaunchable>().fuelPerTile < minRange)
					{
						minRange = p.TryGetComp<CompRefuelable>().Fuel / p.TryGetComp<CompBecomeBuilding>().Props.buildingDef.GetCompProperties<CompProps_ShuttleLaunchable>().fuelPerTile;
					}
					if (p.TryGetComp<CompRefuelable>() != null && p.TryGetComp<CompRefuelable>().FuelPercentOfMax < 0.8f)
					{
						foreach (Thing t in CaravanThings)
						{
							if (p.TryGetComp<CompRefuelable>().Props.fuelFilter.Allows(t.def))
							{
								shuttlesToRefuel.Add(p);
								break;
							}
						}
						allFullyFueled = false;
					}
				}
				else if (p.TryGetComp<CompShuttleLaunchable>() == null)
				{
					pawnWeight += p.def.BaseMass;
				}
			}
			if (shuttleCarryWeight > 0)
			{
				float totalMass = pawnWeight + __instance.MassUsage;
				Gizmo launchGizmo = new Command_Action
				{
					defaultLabel = "Launch Caravan",
					defaultDesc = "Load this caravan into shuttle(s) and launch it",
					icon = CompShuttleLaunchable.LaunchCommandTex,
					action = delegate
					{
						ShuttleCaravanUtility.LaunchMe(__instance, minRange, allFullyFueled);
					}
				};

				if (totalMass > shuttleCarryWeight)
					launchGizmo.Disable("Caravan is too heavy for shuttle(s) to carry: " + totalMass + "/" + shuttleCarryWeight);

				newList.Add(launchGizmo);
			}
			if (shuttlesToRefuel.Count > 0)
			{
				Gizmo refuelGizmo = new Command_Action
				{
					defaultLabel = "Refuel Shuttles",
					defaultDesc = "Use caravan inventory to refuel shuttle(s)",
					icon = CompShuttleLaunchable.SetTargetFuelLevelCommand,
					action = delegate {
						ShuttleCaravanUtility.RefuelMe(__instance, shuttlesToRefuel);
					}
				};

				newList.Add(refuelGizmo);
			}

			List<MinifiedThing> inactiveShuttles = new List<MinifiedThing>();
			foreach (Thing t in __instance.AllThings)
			{
				if (t is MinifiedThing)
				{
					MinifiedThing building = (MinifiedThing)t;
					if (building.InnerThing.TryGetComp<CompShuttleLaunchable>() != null)
					{
						inactiveShuttles.Add(building);
					}
				}
			}
			List<MinifiedThing> fuelableShuttles = new List<MinifiedThing>();
			foreach (MinifiedThing building in inactiveShuttles)
			{
				if (building.InnerThing.TryGetComp<CompRefuelable>() == null)
				{
					fuelableShuttles.Add(building);
				}
				else if (building.InnerThing.TryGetComp<CompRefuelable>().HasFuel)
				{
					fuelableShuttles.Add(building);
				}
				else
				{
					foreach (Thing tee in CaravanInventoryUtility.AllInventoryItems(__instance))
					{
						if (building.InnerThing.TryGetComp<CompRefuelable>().Props.fuelFilter.Allows(tee.def))
						{
							fuelableShuttles.Add(building);
							break;
						}
					}
				}
			}
			if (fuelableShuttles.Count > 0)
			{
				Gizmo activateGizmo = new Command_Action
				{
					defaultLabel = "Activate Shuttles",
					defaultDesc = "Activate shuttle(s) and refuel them if possible",
					icon = CompShuttleLaunchable.SetTargetFuelLevelCommand,
					action = delegate {
						ShuttleCaravanUtility.ActivateMe(__instance, fuelableShuttles);
					}
				};

				newList.Add(activateGizmo);
			}

			__result = newList;
		}
	}

	[HarmonyPatch(typeof(MassUtility), "Capacity")]
	public static class FixShuttleCarryCap
	{
		public static void Postfix(ref float __result, Pawn p)
		{
			if (p.TryGetComp<CompBecomeBuilding>() != null)
			{
				__result = p.TryGetComp<CompBecomeBuilding>().Props.buildingDef.GetCompProperties<CompProperties_Transporter>().massCapacity;
			}
		}
	}

	[HarmonyPatch(typeof(CaravanUIUtility), "AddPawnsSections")]
	public static class UIFix
	{
		public static void Postfix(TransferableOneWayWidget widget, List<TransferableOneWay> transferables)
		{
			if (Find.WorldSelector.FirstSelectedObject == null || !(Find.WorldSelector.FirstSelectedObject is MapParent) || ((MapParent)Find.WorldSelector.FirstSelectedObject).Map == null || !((MapParent)Find.WorldSelector.FirstSelectedObject).Map.IsPlayerHome)
			{
				IEnumerable<TransferableOneWay> source = from x in transferables
															where x.ThingDef.category == ThingCategory.Pawn
															select x;
				widget.AddSection(TranslatorFormattedStringExtensions.Translate("SoSShuttles"), from x in source
																								where (((Pawn)x.AnyThing).TryGetComp<CompBecomeBuilding>() != null)
																								select x);
			}
		}
	}

	[HarmonyPatch(typeof(TransportPodsArrivalAction_GiveToCaravan), "StillValid")]
	public static class MakeSureNotToLoseYourShuttle
	{
		static bool hasShuttle = false;
		public static bool Prefix(IEnumerable<IThingHolder> pods)
		{
			hasShuttle = false;
			foreach (IThingHolder pod in pods)
			{
				foreach (Thing t in pod.GetDirectlyHeldThings())
				{
					if (t.TryGetComp<CompBecomeBuilding>() != null)
					{
						hasShuttle = true;
						return false;
					}
				}
			}
			return true;
		}
		public static void Postfix(ref FloatMenuAcceptanceReport __result)
		{
			if (hasShuttle)
				__result = true;
		}
	}

	[HarmonyPatch(typeof(PawnCapacitiesHandler), "CapableOf")]
	public static class ShuttlesCannotConstruct //This is slow and shitty, but Tynan didn't leave us many options to avoid a nullref
	{
		public static void Postfix(PawnCapacityDef capacity, PawnCapacitiesHandler __instance, ref bool __result)
		{
			if (capacity == PawnCapacityDefOf.Manipulation && __instance.pawn.TryGetComp<CompBecomeBuilding>() != null)
			{
				__result = false;
			}
		}
	}

	[HarmonyPatch(typeof(Pawn_MeleeVerbs), "ChooseMeleeVerb")]
	public static class ThatWasAnOldBug
	{
		public static bool Prefix(Pawn_MeleeVerbs __instance)
		{
			return __instance.Pawn.TryGetComp<CompBecomeBuilding>() == null;
		}
	}

	[HarmonyPatch(typeof(Dialog_LoadTransporters), "AddPawnsToTransferables", null)]
	public static class TransportPrisoners_Patch
	{
		public static bool Prefix(Dialog_LoadTransporters __instance)
		{
			List<Pawn> list = CaravanFormingUtility.AllSendablePawns(__instance.map);
			for (int i = 0; i < list.Count; i++)
			{
				typeof(Dialog_LoadTransporters)
					.GetMethod("AddToTransferables", BindingFlags.NonPublic | BindingFlags.Instance)
					.Invoke(__instance, new object[1] { list[i] });
			}

			return false;
		}
	}

	//obs-shuttle change?
	[HarmonyPatch(typeof(TravelingTransportPods), "Start", MethodType.Getter)]
	public static class FromSpaceship
	{
		public static void Postfix(TravelingTransportPods __instance, ref Vector3 __result)
		{
			foreach (WorldObject ship in Find.World.worldObjects.AllWorldObjects.Where(o => o is WorldObjectOrbitingShip))
				if (ship.Tile == __instance.initialTile)
					__result = ship.DrawPos;
			foreach (WorldObject site in Find.World.worldObjects.AllWorldObjects.Where(o => o is SpaceSite || o is MoonBase))
				if (site.Tile == __instance.initialTile)
					__result = site.DrawPos;
		}
	}

	[HarmonyPatch(typeof(TravelingTransportPods), "End", MethodType.Getter)]
	public static class ToSpaceship
	{
		public static void Postfix(TravelingTransportPods __instance, ref Vector3 __result)
		{
			foreach (WorldObject ship in Find.World.worldObjects.AllWorldObjects.Where(o => o is WorldObjectOrbitingShip))
				if (ship.Tile == __instance.destinationTile)
					__result = ship.DrawPos;
			foreach (WorldObject site in Find.World.worldObjects.AllWorldObjects.Where(o => o is SpaceSite || o is MoonBase))
				if (site.Tile == __instance.destinationTile)
					__result = site.DrawPos;
		}
	}

	[HarmonyPatch(typeof(Skyfaller), "HitRoof")]
	public static class ShuttleBayAcceptsShuttle
	{
		public static bool Prefix(Skyfaller __instance)
		{
			if (__instance.Position.GetThingList(__instance.Map).Any(t =>
				t.def == ResourceBank.ThingDefOf.ShipShuttleBay || t.def == ResourceBank.ThingDefOf.ShipShuttleBayLarge || t.TryGetComp<CompShipSalvageBay>() != null))
			{
				return false;
			}
			if (__instance.Map.IsSpace() && (__instance.def.defName.Equals("ShuttleIncomingPersonal") || __instance.def == ThingDefOf.DropPodIncoming)) //dont breach roof with small pods in space
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(TransportPodsArrivalActionUtility), "DropTravelingTransportPods")]
	public static class ShuttleBayArrivalPrecision
	{
		public static bool Prefix(List<ActiveDropPodInfo> dropPods, IntVec3 near, Map map)
		{
			if (map.Parent != null && map.Parent.def == ResourceBank.WorldObjectDefOf.ShipOrbiting)
			{
				TransportPodsArrivalActionUtility.RemovePawnsFromWorldPawns(dropPods);
				for (int i = 0; i < dropPods.Count; i++)
				{
					DropPodUtility.MakeDropPodAt(near, map, dropPods[i]);
				}

				return false;
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(Trigger_UrgentlyHungry), "ActivateOn")]
	public static class MechsDontEat
	{
		public static bool Prefix(Lord lord, out bool __state)
		{
			__state = false;
			foreach (Pawn p in lord.ownedPawns)
			{
				if (p.RaceProps.IsMechanoid)
				{
					__state = true;
					return false;
				}
			}
			return true;
		}
		public static void Postfix(ref bool __result, bool __state)
		{
			if (__state)
				__result = false;
		}
	}

	[HarmonyPatch(typeof(TransferableUtility), "CanStack")]
	public static class MechsCannotStack
	{
		public static bool Prefix(Thing thing, ref bool __result)
		{
			if (thing is Pawn && ((Pawn)thing).RaceProps.IsMechanoid)
			{
				__result = false;
				return false;
			}

			return true;
		}
	}*/
}
