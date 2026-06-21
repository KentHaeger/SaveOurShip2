using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using RimWorld;
using Verse;
using LudeonTK;

namespace SaveOurShip2
{
	public static class ShipPainter
	{
		public static void PaintFloorAndBuildings(Map map, bool useBuiltInTexture)
        {
			if (map == null)
			{
				Log.Error(ResourceBank.noMapMessage);
				return;
			}
			// This is a demo of a featuyre, so it just uses specific textures
			const string floorColorPNG = "ColorFloor.png";
			const string wallColorPNG = "ColorWall.png";
			Texture2D texFloor = TextureUtility.LoadTextureFromSos2Folder(floorColorPNG, useBuiltInTexture);
			Texture2D texWall = TextureUtility.LoadTextureFromSos2Folder(wallColorPNG, useBuiltInTexture);
			if (texFloor == null || texWall == null)
			{
				return;
			}

			ColorDefMatcher colorDefMatcher = new ColorDefMatcher();
			int processedBuildings = 0;
			for (int x = 0; x < Mathf.Min(map.Size.x, texFloor.width, texWall.width); x++)
			{
				for (int z = 0; z < Mathf.Min(map.Size.z, texFloor.height, texFloor.height); z++)
				{
					Color floorColor = texFloor.GetPixel(x, z); 
					Color wallColor = texWall.GetPixel(x, z);
					IntVec3 tile = new IntVec3(x, 0, z);
					bool hasPlating;
					bool hasBay;
					PlaceWorker_OnShipHull.HasPlatingAndRestrictedBayFor(null, tile, map, out hasPlating, out hasBay);
					if (hasPlating)
					{
						map.terrainGrid.SetTerrain(tile, TerrainDefOf.MetalTile);
						ColorDef colorDef = colorDefMatcher.GetNearestColorDef(floorColor);
						ColorDef databaseColor = DefDatabase<ColorDef>.GetNamedSilentFail(colorDef.defName);
						if (databaseColor == null)
                        {
							DefDatabase<ColorDef>.Add(colorDef);
							databaseColor = DefDatabase<ColorDef>.GetNamed(colorDef.defName);
						}
						map.terrainGrid.SetTerrainColor(tile, databaseColor);
					}
					foreach (Thing t in tile.GetThingList(map))
					{
						if (t is Building b)
						{
							if (ResourceBank.CornerDefs.Contains(b.def) || ResourceBank.HullDefs.Contains(b.def) ||
								ResourceBank.AutoPaintableBuildingDefs.Contains(b.def))
                            {
								processedBuildings++;
								ColorDef colorDef = colorDefMatcher.GetNearestColorDef(wallColor);
								ColorDef databaseColor = DefDatabase<ColorDef>.GetNamedSilentFail(colorDef.defName);
								if (databaseColor == null)
								{
									DefDatabase<ColorDef>.Add(colorDef);
									databaseColor = DefDatabase<ColorDef>.GetNamed(colorDef.defName);
								}
								b.ChangePaint(databaseColor);
							}
						}
					}
				}
			}
			Log.Message($"SoS 2: Processed buildings for coloring: {processedBuildings}");
			map.mapDrawer.WholeMapChanged(MapMeshFlagDefOf.Buildings);
			map.mapDrawer.WholeMapChanged(MapMeshFlagDefOf.Things);
			map.mapDrawer.RegenerateEverythingNow();
			
		}
	}
}
