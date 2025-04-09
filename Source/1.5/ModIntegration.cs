using RimWorld;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
	public class ModIntegration
	{
		public static string CEModName = "Combat Extended";
		public static bool IsCEEnabled()
		{
			return ModLister.HasActiveModWithName(CEModName);
		}
	}
}

