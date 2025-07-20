﻿using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
	public class PlaceWorker_Radiator : PlaceWorker //only used for solar pannels now
	{
		public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing)
		{
			Map currentMap = Find.CurrentMap;
			IntVec3 loc1 = center + IntVec3.North.RotatedBy(rot); 
			IntVec3 loc2 = center + IntVec3.South.RotatedBy(rot);
			IntVec3 loc3 = center + (IntVec3.South.RotatedBy(rot) * 2);
			IntVec3 loc4 = center + (IntVec3.South.RotatedBy(rot) * 3);
			GenDraw.DrawFieldEdges(new List<IntVec3>()
			{
			loc1
			}, GenTemperature.ColorSpotCold);
			GenDraw.DrawFieldEdges(new List<IntVec3>()
			{
			loc2,loc3,loc4
			}, GenTemperature.ColorSpotHot);
			Room roomGroup1 = loc2.GetRoom(currentMap);
			Room roomGroup2 = loc1.GetRoom(currentMap);
			if (roomGroup1 == null || roomGroup2 == null)
				return;
			if (roomGroup1 == roomGroup2 && !roomGroup1.UsesOutdoorTemperature)
			{
				GenDraw.DrawFieldEdges(roomGroup1.Cells.ToList(), new Color(1f, 0.7f, 0.0f, 0.5f));
			}
			else
			{
				if (!roomGroup1.UsesOutdoorTemperature)
					GenDraw.DrawFieldEdges(roomGroup1.Cells.ToList(), GenTemperature.ColorRoomHot);
				if (roomGroup2.UsesOutdoorTemperature)
					return;
				GenDraw.DrawFieldEdges(roomGroup2.Cells.ToList(), GenTemperature.ColorRoomCold);
			}
		}

		public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{
			for (int i = 1; i < 7; i++)
			{
				IntVec3 loc2 = center + (IntVec3.South.RotatedBy(rot) * i);
				if (!loc2.InBounds(map))
					return false;
				if (i<4 && loc2.Impassable(map))
					return TranslatorFormattedStringExtensions.Translate("SoS.UnfoldBlocked");
				Building b = loc2.GetFirstBuilding(Find.CurrentMap);
				if (b !=null && (b.def == ResourceBank.ThingDefOf.ShipInside_SolarGenerator) && b.Rotation == rot.Opposite)
					return TranslatorFormattedStringExtensions.Translate("SoS.UnfoldBlocked");
			}
			return true;
		}
	}
}
