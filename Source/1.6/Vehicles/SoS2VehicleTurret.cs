using RimWorld;
using SmashTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Vehicles;
using Verse;

namespace SaveOurShip2.Vehicles
{
    class SoS2VehicleTurret : VehicleTurret
    {
        public bool isTorpedo;
        public int hardpoint;
        private bool matchedToHardpoint = false;

        public SoS2VehicleTurret() : base()
        {
            PostInitialize();
        }

        public SoS2VehicleTurret(VehiclePawn vehicle) : base(vehicle)
        {
            PostInitialize();

        }

        public SoS2VehicleTurret(VehiclePawn vehicle, VehicleTurret reference) : base(vehicle, reference)
        {
            if (reference is SoS2VehicleTurret)
            {
                isTorpedo = (reference as SoS2VehicleTurret).isTorpedo;
                hardpoint = (reference as SoS2VehicleTurret).hardpoint;
            }
            PostInitialize();
        }

        private void PostInitialize()
		{
            // CHANGE 1.6
            // This contains certain missing initialization steps
            // This should be confirmed as framework iisue and reported
            if (EventRegistry == null)
            {
                EventRegistry = new EventManager<VehicleTurretEventDef>();
            }
        }

        public override void FireTurret()
        {
            if (!isTorpedo && vehicle.compFuel.fuel >= 1)
            {
                vehicle.compFuel.ConsumeFuel(2f/def.magazineCapacity);
                base.FireTurret();
            }
            else if (isTorpedo)
                base.FireTurret();
        }

        public override IEnumerable<SubGizmo> SubGizmos
        {
            get
            {
                if (isTorpedo)
                {
                    return base.SubGizmos;
                }
                else if (autoTargeting)
                {
                    return new List<SubGizmo>() { SubGizmo_AutoTarget(this) };
                }
                else
				{
                    return new List<SubGizmo>();
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref hardpoint, "hardpoint");
        }

		public override bool Tick()
		{
            if (!matchedToHardpoint && vehicle != null && vehicle.Spawned)
            {
                matchedToHardpoint = true;
                // To be inmvestigated why framework calls init on loaded turrets, but not newlu ypgraded turrets
                MatchTurretToHardpoint.Postfix(this);
            }
            return base.Tick();
		}
	}
}
