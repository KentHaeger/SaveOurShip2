﻿using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI.Group;
using RimWorld;

namespace SaveOurShip2
{
	class GenStep_MoonPillarSite : GenStep_Scatterer
	{
		private static readonly IntRange SettlementSizeRange = new IntRange(50, 69);

		public override int SeedPart
		{
			get
			{
				return 666133769;
			}
		}

		protected override bool CanScatterAt(IntVec3 c, Map map)
		{
			return true;
		}

		protected override void ScatterAt(IntVec3 c, Map map, GenStepParams stepparams, int stackCount = 1)
		{
			TerrainDef moonTerrain = ThingDef.Named("Marble").building.naturalTerrain;
			foreach(IntVec3 cell in map.AllCells)
			{
				map.terrainGrid.SetTerrain(cell, moonTerrain);
			}
			Lord defendShip = LordMaker.MakeNewLord(Faction.OfMechanoids, new LordJob_DefendShip(Faction.OfMechanoids, map.Center), map);
			List<Building> cores = new List<Building>();
			ShipInteriorMod2.GenerateShip(DefDatabase<ShipDef>.GetNamed("MechanoidMoonBase"), map, null, Faction.OfMechanoids, defendShip, out cores);
		}
	}
}
