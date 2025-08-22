﻿using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;

namespace SaveOurShip2
{
	public class WorldObjectCompProps_TimedForcedExitShip : WorldObjectCompProperties
	{
		public WorldObjectCompProps_TimedForcedExitShip()
		{
			this.compClass = typeof(TimedForcedExitShip);
		}

		public override IEnumerable<string> ConfigErrors(WorldObjectDef parentDef)
		{
			foreach (string text in base.ConfigErrors(parentDef))
			{
				yield return text;
			}
			
			if (!typeof(MapParent).IsAssignableFrom(parentDef.worldObjectClass))
			{
				yield return parentDef.defName + " has WorldObjectCompProperties_TimedForcedExit but it's not MapParent.";
			}
			yield break;
		}
	}
}
