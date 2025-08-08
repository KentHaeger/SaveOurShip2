using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Vehicles;
using UnityEngine;

namespace SaveOurShip2.Vehicles
{
    class CompVehicleHeatNet : ThingComp
    {
        public ShipHeatNet myNet;
        public static string storedHeatLabel = "heatNetStroageUsed";
        public float heatStoredLoaded;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            RebuildHeatNet();
            if (respawningAfterLoad)
            {
                DistributeLodedHeat();
            }
        }

        public void DistributeLodedHeat()
		{
            float totalCapacity = myNet.StorageCapacity;
            if (Mathf.Abs(totalCapacity) < float.Epsilon)
            {
                totalCapacity = 1f;
            }
            float fraction = heatStoredLoaded / totalCapacity;
            foreach (CompShipHeatSink sink in myNet.Sinks)
			{
                sink.heatStored = sink.Props.heatCapacity * fraction;
            }
        }

        public void RebuildHeatNet()
        {
            Log.Message("Rebuilding heat net for shuttle");
            myNet = new ShipHeatNet();
            foreach (CompShipHeat comp in parent.GetComps<CompShipHeat>())
            {
                myNet.Register(comp);
                comp.myNet = myNet;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
			{
                float heatStored = myNet.StorageUsed;
                Scribe_Values.Look<float>(ref heatStored, storedHeatLabel);
            }
        }

        public override string CompInspectStringExtra()
        {
            string result = TranslatorFormattedStringExtensions.Translate("SoS.VehicleHeatStored", myNet.StorageUsed, myNet.StorageCapacity);
            if (Prefs.DevMode)
            {
                result += TranslatorFormattedStringExtensions.Translate("SoS.VehicleHeatCompsCount", parent.GetComps<CompShipHeat>().Count());
                foreach (CompShipHeat comp in parent.GetComps<CompShipHeat>())
                {
                    result += "\n" + TranslatorFormattedStringExtensions.Translate("SoS.VehicleHeatComps", comp.GetType().Name);
                }
            }
            return result;
        }
    }
}
