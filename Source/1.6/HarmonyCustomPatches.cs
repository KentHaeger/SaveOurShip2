using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using HarmonyLib;

namespace SaveOurShip2
{
	// Here go complicated things like patches to delegates requiring manual method search rather than simple annotation
	public static class HarmonyCustomPatches
	{
		public static void Apply(Harmony harmony)
		{
			GravshipTargetingPatch.Apply(harmony);
			DontCleanShipsPatch.Apply(harmony);
		}
	}

	public static class GravshipTargetingPatch
	{
		public static void Apply(Harmony harmony)
		{
			const string innerClassName = "<>c__DisplayClass14_0";
			const string methodName = "<StartChoosingDestination_NewTemp>b__0";
			Type[] types = typeof(CompPilotConsole).GetNestedTypes(AccessTools.all);
			var innerType = types.FirstOrDefault(type => type.Name == innerClassName);
			List<MethodInfo> innerMethods = innerType.GetDeclaredMethods();
			MethodInfo delegateValidateTarget = innerMethods.FirstOrDefault(method => method.Name == methodName);
			harmony.Patch(delegateValidateTarget, prefix: new HarmonyMethod(typeof(GravshipTargetingPatch), nameof(ValidateTargetPrefix)));
		}

		// Delegate validating gravship target will disallow sending it to the map participating in ship battle
		// to avoid some tricks: for going to enemy, that is cheatingly big boarding shuttle that can have non-ship turrets around
		// and completely bypasses enemy weapons and PDs.
		// For sending to player ship, gravship implements reduced set of ship mechanics, no TWR, so better not be used 
		// as damage sponge that doesn't care about speed.
		public static bool ValidateTargetPrefix(PlanetTile tile)
		{
			MapParent worldObject;
			if (Find.WorldObjects.TryGetWorldObjectAt<MapParent>(tile, out worldObject))
			{
				if (worldObject is WorldObjectOrbitingShip ship &&
					ship.Map.GetComponent<ShipMapComp>().ShipMapState == ShipMapState.inCombat)
				{
					Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.CantLaunchGravshipToCombatMap"),
						MessageTypeDefOf.RejectInput, historical: false);
					return false;
				}
			}
			return true;
		}
	}
	public static class DontCleanShipsPatch
	{
		public static void Apply(Harmony harmony)
		{
			const string innerClassName = "<>c__DisplayClass5_1";
			const string methodName = "<RequestOrbitalTraderOption>b__1";
			Type[] types = typeof(FactionDialogMaker).GetNestedTypes(AccessTools.all);
			var innerType = types.FirstOrDefault(type => type.Name == innerClassName);
			List<MethodInfo> innerMethods = innerType.GetDeclaredMethods();
			MethodInfo delegateDialogOption = innerMethods.FirstOrDefault(method => method.Name == methodName);
			harmony.Patch(delegateDialogOption, prefix: new HarmonyMethod(typeof(DontCleanShipsPatch), nameof(CallOrbitalTraderDialogOptionPrefix)),
				postfix: new HarmonyMethod(typeof(DontCleanShipsPatch), nameof(CallOrbitalTraderDialogOptionPostfix)));
			harmony.Patch(AccessTools.Method(typeof(PassingShip), nameof(PassingShip.Depart)),
				prefix: new HarmonyMethod(typeof(DontCleanShipsPatch), nameof(DepartPrefix)));
			harmony.Patch(AccessTools.Method(typeof(FactionDialogMaker), nameof(FactionDialogMaker.RequestOrbitalTraderOption)),
				prefix: new HarmonyMethod(typeof(DontCleanShipsPatch), nameof(DialogMakerPrefix)));
		}

		// Detect if Depart is called from Odyssey dialog for calling orbital traders.
		// And do not do depart if that is done on space map.
		private static bool cleaningShipsInOdysseyDialog = false;
		private static Map traderDialogMap = null;
		public static bool CallOrbitalTraderDialogOptionPrefix()
		{
			if (traderDialogMap != null && traderDialogMap.IsSpace())
			{
				cleaningShipsInOdysseyDialog = true;
			}
			return true;
		}
		public static void CallOrbitalTraderDialogOptionPostfix()
		{
			cleaningShipsInOdysseyDialog = false;
		}

		public static bool DepartPrefix()
        {
			return !cleaningShipsInOdysseyDialog;
		}
		public static void DialogMakerPrefix(Map map)
        {
			// Save the map here so that don't have to dig it from __locals of the class generated for delegate
			traderDialogMap = map;
		}
	}
}
