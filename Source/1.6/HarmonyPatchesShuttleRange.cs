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
using Vehicles.World;

namespace SaveOurShip2
{
	// Here go complicated things like patches to delegates requiring manual method search rather than simple annotation
	public static class HarmonyPatchesShuttleRange
	{
		public static void Apply(Harmony harmony)
		{
			StartChoosingDestinationPatch.Apply(harmony);
		}
	}

	public static class StartChoosingDestinationPatch
	{
		private static Type GetInnerType()
		{
			const string innerClassName = "<>c__DisplayClass41_0";
			Type[] types = typeof(CompLaunchable).GetNestedTypes(AccessTools.all);
			var innerType = types.FirstOrDefault(type => type.Name == innerClassName);
			return innerType;
		}
		public static void Apply(Harmony harmony)
		{
			const string methodName = "<StartChoosingDestination>b__1";
			Type innerType = GetInnerType();
			List<MethodInfo> innerMethods = innerType.GetDeclaredMethods();
			MethodInfo delegateStarChoosingDestination = innerMethods.FirstOrDefault(method => method.Name == methodName);
			harmony.Patch(delegateStarChoosingDestination, transpiler: new HarmonyMethod(typeof(StartChoosingDestinationPatch), nameof(Transpiler)));
		}
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo originalDrawRadius = AccessTools.Method(typeof(GenDraw), nameof(GenDraw.DrawWorldRadiusRing));
			//List<MethodInfo> methods = typeof(StartChoosingDestinationPatch).GetDeclaredMethods();
			MethodInfo patchedDrawRadius = AccessTools.Method(typeof(StartChoosingDestinationPatch), nameof(DrawWorldRadiusRingPatched));
			//MethodInfo patchedDrawRadius = methods.FirstOrDefault(method => method.Name == "DrawWorldRadiusRingPatched");
			foreach (CodeInstruction instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Call && (MethodInfo)instruction.operand == originalDrawRadius)
				{
					const string thisFieldName = "<>4__this";
					// Extra argunment, this for launchable argument
					List<FieldInfo> innerFields = GetInnerType().GetDeclaredFields();
					FieldInfo this_field = innerFields.FirstOrDefault(field => field.Name == thisFieldName);
					// Load inner class self
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					// Grab captured this field from inner class and load it
					yield return new CodeInstruction(OpCodes.Ldfld, this_field);
					yield return new CodeInstruction(OpCodes.Call, patchedDrawRadius);
				}
				else
				{
					yield return instruction;
				}
			}
		}

		public static void DrawWorldRadiusRingPatched(PlanetTile center, int radius, Material overrideMat, CompLaunchable launchable)
		{
			PlanetTile adjustedCenter = center;
			if (HarmonyShuttleUtility.ShouldAdjustDistance(launchable))
			{
				Map sourceMap = launchable?.parent?.Map;
				MapParent sourceObject = sourceMap.Parent;
				if (sourceObject is WorldObjectOrbitingShip ship)
				{
					adjustedCenter = WorldObject_FastTileGetter.GetClosesTileFor(sourceObject);
					Log.Message("Adjusted Center");
				}
			}
			GenDraw.DrawWorldRadiusRing(adjustedCenter, radius, overrideMat);
		}
	}

	// Utility for adjusting shuttle range and actual fuel spent in a similaw ay in several base game functions
	public static class HarmonyShuttleUtility
	{
		public static bool ShouldAdjustDistance(CompLaunchable launchable)
		{
			Map sourceMap = launchable?.parent?.Map;
			return sourceMap?.IsSpace() ?? false;
		}
		public static float AdjustedDistance(GlobalTargetInfo target, CompLaunchable launchable)
		{
			Map sourceMap = launchable?.parent?.Map;
			MapParent sourceObject = sourceMap.Parent;
			if (target.Tile == null)
			{
				Log.WarningOnce("SOS 2: ChoseWorldTarget patch: no target tile", 15675728);
				return -1f;
			}
			Vector3 targetPos = WorldHelper.GetTilePos(target.Tile);
			float distance = SmashTools.Ext_Math.SphericalDistance(sourceObject.DrawPos, targetPos);
			return distance;
		}
	}

	// Patch for Odyssey shuttle launched from space tile. fuel calculation change: from source tile to actual source world object location
	public static class WorldGridExtension
	{
		public static int TraversalDistanceBetweenPatched(this WorldGrid grid, PlanetTile start, PlanetTile end,
			bool passImpassable, int maxDist, bool canTraverseLayers, CompLaunchable launchable)
		{
			Log.WarningOnce("Launchable passed: " + launchable.GetType(), 36729572);
			int originalDistance = grid.TraversalDistanceBetween(start, end, passImpassable, maxDist, canTraverseLayers);
			Log.Message("Originaldistance:" + originalDistance);
			if (HarmonyShuttleUtility.ShouldAdjustDistance(launchable))
			{
				Map sourceMap = launchable?.parent?.Map;
				MapParent sourceObject = sourceMap.Parent;
				PlanetTile actualStartTile = PlanetTile.Invalid;
				if (sourceObject is WorldObjectOrbitingShip ship)
				{
					actualStartTile = WorldObject_FastTileGetter.GetClosesTileFor(sourceObject);
				}
				else
				{
					return originalDistance;
				}
				/*GlobalTargetInfo target = new GlobalTargetInfo(end);
				//float adjusteddistance = HarmonyShuttleUtility.AdjustedDistance(target, launchable);*/
				int adjusteddistance = grid.TraversalDistanceBetween(actualStartTile, end, passImpassable, maxDist, canTraverseLayers);
				Log.Message("Adjusteddistance:" + adjusteddistance);
				return adjusteddistance;
			}
			return originalDistance;
		}
	}

	[HarmonyPatch(typeof(CompLaunchable), "TargetingLabelGetter")]
	public static class FixTargetingLabelGetter
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo originalTraversalDistance = AccessTools.Method(typeof(WorldGrid), nameof(WorldGrid.TraversalDistanceBetween));
			MethodInfo patchedTraversalDistance = AccessTools.Method(typeof(WorldGridExtension), nameof(WorldGridExtension.TraversalDistanceBetweenPatched));
			foreach (CodeInstruction instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Callvirt && (MethodInfo)instruction.operand == originalTraversalDistance)
				{
					// Extra argunment, this for launchable argument
					yield return new CodeInstruction(OpCodes.Ldarg_S, 5);
					yield return new CodeInstruction(OpCodes.Call, patchedTraversalDistance);
				}
				else
				{
					yield return instruction;
				}
			}
		}
	}

	[HarmonyPatch(typeof(CompLaunchable), "TryLaunch")]
	public static class FixTryLaunchDistance
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo originalTraversalDistance = AccessTools.Method(typeof(WorldGrid), nameof(WorldGrid.TraversalDistanceBetween));
			MethodInfo patchedTraversalDistance = AccessTools.Method(typeof(WorldGridExtension), nameof(WorldGridExtension.TraversalDistanceBetweenPatched));
			foreach (CodeInstruction instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Callvirt && (MethodInfo)instruction.operand == originalTraversalDistance)
				{
					// Extra argunment, this for launchable argument
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Call, patchedTraversalDistance);
				}
				else
				{
					yield return instruction;
				}
			}
		}
	}

	[HarmonyPatch(typeof(CompLaunchable), "ChoseWorldTarget", new Type[] { typeof(GlobalTargetInfo), typeof(PlanetTile), typeof(IEnumerable<IThingHolder>),
		typeof(int), typeof(Action<PlanetTile, TransportersArrivalAction>),  typeof(CompLaunchable), typeof(float?)})]
	public static class CannotLaunchToEnemyShip
	{
		// As transpilers are hard to read already, this one is intended copy-paste of the transpiler for CompLaunchable.TryLaunch above with a slight change in extra instuction,
		// in case of changes, obviously, both transpilers shoud be considered.
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo originalTraversalDistance = AccessTools.Method(typeof(WorldGrid), nameof(WorldGrid.TraversalDistanceBetween));
			MethodInfo patchedTraversalDistance = AccessTools.Method(typeof(WorldGridExtension), nameof(WorldGridExtension.TraversalDistanceBetweenPatched));
			foreach (CodeInstruction instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Callvirt && (MethodInfo)instruction.operand == originalTraversalDistance)
				{
					// Extra argunment for patched method 5 is the index of launchable argument
					yield return new CodeInstruction(OpCodes.Ldarg_S, 5);
					yield return new CodeInstruction(OpCodes.Call, patchedTraversalDistance);
				}
				else
				{
					yield return instruction;
				}
			}
		}
		public static bool Prefix(GlobalTargetInfo target, PlanetTile tile, Action<PlanetTile, TransportersArrivalAction> launchAction, CompLaunchable launchable,
			ref int maxLaunchDistance, float? overrideFuelLevel, ref bool __result)
		{
			return true;
		}
		public static void Postfix(GlobalTargetInfo target, PlanetTile tile, Action<PlanetTile, TransportersArrivalAction> launchAction, CompLaunchable launchable,
			int maxLaunchDistance, float? overrideFuelLevel, ref bool __result)
		{
			if (!__result)
			{
				return;
			}
			// Do not allow sednding Odyssey shuttle to enemy ship, bypassing PD
			MapParent worldObject;
			if (Find.WorldObjects.TryGetWorldObjectAt<MapParent>(tile, out worldObject))
			{
				if (worldObject is WorldObjectOrbitingShip ship &&
					ship.Map.GetComponent<ShipMapComp>().ShipMapState == ShipMapState.inCombat &&
					worldObject.Map != ShipInteriorMod2.FindPlayerShipMap())
				{
					Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.CantLaunchToEnemyMap"),
						MessageTypeDefOf.RejectInput, historical: false);
					__result = false;
				}
			}
		}
	}
}
