﻿using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	class GenStep_BlackBoxMission : GenStep
	{

		public override int SeedPart
		{
			get
			{
				return 420666;
			}
		}

		public override void Generate(Map map, GenStepParams parms)
		{
			ResolveParams resolveParams = default(ResolveParams);
			resolveParams.rect = new CellRect(12, map.Size.z / 3, 1, 1);
			resolveParams.faction = Faction.OfAncientsHostile;
			BaseGen.globalSettings.map = map;
			BaseGen.symbolStack.Push("ship_blackboxmission", resolveParams);
			BaseGen.Generate();
		}
	}
}
