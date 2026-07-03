using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld.QuestGen;
using RimWorld.Planet;
using RimWorld;

namespace SaveOurShip2
{
	[StaticConstructorOnStartup]
	public static class QuestHandler_SoSStashedShip
	{
		static void BlacklistQuest()
		{
			// Blacklist quest on all layers to prevent it from randomly appearing
			QuestScriptDef quest = ResourceBank.QuestScriptDefOf.SoSStashedShipScript;
			if (quest.layerBlacklist == null)
			{
				quest.layerBlacklist = new List<PlanetLayerDef>();
			}
			foreach (PlanetLayerDef layerDef in DefDatabase<PlanetLayerDef>.AllDefs)
			{
				quest.layerBlacklist.Add(layerDef);
			}
		}
		static QuestHandler_SoSStashedShip()
		{
			// Just in case anyone changes planet layer defs, do blacklisting with a delay
			LongEventHandler.QueueLongEvent(BlacklistQuest, "PostGameStart", doAsynchronously: false, null);
		}
	}
}
