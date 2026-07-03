using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Verse;

namespace SaveOurShip2
{
	public static class AccessExtensions
	{
		public static ShipGameComp Utility;

        public static bool IsSpace(this Map map)
		{
			return Utility[map];
		}

		public static bool IsSpaceMapParent(this MapParent mapParent)
		{
			return mapParent is WorldObjectOrbitingShip || mapParent is SpaceSite || mapParent is MoonBase;
		}

        // For performance optimization, normally use IsSpace
        public static bool IsKnownMap(this Map map)
        {
            return Utility.IsKnownMap(map);
        }

		public static bool IsKnownMapInSpace(this Map map)
		{
			return Utility.IsKnownMapInSpace(map);
		}

		public static Thing GetThingOfDefAt(this Map map, IntVec3 cell, IEnumerable<ThingDef> thingDefs)
		{
			foreach (Thing t in map.thingGrid.ThingsAt(cell))
			{
				if (thingDefs.Contains(t.def))
				{
					return t;
				}
			}
			return null;
		}
		public static Thing GetThingOfDefAt(this Map map, IntVec3 cell, ThingDef thingDef)
		{
			return GetThingOfDefAt(map, cell, new List<ThingDef>() { thingDef });
		}

		public static string GetNameForLogs(this Map map)
		{
			ShipMapComp mapComp = map.GetComponent<ShipMapComp>(); 
			if (map == ShipInteriorMod2.FindPlayerShipMap())
			{
				return "player map";
			}
			else if (map == ShipInteriorMod2.FindEnemyShipMap())
			{
				return "enemy map";
			}
			else if (mapComp.ShipMapState == ShipMapState.isGraveyard)
			{
				return "graveyard map";
			}
			else
			{
				return "other map";
			}

		}
		public static float DecompressionResistance(this Pawn pawn)
		{
			float resistance = pawn.GetStatValue(ResourceBank.StatDefOf.DecompressionResistance);
			resistance += pawn.CurrentBed()?.GetStatValue(ResourceBank.StatDefOf.DecompressionResistanceOffset) ?? 0.0f;
			return Mathf.Clamp(resistance, 0.0f, 1.0f);
		}

		public static float HypoxiaResistance(this Pawn pawn)
		{
			float resistance = pawn.GetStatValue(ResourceBank.StatDefOf.HypoxiaResistance);
			resistance += pawn.CurrentBed()?.GetStatValue(ResourceBank.StatDefOf.HypoxiaResistanceOffset) ?? 0.0f;
			return Mathf.Clamp(resistance, 0.0f, 1.0f);
		}

		public static bool CanSurviveVacuum(this Pawn pawn)
		{
			return (pawn.DecompressionResistance() >= 1.0f && pawn.HypoxiaResistance() >= 1.0f) || pawn is VehiclePawn;
		}
	}
}

