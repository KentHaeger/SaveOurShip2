﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
	class Graphic_ShipHeat_Overlay : Graphic_Linked
	{
		public Graphic_ShipHeat_Overlay(Graphic subGraphic)
		{
			this.subGraphic = subGraphic;
		}

		public override bool ShouldLinkWith(IntVec3 c, Thing parent)
		{
			if(GenGrid.InBounds(c, parent.Map))
			{
				return parent.Map.GetComponent<ShipMapComp>().grid[parent.Map.cellIndices.CellToIndex(c)] != -1;
			}
			return false;
		}

		public override void Print(SectionLayer layer, Thing parent, float extraRotation)
		{
			CellRect val = GenAdj.OccupiedRect(parent);
			foreach(IntVec3 cell in val.Cells)
			{
				Vector3 vector = cell.ToVector3ShiftedWithAltitude(AltitudeLayer.WorldClipper);
				Printer_Plane.PrintPlane(layer, vector, Vector2.one, LinkedDrawMatFrom(parent, cell), 0f, false, null, null, 0.01f, 0f);
			}
		}
	}
}
