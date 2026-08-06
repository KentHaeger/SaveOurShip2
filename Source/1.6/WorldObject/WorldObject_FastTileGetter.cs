using RimWorld.Planet;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
	// There are different SOS 2 world objects, that all need this fast tile getter for Odyssey shuttle launch distance calculation.
	// So it is implemented as a delegate to avoid any issues with persistence.
	public class WorldObject_FastTileGetter
	{
		private float cachedPhi;
		private float cachedTheta;
		private float cachedRadius = -1f;
		private PlanetTile cachedTile;

		PlanetTile GetClosesTileImpl(float phi, float theta, float radius, WorldObject worldObject)
		{
			if (Mathf.Approximately(phi, cachedPhi) && Mathf.Approximately(theta, cachedTheta) && Mathf.Approximately(radius, cachedRadius))
			{
				return cachedTile;
			}
			cachedPhi = phi;
			cachedTheta = theta;
			cachedRadius = radius;
			Vector3 worldObjectPos = worldObject.DrawPos;
			float bestDistance = float.MaxValue;
			// TODO: using wave algortinm can decrease time from O(N of tiles) to O(kinda sqrt(N)) when there is time for that.
			foreach (PlanetLayer layer in Find.WorldGrid.PlanetLayers.Values)
			{
				for (int i = 0; i < layer.TilesCount; i++)
				{
					Vector3 tileCenter = layer.GetTileCenter(i);
					float currentDistance = (tileCenter - worldObjectPos).sqrMagnitude;
					if (currentDistance < bestDistance)
					{
						bestDistance = currentDistance;
						cachedTile = layer.Tiles[i].tile;
					}
				}
			}
			return cachedTile;
		}

		public static PlanetTile GetClosesTileFor(WorldObject worldObject)
		{
			if (worldObject is WorldObjectOrbitingShip ship)
			{
				return ship.fastTileGetter.GetClosesTileImpl(ship.Phi, ship.Theta, ship.Radius, ship);
			}
			else if (worldObject is SpaceSite site)
			{
				return site.fastTileGetter.GetClosesTileImpl(site.phi, site.theta, site.radius, site);
			}
			else if (worldObject is MoonBase moon)
			{
				return moon.fastTileGetter.GetClosesTileImpl(moon.phi, moon.theta, moon.radius, moon);
			}
			// returning actual tile is more failsafe than returning Invalid
			return worldObject.tile;
		}
	}
}
