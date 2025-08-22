﻿using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SaveOurShip2
{
    class CompCryptoCocoon : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (parent.pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Malnutrition) != null)
                return;
            Building_CryptosleepCasket cocoon = ThingMaker.MakeThing(ResourceBank.ThingDefOf.SoS2CryptosleepCocoon) as Building_CryptosleepCasket;
            if (GenPlace.TryPlaceThing(cocoon, target.Cell, parent.pawn.Map, ThingPlaceMode.Near))
            {
                cocoon.SetFaction(Faction.OfPlayer);
                FilthMaker.TryMakeFilth(target.Cell, parent.pawn.Map, ThingDefOf.Filth_Slime, Rand.Range(8, 12));
                parent.pawn.DeSpawn();
                if (parent.pawn.holdingOwner != null)
                    parent.pawn.holdingOwner.TryTransferToContainer(parent.pawn, cocoon.innerContainer);
                else
                    cocoon.TryAcceptThing(parent.pawn);
                cocoon.innerContainer.TryAdd(parent.pawn);
                cocoon.contentsKnown = true;
                if (parent.pawn.needs.food != null)
                {
                    parent.pawn.health.AddHediff(HediffDefOf.Malnutrition).Severity = 0.5f - (0.25f * parent.pawn.needs.food.CurLevel);
                    parent.pawn.needs.food.CurLevel = 0.1f;
                }
            }
            else
                Messages.Message("SoSCantPlaceCocoon".Translate(), target.Thing, MessageTypeDefOf.CautionInput);
        }

        public override bool GizmoDisabled(out string reason)
        {
            if (parent.pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Malnutrition) != null)
            {
                reason = "SoSAlreadyMalnourished".Translate();
                return true;
            }
            reason = null;
            return false;
        }
    }
}
