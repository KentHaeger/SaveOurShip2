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
}
