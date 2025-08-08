using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Vehicles;
using SmashTools;

namespace SaveOurShip2
{
	public static class SOS2MapUtility
	{
		public static bool AnyVehiclePreventsMapRemoval(Map map)
		{
			if(MapHelper.AnyVehicleSkyfallersBlockingMap(map) ||
				MapHelper.AnyAerialVehiclesInRecon(map))
			{
				return true;
			}
			foreach (VehiclePawn vehicle in map.GetDetachedMapComponent<VehiclePositionManager>().AllClaimants)
			{
				if (vehicle.MovementPermissions.HasFlag(VehiclePermissions.Autonomous))
				{
					return true;
				}

				foreach (Pawn passenger in vehicle.AllPawnsAboard)
				{
					if (MapPawns.IsValidColonyPawn(passenger))
					{
						return true;
					}
				}
			}
			return false;
		}

		public static void TryLinkMapToWorldObject(Map map, int tile)
		{
			// For now, issue was found with Escape Ship map due to that map not being linked to world object
			// So, fixing onlyy that case for now
			WorldObject worldObject = Find.WorldObjects.ObjectsAt(tile).FirstOrDefault(t => true);
			if (worldObject != null && worldObject.Faction != Faction.OfPlayer)
			{
				// Link map to Escap ship object sho that it gets "Home" icon and when selected on world map, there is Abadon option 
				map.info.parent = (MapParent)worldObject;
				worldObject.SetFaction(Faction.OfPlayer);
			}
		}
	}
}

