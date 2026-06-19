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
	public static class ShipImporter
	{
		private static HashSet<IntVec3> cellsToDesignate;
		private static HashSet<IntVec3> floorCells;

		private static bool IsRed(Color color)
		{
			return color.r > 0.98 && color.g < 0.02 && color.b < 0.02;
		}

		private static bool IsGreen(Color color)
		{
			return color.r < 0.02 && color.g > 0.98 && color.b < 0.02;
		}

		private static bool PickFlippedCornerAt(IntVec3 pos, Map map)
        {
			int xSign = Math.Sign(pos.x - map.Size.x / 2);
			int zSign = Math.Sign(pos.z - map.Size.z / 2);
			return xSign * zSign > 0;
		}

		private static bool IsCornerHere(IntVec3 pos, int cornerLength, out Rot4 cornerRotation, out bool isFlipped)
		{
			cornerRotation = Rot4.Invalid;
			isFlipped = false;
			foreach (bool flipped in new List<bool>() { false, true })
			{
				foreach (Rot4 rot in Rot4.AllRotations)
				{
					bool cornerHere = true;
					Rot4 rotWithFlip = flipped ? rot.Opposite : rot;
					// Check that corner is going to be placed in unoccupied tiles 
					for (int offset = 0; offset < cornerLength; offset++)
					{
						IntVec3 current = pos + rotWithFlip.AsIntVec3 * offset;
						if (cellsToDesignate.Contains(current) || floorCells.Contains(current))
						{
							cornerHere = false;
						}
					}
					// Check corner base
					IntVec3 cornerBase = pos + rotWithFlip.Opposite.AsIntVec3;
					if (!cellsToDesignate.Contains(cornerBase))
					{
						cornerHere = false;
					}
					// Check corner side
					for (int offset = 0; offset < cornerLength; offset++)
					{
						RotationDirection rotDir = flipped ? RotationDirection.Counterclockwise : RotationDirection.Clockwise;
						IntVec3 sideWall = pos + rotWithFlip.AsIntVec3 * offset + rotWithFlip.Rotated(rotDir).AsIntVec3;
						if (!cellsToDesignate.Contains(sideWall))
						{
							cornerHere = false;
						}
					}
					// If all checks for corner successfully passed
					if (cornerHere)
					{
						cornerRotation = rot;
						isFlipped = flipped;
						return true;
					}
				}
			}
			return false;
		}

		private static void SpawnAndSetFaction(ThingDef def, IntVec3 position, Map map, Rot4 rot)
        {
			Thing thing = GenSpawn.Spawn(def, position, map, rot);
			thing.factionInt = Faction.OfPlayer;
        }

		private static IntVec3 AdjustSpawnPos(IntVec3 origin, Rot4 rot, int length, bool isFlipped)
        {
			// Intended integer / 2
			Rot4 finalRot = isFlipped ? rot.Opposite : rot;
			// This particual corner doesn't fit general rule
			IntVec3 offset = IntVec3.Zero;
			if (length == 2 && isFlipped == true)
            {
				offset = finalRot.AsIntVec3;
			}
			return origin + finalRot.AsIntVec3 * (Mathf.Max(0, length - 1) / 2) + offset;
		}
		public static void ImportShipDesign(bool placeActualBuildings, bool useNanameWalls = false)
		{
			Map map = Find.CurrentMap;
			if (map == null)
			{
				Messages.Message(ResourceBank.noMapMessage, null, MessageTypeDefOf.NeutralEvent);
				return;
			}

			if (useNanameWalls && DefDatabase<ThingDef>.GetNamedSilentFail("Ship_Beam_NAWDiagonal") == null)
            {
				Messages.Message("Naname ship hull def not found", null, MessageTypeDefOf.NeutralEvent);
				return;
			}
			Texture2D tex = TextureUtility.LoadTextureFromSos2Folder("Blueprint.png", useBuiltInTexture:false);
			if (tex == null)
            {
				return;
            }
			cellsToDesignate = new HashSet<IntVec3>();
			floorCells = new HashSet<IntVec3>();

			for (int x = 0; x < Mathf.Min(map.Size.x, tex.width); x++)
			{
				for (int z = 0; z < Mathf.Min(map.Size.z, tex.height); z++)
				{
					Color color = tex.GetPixel(x, z);
					if (IsRed(color))
					{
						cellsToDesignate.Add(new IntVec3(x, 0, z));
					}
					if (IsGreen(color))
					{
						floorCells.Add(new IntVec3(x, 0, z));
					}
				}
			}
			if (placeActualBuildings)
            {
				foreach (IntVec3 item in cellsToDesignate)
				{
					try
					{
						if (map.thingGrid.ThingAt(item, ThingCategory.Building) == null)
						{
							if (!useNanameWalls)
							{
								SpawnAndSetFaction(ThingDefOf.Ship_Beam, item, map, Rot4.North);
							}
                            else
                            {
								SpawnAndSetFaction(ThingDef.Named("Ship_Beam_NAWDiagonal"), item, map, Rot4.North);
							}
						}
						if(!useNanameWalls)
                        {
							// Valid locations for corners are adjacent to walls, not any tile
							foreach (IntVec3 cornerBase in GenAdj.CellsAdjacentCardinal(item, Rot4.North, new IntVec2(1, 1)))
							{
								Rot4 cornerRotaion;
								bool isFlipped;
								// Check curent cell for placing 3-length corners, then 2-length corners, then 1x1 corner
								if (IsCornerHere(cornerBase, 3, out cornerRotaion, out isFlipped))
								{
									if (!isFlipped)
									{
										IntVec3 spawnPos = AdjustSpawnPos(cornerBase, cornerRotaion, 3, isFlipped);
										SpawnAndSetFaction(ThingDef.Named("Ship_Corner_OneThree"), spawnPos, map, cornerRotaion);
									}
                                    else 
									{
										IntVec3 spawnPos = AdjustSpawnPos(cornerBase, cornerRotaion, 3, isFlipped);
										SpawnAndSetFaction(ThingDef.Named("Ship_Corner_OneThreeFlip"), spawnPos, map, cornerRotaion);
									}
								}
								else if (IsCornerHere(cornerBase, 2, out cornerRotaion, out isFlipped))
								{
									if (!isFlipped)
									{
										IntVec3 spawnPos = AdjustSpawnPos(cornerBase, cornerRotaion, 2, isFlipped);
										SpawnAndSetFaction(ThingDef.Named("Ship_Corner_OneTwo"), spawnPos, map, cornerRotaion);
									}
									else
									{
										IntVec3 spawnPos = AdjustSpawnPos(cornerBase, cornerRotaion, 2, isFlipped);
										SpawnAndSetFaction(ThingDef.Named("Ship_Corner_OneTwoFlip"), spawnPos, map, cornerRotaion);
									}
								}
								else if (IsCornerHere(cornerBase, 1, out cornerRotaion, out isFlipped))
								{
									IntVec3 spawnPos = AdjustSpawnPos(cornerBase, cornerRotaion, 1, isFlipped);
									string cornerDef = "Ship_Corner_OneOne";
									if (PickFlippedCornerAt(spawnPos, map))
									{
										cornerDef = "Ship_Corner_OneOneFlip";
										cornerRotaion.Rotate(RotationDirection.Counterclockwise);
									}
									SpawnAndSetFaction(ThingDef.Named(cornerDef), spawnPos, map, cornerRotaion);
								}
							}
						}
					}
					catch (Exception e)
					{
						// if parts of the blueprint from external file failed to spawn, that isn't  considered major issue
						Log.Warning(e.Message);
					}
				}
				foreach (IntVec3 item in floorCells)
				{
					try
					{
						if (map.thingGrid.ThingAt(item, ThingCategory.Building) == null)
						{
							SpawnAndSetFaction(ResourceBank.ThingDefOf.ShipHullTile, item, map, Rot4.North);
						}
					}
					catch (Exception e)
					{
						Log.Warning(e.Message);
					}
				}
			}
            else
            {
				Designator_Plan_Add adder = new Designator_Plan_Add();
				adder.PlanCells(cellsToDesignate);
			}
		}
	}
}
