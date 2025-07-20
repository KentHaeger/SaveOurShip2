using RimWorld.Planet;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
	// Since Space Site and orbiting ship inherit from different base game objects, they got tho have their common math in utility class, not common parent
	static class WorldObjectMath
	{
		public const int defaultRadius = 150;
		public static Vector3 vecEquator = new Vector3(0, 0, 1);
		public static Vector3 GetPos(float phi, float theta, float radius)
		{
			phi = Mathf.Clamp(phi, -Mathf.PI / 2, Mathf.PI / 2);
			float y = radius * Mathf.Sin(phi);
			float projectionRadius = radius * Mathf.Cos(phi);
			// Projection to equatorial plane
			Vector3 vPlanar = Vector3.SlerpUnclamped(vecEquator * projectionRadius, vecEquator * projectionRadius * -1, theta * -1);
			return new Vector3(vPlanar.x, y, vPlanar.z);
		}

		// Uniform coords getting for both space site and player ship
		public static void GetSphericalCoords(MapParent obj, out float phi, out float theta, out float radius)
		{
			if (obj is WorldObjectOrbitingShip ship)
			{
				Log.Warning("Graveyard for ship");
				phi = ship.Phi;
				theta = ship.Theta;
				radius = ship.Radius;
			}
			else if (obj is SpaceSite site)
			{
				Log.Warning("Graveyard for site"); 
				phi = site.phi;
				theta = site.theta;
				radius = site.radius;
			}
			else
			{
				phi = 0;
				theta = 0;
				radius = defaultRadius;
				Log.ErrorOnce("SoOS2: Failed to get coordinates for object of type:" + obj.GetType().Name, 104857937);
			}
		}
	}
}
