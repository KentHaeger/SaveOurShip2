using RimWorld;
using System;
using Verse;

namespace SaveOurShip2
{
	public class IncidentWorker_ShipCombat : IncidentWorker
	{
		protected override bool CanFireNowSub(IncidentParms parms)
		{
			Map map = (Map)parms.target;
			var mapComp = map.GetComponent<ShipMapComp>();
			if (!mapComp.IsPlayerShipMap || mapComp.ShipMapState != ShipMapState.nominal || mapComp.NextTargetMap != null ||
				ModSettings_SoS.frequencySoS == 0 || !mapComp.ShipBattleMandatoryIntervalExpired())
				return false;

			// Can't be attacked if just got to space, for interoperability with Odyssey
			if (Find.TickManager.TicksGame - map.generationTick < GenDate.TicksPerHour * 3)
            {
				return false;
            }

			if(mapComp.GetActiveCloak() != null)
			{
				return false;
			}
			return true;
		}

		protected override bool TryExecuteWorker(IncidentParms parms)
		{
			var mapComp = ((Map)parms.target).GetComponent<ShipMapComp>();
			mapComp.LastAttackTick = Find.TickManager.TicksGame;
			Log.Message("Ship battle starting, storyteller incident");
			mapComp.StartShipEncounter(fac: parms.faction);
			return true;
		}
	}
}
