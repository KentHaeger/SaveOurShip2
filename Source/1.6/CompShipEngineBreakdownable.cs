using HarmonyLib;
using RimWorld;
using Verse;

namespace SaveOurShip2
{
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
