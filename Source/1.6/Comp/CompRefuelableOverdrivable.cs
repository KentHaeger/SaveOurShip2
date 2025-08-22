using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	class CompRefuelableOverdrivable : CompRefuelable
	{
		private CompPowerTraderOverdrivable overdriveComp;
		private new float ConsumptionRatePerTick => Props.fuelConsumptionRate / 60000f;

		public override void Initialize(CompProperties props)
		{
			base.Initialize(props);
			overdriveComp = parent.GetComp<CompPowerTraderOverdrivable>();
		}

		public override void CompTick()
		{
			base.CompTick();
			if (overdriveComp!=null&&overdriveComp.overdriveSetting>0 && (flickComp == null || flickComp.SwitchIsOn))
			{
				ConsumeFuel(ConsumptionRatePerTick*(1+(2*overdriveComp.overdriveSetting)));
			}
		}

		public override string CompInspectStringExtra()
		{
			string text = TranslatorFormattedStringExtensions.Translate("SoS.FuelInfo", Props.FuelLabel,Fuel.ToStringDecimalIfSmall(),Props.fuelCapacity.ToStringDecimalIfSmall());
			if (!Props.consumeFuelOnlyWhenUsed && HasFuel)
			{
				int fuelMult = 1;
				if (overdriveComp != null)
					fuelMult = 1+(2*overdriveComp.overdriveSetting);
				int numTicks = (int)(Fuel / Props.fuelConsumptionRate / fuelMult * 60000f);
				text = text + " " + TranslatorFormattedStringExtensions.Translate("SoS.FuelAvailableTime", numTicks.ToStringTicksToPeriod());
			}
			if (!HasFuel && !Props.outOfFuelMessage.NullOrEmpty())
			{
				text += "\n" + TranslatorFormattedStringExtensions.Translate("SoS.FuelEmptyInfo", Props.outOfFuelMessage,GetFuelCountToFullyRefuel(),Props.fuelFilter.AnyAllowedDef.label);
			}
			if (Props.targetFuelLevelConfigurable)
			{
				text += "\n" + TranslatorFormattedStringExtensions.Translate("SoS.ConfiguredTargetFuelLevel", TargetFuelLevel.ToStringDecimalIfSmall());
			}
			return text;
		}
	}
}
