using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SaveOurShip2
{
	// Helper: building sits on Odyssey gravship substructure (any cell under it has a substructure
	// foundation). Anything on gravship substructure cannot also be a SOS2 ship part - a lone engine,
	// wall, or anything else bolted to a gravship must not be pulled into SOS2's caching system, or it
	// gets treated as a wreck and the gravship gets misclassified.
	static class GravshipSubstructureCheck
	{
		public static bool OnSubstructure(Thing thing)
		{
			if (!ModsConfig.OdysseyActive)
				return false;
			if (thing == null || !thing.Spawned)
				return false;
			if (thing.TryGetComp<CompShipCachePart>() == null)
				return false;
			Map map = thing.Map;
			if (map == null)
				return false;
			foreach (IntVec3 c in thing.OccupiedRect())
			{
				if (!c.InBounds(map))
					continue;
				TerrainDef foundation = map.terrainGrid.FoundationAt(c);
				if (foundation != null && foundation.IsSubstructure)
					return true;
			}
			return false;
		}
	}

	// Shared detach: pull a SOS2 ship part out of its SpaceShipCache and scrub every cell it occupies
	// from MapShipCells. If the cache is left empty, drop it entirely. Used at two trigger points:
	// part spawned on substructure, and substructure laid under an already-registered part.
	static class GravshipDetach
	{
		public static void DetachFromSos2(Building b)
		{
			if (b == null || !b.Spawned)
				return;
			Map map = b.Map;
			if (map == null)
				return;
			ShipMapComp mapComp = map.GetComponent<ShipMapComp>();
			if (mapComp == null)
				return;
			int idx = mapComp.ShipIndexOnVec(b.Position);
			SpaceShipCache ship = null;
			if (idx != -1 && mapComp.ShipsOnMap.TryGetValue(idx, out ship))
				ship.RemoveFromCache(b, DestroyMode.Vanish);
			//clear every cell this building occupies from MapShipCells - it is no longer a SOS2 ship part
			foreach (IntVec3 cell in b.OccupiedRect())
			{
				if (mapComp.MapShipCells.ContainsKey(cell))
					mapComp.MapShipCells.Remove(cell);
			}
			//catch any empty ships - the one this part was just removed from, and any others (e.g.
			//pre-existing zombies left by the prior prefix-skip implementation, or odd loads).
			//Use Buildings.Count as the source of truth (BuildingCount field can drift).
			PruneEmptyShips(mapComp);
		}

		public static void PruneEmptyShips(ShipMapComp mapComp)
		{
			if (mapComp == null) return;
			List<int> empties = null;
			foreach (var kv in mapComp.ShipsOnMap)
			{
				SpaceShipCache s = kv.Value;
				if (s == null || s.Buildings == null || s.Buildings.Count == 0)
				{
					if (empties == null) empties = new List<int>();
					empties.Add(kv.Key);
				}
			}
			if (empties != null)
			{
				foreach (int idx in empties)
					mapComp.RemoveShipFromCache(idx);
			}
		}
	}

	// CompGravshipThruster hard-requires a CompBreakdownable - its CanBeActive (and inspect string)
	// dereference Breakdownable and NRE without one. SOS2 engines should never actually break down,
	// so this variant satisfies that requirement but immediately drops out of the BreakdownManager's
	// roster, and DoBreakdown is patched out for it (see below).
	public class CompShipEngineBreakdownable : CompBreakdownable
	{
		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad); //base registers with the BreakdownManager
			parent.Map?.GetComponent<BreakdownManager>()?.Deregister(this); //...drop straight back out
		}
	}


	// ===== Harmony patches =====

	// Sweep empty SpaceShipCache entries on map FinalizeInit - catches zombie ships persisted from
	// pre-fix saves (engine-on-substructure left an empty cache) or any other source of 0-part ships.
	[HarmonyPatch(typeof(Map), nameof(Map.FinalizeInit))]
	public static class Map_FinalizeInit_PruneEmptyShipCaches
	{
		public static void Postfix(Map __instance)
		{
			ShipMapComp mapComp = __instance.GetComponent<ShipMapComp>();
			GravshipDetach.PruneEmptyShips(mapComp);
		}
	}

	// A SOS2 ship part on gravship substructure must not be a SOS2 ship part. Let vanilla PostSpawnSetup
	// run normally (so all internal fields - map/mapComp/cellsUnder/fac - are initialized; otherwise
	// CompInspectStringExtra and PostDeSpawn NRE), then immediately detach the part if on substructure.
	[HarmonyPatch(typeof(CompShipCachePart), nameof(CompShipCachePart.PostSpawnSetup))]
	public static class CompShipCachePart_PostSpawnSetup_OnSubstructure
	{
		public static void Postfix(CompShipCachePart __instance)
		{
			if (GravshipSubstructureCheck.OnSubstructure(__instance.parent))
				GravshipDetach.DetachFromSos2(__instance.parent as Building);
		}
	}

	// Retroactive case: SOS2 ship part already spawned + registered, then substructure laid under it.
	// PostSpawnSetup ran before substructure existed, so the part stayed in the SOS2 cache. When
	// substructure is set, scan the cell for any SOS2 ship parts and force-detach them.
	[HarmonyPatch(typeof(TerrainGrid), nameof(TerrainGrid.SetFoundation))]
	public static class TerrainGrid_SetFoundation_DetachOnSubstructure
	{
		public static void Postfix(TerrainGrid __instance, IntVec3 c, TerrainDef newTerr)
		{
			if (newTerr == null || !newTerr.IsSubstructure)
				return;
			Map map = __instance.map; //publicized via Krafs.Publicizer
			if (map == null || !c.InBounds(map))
				return;
			//snapshot - DetachFromSos2 mutates the cell's MapShipCells while we iterate
			foreach (Thing t in c.GetThingList(map).ToList())
			{
				if (t is Building b && b.TryGetComp<CompShipCachePart>() != null)
					GravshipDetach.DetachFromSos2(b);
			}
		}
	}

	// DoBreakdown is the single chokepoint for every breakdown - the BreakdownManager's random check
	// AND direct calls such as a failed gravship landing outcome. Deregistering only stops the former;
	// skipping DoBreakdown here makes SOS2 engines immune to all of them.
	[HarmonyPatch(typeof(CompBreakdownable), nameof(CompBreakdownable.DoBreakdown))]
	public static class CompBreakdownable_DoBreakdown_ShipEngine
	{
		public static bool Prefix(CompBreakdownable __instance)
		{
			return !(__instance is CompShipEngineBreakdownable);
		}
	}

	// A SOS2 engine carries CompGravshipThruster only as a secondary (Odyssey) role. While it isn't
	// linked to a grav engine, the comp's red "not functional: not connected" inspect text is just
	// noise on a working SOS2 engine - blank the thruster inspect line in that case.
	[HarmonyPatch(typeof(CompGravshipThruster), nameof(CompGravshipThruster.CompInspectStringExtra))]
	public static class CompGravshipThruster_InspectString_ShipEngine
	{
		public static void Postfix(CompGravshipThruster __instance, ref string __result)
		{
			if (__instance.parent.TryGetComp<CompEngineTrail>() != null && __instance.LinkedBuildings.NullOrEmpty())
				__result = "";
		}
	}

	// Boundary safety: a SOS2 engine that's part of a SOS2 ship structure must not also pose as a
	// gravship thruster - the SOS2 ship would tear it off on move, the gravship would tear it off on
	// launch, and both systems would double-claim its fuel. Refuse to link/be-active in that case;
	// the dual role activates only when the engine is standalone (no SOS2 ship cache on its cell).
	[HarmonyPatch(typeof(CompGravshipThruster), nameof(CompGravshipThruster.CanLink))]
	public static class CompGravshipThruster_CanLink_NotOnShip
	{
		public static void Postfix(CompGravshipThruster __instance, ref bool __result)
		{
			if (__result && IsOnSos2Ship(__instance.parent))
				__result = false;
		}

		public static bool IsOnSos2Ship(Thing engine)
		{
			if (engine == null || !engine.Spawned)
				return false;
			//SOS2 engine has CompShipCachePart by def - the meaningful question is whether its cell
			//is currently registered to a ship in the map's ShipMapComp
			if (engine.TryGetComp<CompShipCachePart>() == null)
				return false;
			ShipMapComp mapComp = engine.Map?.GetComponent<ShipMapComp>();
			if (mapComp == null)
				return false;
			return mapComp.ShipIndexOnVec(engine.Position) != -1;
		}
	}

	// Symmetric guard on CanBeActive: even if a stale link slipped past CanLink (e.g. the engine
	// joined a SOS2 ship cache after linking), don't let the thruster contribute during launch.
	[HarmonyPatch(typeof(CompGravshipThruster), nameof(CompGravshipThruster.CanBeActive), MethodType.Getter)]
	public static class CompGravshipThruster_CanBeActive_NotOnShip
	{
		public static void Postfix(CompGravshipThruster __instance, ref bool __result)
		{
			if (__result && CompGravshipThruster_CanLink_NotOnShip.IsOnSos2Ship(__instance.parent))
				__result = false;
		}
	}
}
