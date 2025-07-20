using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
	class PlaceWorker_ShipEngine : PlaceWorker
	{
		public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{
			if (ShipInteriorMod2.HasSoS2CK)
				return AcceptanceReport.WasAccepted;
			CompEngineTrail engineprev = null;
			var mapComp = map.GetComponent<ShipMapComp>();
			if (mapComp.ShipsOnMap.Values.Any(s => s.Engines.Any()))
			{
				//prefer player owned non wreck ships
				if (mapComp.ShipsOnMap.Values.Any(s => s.Engines.Any() && !s.IsWreck && s.Faction == Faction.OfPlayer))
					engineprev = mapComp.ShipsOnMap.Values.Where(s => s.Engines.Any() && !s.IsWreck && s.Faction == Faction.OfPlayer).First().Engines.First();
				else if (mapComp.ShipsOnMap.Values.Any(s => s.Engines.Any()))
					engineprev = mapComp.ShipsOnMap.Values.First(s => s.Engines.Any()).Engines.First();
			}
			if (engineprev != null && engineprev.parent.Rotation != rot)
				return (AcceptanceReport)TranslatorFormattedStringExtensions.Translate("SoS.EnginePlaceRotation");
			return AcceptanceReport.WasAccepted;
		}
		public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
		{
			List<IntVec3> rect = GenAdj.CellsOccupiedBy(center, rot, def.size).ToList();
			int minx = 99999;
			int minz = 99999;
			foreach (IntVec3 vec in rect)
			{
				if (vec.x < minx)
					minx = vec.x;
				if (vec.z < minz)
					minz = vec.z;
			}

			CellRect occupiedRect = GenAdj.OccupiedRect(center, rot, def.size);
			CompProps_EngineTrail compProps = def.GetCompProperties<CompProps_EngineTrail>();
			CellRect rectToKill = GenAdjExtension.GetDirectAdjacentRect(center.ToIntVec2, rot.Rotated(RotationDirection.Opposite).rotInt, occupiedRect,
				compProps.killZoneWidth, compProps.killZoneLength, compProps.killZoneExtraOffset);
			GenDraw.DrawFieldEdges(rectToKill.Cells.ToList(), Color.red);
		}
	}
}
