using RimWorld;
using RimWorld.Planet;
using System;

namespace SaveOurShip2
{
	public class BiomeWorker_OuterSpace : BiomeWorker
	{

		public override float GetScore(BiomeDef biome, Tile tile, PlanetTile planetTile)
		{
			return -999f;
		}
	}
}
