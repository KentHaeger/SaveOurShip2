using HarmonyLib;
using RimWorld;
using Verse;

namespace SaveOurShip2
{
	// Helper: SOS2 engine sits on Odyssey gravship substructure (any cell under the engine has a
	// substructure foundation). Used to gate the SOS2 ship-cache so a lone engine bolted to a
	// gravship isn't pulled into SOS2's caching system and treated as a wreck.
	static class ShipEngineSubstructureCheck
	{
		public static bool EngineOnGravship(Thing thing)
		{
			if (!ModsConfig.OdysseyActive)
				return false;
			if (thing == null || !thing.Spawned)
				return false;
			if (thing.TryGetComp<CompEngineTrail>() == null)
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

	// A SOS2 engine on gravship substructure must not be treated as a SOS2 ship part: skip the
	// CompShipCachePart spawn logic that would register it in MapShipCells / spin up a new
	// SpaceShipCache for it (would otherwise produce a one-engine wreck on the gravship). The
	// engine still works (CompEngineTrail and its other comps are unaffected).
	[HarmonyPatch(typeof(CompShipCachePart), nameof(CompShipCachePart.PostSpawnSetup))]
	public static class CompShipCachePart_PostSpawnSetup_GravshipEngine
	{
		public static bool Prefix(CompShipCachePart __instance)
		{
			return !ShipEngineSubstructureCheck.EngineOnGravship(__instance.parent);
		}
	}

	// Symmetric guard on despawn - we never registered, so don't try to deregister either (the
	// SOS2 despawn path NREs in some places when given a part it never tracked).
	[HarmonyPatch(typeof(CompShipCachePart), nameof(CompShipCachePart.PreDeSpawn))]
	public static class CompShipCachePart_PreDeSpawn_GravshipEngine
	{
		public static bool Prefix(CompShipCachePart __instance)
		{
			return !ShipEngineSubstructureCheck.EngineOnGravship(__instance.parent);
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
