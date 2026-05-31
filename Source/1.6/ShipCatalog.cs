using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Verse;

namespace SaveOurShip2
{
	public struct ShipClass
	{
		public string NameKey;
		public int MinCRInclusive;
		public int MaxCRExclusive;
		public Func<ShipDef, bool> Predicate;
		public ShipClass(string name, int minCRInclusive, int maxCRExclusive, Func<ShipDef, bool> predicate = null)
		{
			this.NameKey = name;
			this.MinCRInclusive = minCRInclusive;
			this.MaxCRExclusive = maxCRExclusive;
			this.Predicate = predicate;
		}
	}

	[StaticConstructorOnStartup]
	public static class ShipCatalog
	{
		private static List<ShipClass> shipClasses;

		public static IEnumerable<ShipClass> ShipClasses
		{
			get { return shipClasses; }
		}

		public const int FighterBomberCR = 40;
		public const int CorvetteCR = 131;
		public const int FrigateCR = 398;
		public const int DestroyerCR = 790;
		public const int CruiserCR = 1100;
		public const int BattleshipCR = 1900;
		public const int BattlecruiserCR = 2400;
		public const int DreadnoughtCR = 4000;

		static ShipCatalog()
		{
			shipClasses = new List<ShipClass>()
			{
				new ShipClass("SoS.StashedShip.Fighters", FighterBomberCR, CorvetteCR, x => !x.IsBomber()),
				new ShipClass("SoS.StashedShip.Bombers", FighterBomberCR, CorvetteCR, x => x.IsBomber()),
				new ShipClass("SoS.StashedShip.Corvettes", CorvetteCR, FrigateCR),
				new ShipClass("SoS.StashedShip.Frigates", FrigateCR, DestroyerCR),
				new ShipClass("SoS.StashedShip.Destroyers", DestroyerCR, CruiserCR),
				new ShipClass("SoS.StashedShip.Cruisers", CruiserCR, BattlecruiserCR),
				new ShipClass("SoS.StashedShip.Battleships", BattlecruiserCR, BattleshipCR),
				new ShipClass("SoS.StashedShip.Battlecruisers", BattleshipCR, DreadnoughtCR),
				new ShipClass("SoS.StashedShip.Dreadnoughts", DreadnoughtCR, int.MaxValue),
			};
		}
	}
}

