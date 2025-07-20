using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SaveOurShip2
{
    public class HediffComp_GearRepair : HediffComp
    {
        public HediffCompProperties_GearRepair Props => props as HediffCompProperties_GearRepair;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if(parent.pawn.IsHashIntervalTick(Props.ticksPerRepair))
            {
                if (Rand.Chance(0.25f))
                {
                    if (!TryFixWeapon())
                        TryFixGear();
                }
                else
                {
                    if (!TryFixGear())
                        TryFixWeapon();
                }
            }
        }

        bool TryFixWeapon()
        {
            foreach(Thing thing in parent.pawn.equipment.AllEquipmentListForReading.InRandomOrder())
            {
                if(thing.def.useHitPoints && thing.HitPoints < thing.MaxHitPoints)
                {
                    thing.HitPoints++;
                    return true;
                }
            }
            return false;
        }

        bool TryFixGear()
        {
            foreach (Thing thing in parent.pawn.apparel.WornApparel.InRandomOrder())
            {
                if (thing.def.useHitPoints && thing.HitPoints < thing.MaxHitPoints)
                {
                    thing.HitPoints++;
                    return true;
                }
            }
            return false;
        }
    }
}
