using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
	public class ColorDefMatcher
	{
		private readonly Dictionary<(byte r, byte g, byte b), ColorDef> cache = new Dictionary<(byte, byte, byte), ColorDef>();
		public ColorDef GetNearestColorDef(Color color)
		{
			var key = ToKey(color);
			if (cache.TryGetValue(key, out ColorDef cached))
				return cached;

			ColorDef nearest = null;
			float nearestDistSqared = float.MaxValue;
			foreach (ColorDef colorDef in DefDatabase<ColorDef>.AllDefs)
			{
				float distSqared = ColorDistanceSqared(color, colorDef.color);
				if (distSqared < nearestDistSqared)
				{
					nearestDistSqared = distSqared;
					nearest = colorDef;
				}
			}

			cache[key] = nearest;
			return nearest;
		}

		public void ClearCache()
		{
			cache.Clear();
		}

		private static (byte r, byte g, byte b) ToKey(Color color)
		{
			// Lower color precision by setting low bytest 0 so that quering very similar
			// colors results in instanc cache match
			Color32 c = LimitColorBits(color);
			return (c.r, c.g, c.b);
		}

		private static float LimitcolorComponentBits(float component)
		{
			// To avoid excess resource use due to graphic data stored for one building too many times for different colors,
			// limit color precision so that long rgadient transitions don't spawn like 100+ color defs.
			// Bumber of bits that can be stripped is verified by visual review.
			byte byteColor = (byte)(component * byte.MaxValue);
			byteColor = (byte)(byteColor & ~0b111);
			return (float)byteColor / byte.MaxValue;
		}
		private static Color LimitColorBits(Color sourceColor)
		{
			return new Color(
				LimitcolorComponentBits(sourceColor.r),
				LimitcolorComponentBits(sourceColor.g),
				LimitcolorComponentBits(sourceColor.b),
				LimitcolorComponentBits(sourceColor.a));
		}

		/*private static float ColorDistanceSqared(Color a, Color b)
		{
			float dr = a.r - b.r;
			float dg = a.g - b.g;
			float db = a.b - b.b;
			return dr * dr + dg * dg + db * db;
		}*/

		private static float ColorDistanceSqared(Color a, Color b)
		{
			Color.RGBToHSV(a, out float ha, out float sa, out float va);
			Color.RGBToHSV(b, out float hb, out float sb, out float vb);

			float deltaHue = Mathf.Abs(ha - hb);
			deltaHue = Mathf.Min(deltaHue, 1f - deltaHue);
			float ds = sa - sb;
			float dv = va - vb;

			// Hue is meaningless at low saturation; weight it by the duller color's chroma.
			float deltaHueAdjusted = deltaHue * Mathf.Min(sa, sb);

			// Based on how it visually looks strange when colors in the middle of some gradient are picked from built-in palette 
			// if they math by equally-weighed hueAdjusted, S ands B. Hue matching should be really important.
			const float hueWeight = 2;

			return hueWeight * deltaHueAdjusted * deltaHueAdjusted + ds * ds + dv * dv;
		}

		private static void ToHSV(Color color, out float h, out float s, out float brightness)
		{
			Color.RGBToHSV(color, out h, out s, out brightness);
		}
	}
}
