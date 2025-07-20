﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	class CompExplosivePlant : CompExplosive
	{
		public override void CompTick()
		{
			if (Find.TickManager.TicksGame % 2000 == 0)
				parent.TickLong();
		}
	}
}
