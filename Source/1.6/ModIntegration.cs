using RimWorld;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
	public class ModIntegration
	{
		public static bool HasActiveModWithIdentifierAndOptionalSuffix(string modID)
		{
			string suffixedModID = modID + "_steam";
			return (ModLister.GetActiveModWithIdentifier(modID) ??
				    ModLister.GetActiveModWithIdentifier(suffixedModID)) != null;
		}

		// Todo: it is to be verified with mod maintainers that mod identification can be switched from name to mod ID
		public const string CEModName = "Combat Extended";

		public const string UnlockModID = "Boris.SOS2uas";

		public const string SpinalEnginesModID = "TheCafFiend.SOS2SpinalEngines";
		public static bool IsCEEnabled()
		{
			return ModLister.HasActiveModWithName(CEModName);
		}

		public const string MissileGirlModID = "vr.missilegirl";
		public static bool IsMissileGirlActive()
		{
			return ModLister.GetActiveModWithIdentifier(MissileGirlModID, true) != null;
		}

		// Because of Odyssey changes, need to place world objects further from each other,
		// as zoomin in in orbit normally results in hiding orbit, switching to syrface layer.
		public const float NewOdyOffsetScale = 2.5f;
	}
}

