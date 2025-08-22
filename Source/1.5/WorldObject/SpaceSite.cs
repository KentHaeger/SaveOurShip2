﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace SaveOurShip2
{
	class SpaceSite : Site //legacy space sites using RW mapgen
	{
		public float theta;
		public float radius;
		public float phi;

		public static Vector3 orbitVec = new Vector3(0, 0, 1);
		public static Vector3 orbitVecPolar = new Vector3(0, 1, 0);

		public override Vector3 DrawPos
		{
			get
			{
				// Vector3 v = Vector3.SlerpUnclamped(orbitVec * radius, orbitVec * radius * -1, theta * -1);
				// Todo: y = phi*16 is a temporary fix to make space sites look spread around whitout bigger changes to coordinates code
				// return new Vector3(v.x, phi*16, v.z);
				return WorldObjectMath.GetPos(phi, theta, radius);
			}
		}

		public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
		{
			return new List<FloatMenuOption>();
		}

		[DebuggerHidden]
		public override IEnumerable<FloatMenuOption> GetTransportPodsFloatMenuOptions(IEnumerable<IThingHolder> pods, CompLaunchable representative)
		{
			return new List<FloatMenuOption>();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<float>(ref theta, "theta", 0, false);
			Scribe_Values.Look<float>(ref phi, "phi", 0, false);
			Scribe_Values.Look<float>(ref radius, "radius", 0f, false);
			const string newCoordsName = "newCoords";
			bool newCoords = true;
			Scribe_Values.Look<bool>(ref newCoords, newCoordsName, false, true);
			if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				if (!newCoords)
				{
					// Old coordinates were using *16 , also degre to radians change
					phi *= 16;
					phi /= 180f / Mathf.PI;
				}
			}
		}

		public override void Print(LayerSubMesh subMesh)
		{
			float averageTileSize = Find.WorldGrid.averageTileSize;
			WorldRendererUtility.PrintQuadTangentialToPlanet(this.DrawPos, 1.7f * averageTileSize, 0.008f, subMesh, false, false, true);
		}

		public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
		{
			alsoRemoveWorldObject = true;
			if (Find.World.worldObjects.AllWorldObjects.Any(ob => ob is TravelingTransportPods && ((int)typeof(TravelingTransportPods).GetField("initialTile", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ob) == this.Tile || ((TravelingTransportPods)ob).destinationTile == this.Tile)))
				return false;
			if (this.Map.listerBuildings.allBuildingsNonColonist.Any(t => t.TryGetComp<CompBlackBoxAI>() != null))
				return false;
			return base.ShouldRemoveMapNow(out alsoRemoveWorldObject);
		}

        public override void PostMapGenerate()
        {
            base.PostMapGenerate();
			FloodFillerFog.FloodUnfog(IntVec3.Zero, Map);

		}
    }
}
