﻿using System.Collections.Generic;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	public class ShipGameComp : GameComponent
	{
		private Dictionary<int, bool> spaceMaps = new Dictionary<int, bool>();
		public HashSet<Thing> shuttleCache = new HashSet<Thing>();
		public List<ShipMapComp> shipHeatMapCompCache = new List<ShipMapComp>();

		public ShipGameComp(Game game)
		{
			AccessExtensions.Utility = this;
		}

		public bool this[Map map]
		{
			get
			{
				if (map == null) return false;
				if (spaceMaps.TryGetValue(map.uniqueID, out var space)) return space;

				var isSpace = map.Biome == ResourceBank.BiomeDefOf.OuterSpaceBiome;
				spaceMaps.Add(map.uniqueID, isSpace);
				return isSpace;
			}
		}

		public void RecacheSpaceMaps()
		{
			spaceMaps = new Dictionary<int, bool>();
		}
	}
}