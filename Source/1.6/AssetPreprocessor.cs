using System.IO;
using RimWorld;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
	[StaticConstructorOnStartup]
	public static class AssetPreprocessor
	{
		static AssetPreprocessor()
		{
			ProcessCornerDefs();
		}

		private static void ProcessCornerDefs()
		{
			// Corner items have damage graphics set for all corners and sides. However, it's better to not have damage graphics in the empty part of the corner.
			// Would be too tedious to do it in XML for 3 corners, x2 variants of flips x3 variants of style
			ModContentPack content = LoadedModManager.GetMod<ShipInteriorMod2>().Content;
			const float alphaThreshold = 0.1f;
			const int delta = 4;
			foreach (ThingDef corner in ResourceBank.CornerDefs)
			{	
				string texPath = corner.graphicData?.texPath;
				if (texPath.NullOrEmpty() || corner.graphicData.damageData == null)
				{
					continue;
				}
				if (!TryLoadReadableTexture(content, texPath, out Texture2D tex))
				{
					Log.Error($"SoS 2: error when loading corner texture as readable: { corner.defName }");
					continue;
				}
				int handledCornersCount = 0;
				Color topLeft = tex.GetPixelsAroundAveraged(delta, tex.height - 1 - delta);
				if (topLeft.a < alphaThreshold)
				{
					corner.graphicData.damageData.cornerTLMat = null;
					// For the corner that has top left empty and sharp ends at top right and bottom left,
					// remove damage graphics from those 2 sharp corners too as corner damage graphics have 
					// quite long sides and will be drawn over empty/transparent part of the texture.
					corner.graphicData.damageData.cornerTRMat = null;
					corner.graphicData.damageData.cornerBLMat = null;
					corner.graphicData.damageData.edgeTopMat = null;
					corner.graphicData.damageData.edgeLeftMat = null;
					handledCornersCount++;
				}
				// Handle other corners similar to top left
				Color bottomLeft = tex.GetPixelsAroundAveraged(delta, delta);
				if (bottomLeft.a < alphaThreshold)
				{
					corner.graphicData.damageData.cornerBLMat = null;
					corner.graphicData.damageData.cornerTLMat = null;
					corner.graphicData.damageData.cornerBRMat = null;
					corner.graphicData.damageData.edgeBotMat = null;
					corner.graphicData.damageData.edgeLeftMat = null;
					handledCornersCount++;
				}
				Color topRight = tex.GetPixelsAroundAveraged(tex.width - 1 - delta, tex.height - 1 - delta);
				if (topRight.a < alphaThreshold)
				{
					corner.graphicData.damageData.cornerTRMat = null;
					corner.graphicData.damageData.cornerBRMat = null;
					corner.graphicData.damageData.cornerTLMat = null;
					corner.graphicData.damageData.edgeTopMat = null;
					corner.graphicData.damageData.edgeRightMat = null;
					handledCornersCount++;
				}
				Color bottomRight = tex.GetPixelsAroundAveraged(tex.width - 1 - delta, delta);
				if (bottomRight.a < alphaThreshold)
				{
					corner.graphicData.damageData.cornerBRMat = null;
					corner.graphicData.damageData.cornerTRMat = null;
					corner.graphicData.damageData.cornerBLMat = null;
					corner.graphicData.damageData.edgeBotMat = null;
					corner.graphicData.damageData.edgeRightMat = null;
					handledCornersCount++;
				}
				if(handledCornersCount != 1)
				{
					Log.Error($"SoS 2: Error preprocessing corner damage graphics for: {corner.defName} { handledCornersCount }");
				}
				Object.Destroy(tex);
			}
		}
		static bool TryLoadReadableTexture(ModContentPack content, string texPath, out Texture2D tex)
		{
			tex = null;
			string file = Path.Combine(content.RootDir, "Textures", texPath + ".png");
			if (!File.Exists(file))
				return false;
			tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
			return tex.LoadImage(File.ReadAllBytes(file));
		}

		static Color GetPixelsAroundAveraged(this Texture2D tex, int x, int y)
		{
			Color result = Color.clear;
			int delta = 1;
			for (int dx = -delta; dx <= delta; dx++)
			{
				for(int dy  = -delta; dy <= delta; dy++)
				{
					result += tex.GetPixel(x + dx, y + dy);
				}
			}
			float sampleSize = (float)(delta * 2 + 1);
			return result / sampleSize / sampleSize;
		}
	}
}
