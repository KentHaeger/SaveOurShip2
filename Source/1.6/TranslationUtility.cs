using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace SaveOurShip2
{
	public static class TranslationUtility
	{
		public static ShipGameComp Utility;

		public static TaggedString ToStringTranslated(this bool flag)
		{
			return TranslatorFormattedStringExtensions.Translate(flag ? "SoS.True" : "SoS.False");
		}

		public static TaggedString EnumToStringTranslated(this ShipStartFlags flags)
		{
			string key = "";
			switch (flags)
			{
				case ShipStartFlags.None:
					key = "SoS.ShipStartFlags.None";
					break;
				case ShipStartFlags.Ship:
					key = "SoS.ShipStartFlags.Ship";
					break;
				case ShipStartFlags.Station:
					key = "SoS.ShipStartFlags.Station";
					break;
				case ShipStartFlags.LoadShip:
					key = "SoS.ShipStartFlags.LoadShip";
					break;
				default:
					key = flags.ToString();
					break;
			}
			return TranslatorFormattedStringExtensions.Translate(key);
		}
	}
}

