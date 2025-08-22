using RimWorld;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
	public class GenAdjExtension
	{
		public static CellRect GetDirectAdjacentRect(IntVec2 center, byte rotation, CellRect occupiedRect, int width, int length, int extraOffset)
		{
			IntVec2 newCenter = center;
			// This handles even-width rects
			int minXDecrement = 0, maxXIncrement = 0, minZDecrement = 0, maxZIncrement = 0;
			switch (rotation)
			{
				case Rot4.NorthInt:
					newCenter.z += 1 + occupiedRect.maxZ - center.z + (length - 1) / 2 + extraOffset;
					if (width % 2 == 0)
					{
						minXDecrement = 1;
					}
					break;
				case Rot4.EastInt:
					newCenter.x += 1 + occupiedRect.maxX - center.x + (length - 1) / 2 + extraOffset;
					if (width % 2 == 0)
					{
						maxZIncrement = 1;
					}
					break;
				case Rot4.SouthInt:
					newCenter.z -= 1 + center.z - occupiedRect.minZ + (length - 1) / 2 + extraOffset;
					if (width % 2 == 0)
					{
						maxXIncrement = 1;
					}
					break;
				case Rot4.WestInt:
					newCenter.x -= 1 + center.x - occupiedRect.minX + (length - 1) / 2 + extraOffset;
					if (width % 2 == 0)
					{
						minZDecrement = 1;
					}
					break;
			}
			CellRect result = CellRect.SingleCell(newCenter.ToIntVec3);
			// Given width and height are relative to rotation, convert to widh and height in actuall coordinates
			int actualWidth, actualHeight;
			if (rotation == Rot4.NorthInt || rotation == Rot4.SouthInt)
			{
				actualWidth = width;
				actualHeight = length;
			}
			else
			{
				actualWidth = length;
				actualHeight = width;

			}
			result.minX -= (actualWidth - 1) / 2 + minXDecrement;
			result.maxX += (actualWidth - 1) / 2 + maxXIncrement;
			result.minZ -= (actualHeight - 1) / 2 + minZDecrement;
			result.maxZ += (actualHeight - 1) / 2 + maxZIncrement;
			return result;
		}
	}
}

