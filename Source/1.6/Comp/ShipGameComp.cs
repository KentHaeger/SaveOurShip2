using System.Collections.Generic;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	public class ShipGameComp : GameComponent
	{
		private Dictionary<int, bool> spaceMaps = new Dictionary<int, bool>();
		public HashSet<Thing> shuttleCache = new HashSet<Thing>();
		public List<ShipMapComp> shipHeatMapCompCache = new List<ShipMapComp>();

		public ShipGameComp(Game game)
		{
			AccessExtensions.Utility = this;
		}

		public bool this[Map map]
		{
			get
			{
				if (map == null) return false;
				if (spaceMaps.TryGetValue(map.uniqueID, out var space)) return space;

				var isSpace = map.Biome == ResourceBank.BiomeDefOf.OuterSpaceBiome;
				spaceMaps.Add(map.uniqueID, isSpace);
				return isSpace;
			}
		}

		public bool IsKnownMap(Map map)
		{
			return spaceMaps.ContainsKey(map.uniqueID);
		}

		// Just a safety measure avoiding potential infinite recursion between bool thiis[Map] and biomre getter used there and having Harmony patch
		public bool IsKnownMapInSpace(Map map)
		{
			return spaceMaps[map.uniqueID];
		}

		public void RecacheSpaceMaps()
		{
			spaceMaps = new Dictionary<int, bool>();
		}

		private void ModCompatibilityWarning()
        {
			// Players can try using that mod, but bugreports with it won't be accepted. Mod's design idea is 
			// easy to implement, but waay too expensive to make it work without issues.
			// Not recoomended in favor of new integration features.
			if(ModLister.GetActiveModWithIdentifier("Laurence042.Sos2ShipHullPlatingIsGravshipSubstructure") != null)
			{
				Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.SoftIncompatibilityWithPlatingIsSubstructure"), null, MessageTypeDefOf.SilentInput);
			}
        }

		public override void StartedNewGame()
		{
			ModCompatibilityWarning();
		}

		public override void LoadedGame()
		{
			ModCompatibilityWarning();
		}

		// Laurence042.Sos2ShipHullPlatingIsGravshipSubstructure 
	}
}