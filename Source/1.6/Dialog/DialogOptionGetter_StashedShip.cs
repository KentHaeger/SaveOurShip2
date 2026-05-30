using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;

namespace SaveOurShip2
{
	struct ShipClass
	{
		public string NameKey;
		public int MinCRInclusive;
		public int MaxCRExclusive;
		public ShipClass(string name, int minCRInclusive, int maxCRExclusive)
		{
			this.NameKey = name;
			this.MinCRInclusive = minCRInclusive;
			this.MaxCRExclusive = maxCRExclusive;
		}
	}
	// Quite complicated getter for stashed ship quest dialog option
	[StaticConstructorOnStartup]
	public static class DialogOptionGetter_StashedShip
	{
		private static Pawn negotiator;
		private static Map map;
		private static Faction faction;
		private static readonly string dummyOptionName = "SoS.StashedShip.RequestOptionDummy".Translate();

		private static List<ShipClass> shipClasses = null;

		static DialogOptionGetter_StashedShip()
		{
			const int fighterCR = 50;
			const int bomberCR = 100;
			const int corvetteCR = 168;
			const int frigateCR = 398;
			const int destroyerCR = 847;
			const int cruiserCR = 1100;
			const int battleshipCR = 1900;
			const int battlecruiserCR = 2400;
			const int dreadnoughtCR = 3500;

			shipClasses = new List<ShipClass>()
			{
				new ShipClass("SoS.StashedShip.Fighters".Translate(), fighterCR, bomberCR),
				new ShipClass("SoS.StashedShip.Bombers".Translate(), bomberCR, corvetteCR),
				new ShipClass("SoS.StashedShip.Corvettes".Translate(), corvetteCR, frigateCR),
				new ShipClass("SoS.StashedShip.Frigates".Translate(), frigateCR, destroyerCR),
				new ShipClass("SoS.StashedShip.Destroyers".Translate(), destroyerCR, cruiserCR),
				new ShipClass("SoS.StashedShip.Cruisers".Translate(), cruiserCR, battlecruiserCR),
				new ShipClass("SoS.StashedShip.Battleships".Translate(), battlecruiserCR, battleshipCR),
				new ShipClass("SoS.StashedShip.Battlecruisers".Translate(), battleshipCR, dreadnoughtCR),
				new ShipClass("SoS.StashedShip.Dreadnoughts".Translate(), dreadnoughtCR, int.MaxValue),
			};
		}
		public static void Init(Pawn negotiator, Faction faction)
		{
			DialogOptionGetter_StashedShip.negotiator = negotiator;
			map = negotiator.Map;
			DialogOptionGetter_StashedShip.faction = faction;
		}

		public static void AddGroup(DiaNode parentNode, string groupKey, IEnumerable<ShipDef> ships)
		{
			DiaNode listNode = new DiaNode(groupKey.Translate());
			DiaOption listOption = new DiaOption(groupKey.Translate());
			listOption.link = listNode;
			parentNode.options.Add(listOption);

			SimpleCurve valueFromShapeCount = new SimpleCurve()
			{
				new CurvePoint(0f, 2000f ),
				new CurvePoint(120f, 3000f),       // Small starter ship
				new CurvePoint(500f, 5000f),
				new CurvePoint(1000f, 7000f),
				new CurvePoint(5000f, 12000f),
				new CurvePoint(20000f, 25000f),    // Largest dreadnoughtrs have over 20000 shapes
				new CurvePoint(1000000f, 25000f),  // Cap
			};

			SimpleCurve threatFromCR = new SimpleCurve()
			{
				new CurvePoint(0f, 0f ),
				new CurvePoint(23f, 0f ),          // Fast Scout - unguarded
				new CurvePoint(27f, 150f ),        // Small Science vessel - small thereat. This CR represents unarmed ship that is larger than small scout in size
				new CurvePoint(150f, 400f),        // Fighter     
				new CurvePoint(300f, 700f),        // Corvette
				new CurvePoint(1800f, 2000f),      // Destroyer
				new CurvePoint(1800f, 3000f),      // Cruiser
				new CurvePoint(4000f, 5000f),      // reach cap at Small Dreadnougnts
				new CurvePoint(1000000f, 5000f),   // The cap is 5000, as it is considered not conenient to fight full size 10000 points threat on some away map, not 
		              							   // on player map with defenses
			};

			foreach (ShipDef ship in ships)
			{
				if (ship == null)
				{
					Log.Message("SoS 2: skipping null ship in stashed ships menu");
					continue;
				}
				DiaOption unavailableOption;
				float silverCost = valueFromShapeCount.Evaluate(ship.parts.Count);
				int roundedCost = (int)silverCost / 100 * 100;
				int threatPoints = (int)threatFromCR.Evaluate(ship.combatPoints);
				if (IsSpecificOptionUnvailable(ship.defName, roundedCost, out unavailableOption))
				{
					listNode.options.Add(unavailableOption);
				}
				else
				{
					listNode.options.Add(RequestStashedShipOption(ship.defName, roundedCost, threatPoints));
					Log.Message($"Add ship: {ship.defName}");
				}

				/*DiaNode shipNode = new DiaNode(ship.LabelCap);
				DiaOption shipOption = new DiaOption(ship.LabelCap);
				shipOption.link = listNode;
				listNode.options.Add(shipOption);*/
			}

			DiaOption optionBack = new DiaOption("GoBack".Translate());
			optionBack.linkLateBind = delegate () { return parentNode; }; // FactionDialogMaker.ResetToRoot(faction, negotiator);
			listNode.options.Add(optionBack);
		}

		private static bool IsPickableShip(ShipDef ship)
		{
			return !ship.startingDungeon &&
				!ship.startingShip &&          // Starting ships, traders and cerriers are a separate categoried
				!ship.tradeShip &&
				!ship.carrier &&
				!ship.neverWreck &&
				!ship.neverRandom &&           // No rare, strange, special ships like Mysterius Archotech Sphere
				!ship.neverAttacks &&
				!ship.spaceSite &&
				!ship.neverWreck &&

				ship.defName.Substring(0, 2) != "BP" &&         // Exclude blueprints
				ship != ResourceBank.ShipDefOf.MechPsychicAmp; 
		}
		public static void AddOptionsToNode(ref DiaNode dialogNode)
		{
			DiaOption unavailableOption;
			if (AreAllOptionsUnvailable(out unavailableOption))
			{
				dialogNode.options.Add(unavailableOption);
				return;
			}


			/*var shipList = new List<(string ShipDefName, int SilverCost, int ThreatPoints)> {
				("FastScout", 3000, 0),
				("SmallScienceVessel", 7000, 250),
				("StartShipD", 9000, 250),           // Small Trade ship
				("StartShipC", 9000, 400),           // Exploration vessel
				("StartShipTachinante", 9000, 400),  // Old Martian Corvette
				("StartShipL", 12000, 500),          // Ardent Class Corvette
				("StartShipF", 20000, 2000)          // Heavy frigate
			};*/
			DiaNode parentNode = new DiaNode("SoS.StashedShip.RequestOptionParent".Translate());
			DiaOption parentOption = new DiaOption("SoS.StashedShip.RequestOptionParent".Translate());
			parentOption.link = parentNode;
			dialogNode.options.Add(parentOption);

			/*foreach (var (ShipDefName, SilverCost, ThreatPoints) in shipList)
			{
				if (IsSpecificOptionUnvailable(ShipDefName, SilverCost, out unavailableOption))
				{
					//dialogNode.options.Add(unavailableOption);
					parentNode.options.Add(unavailableOption);
				}
				else
				{
					//dialogNode.options.Add(RequestStashedShipOption(ShipDefName, SilverCost, ThreatPoints));
					parentNode.options.Add(RequestStashedShipOption(ShipDefName, SilverCost, ThreatPoints));
				}
			}*/

			List<ShipDef> recommandedShips = new List<ShipDef>()
			{
				DefDatabase<ShipDef>.GetNamedSilentFail("FastScout"),
				DefDatabase<ShipDef>.GetNamedSilentFail("SmallScienceVessel"),
			};
			AddGroup(parentNode, "recommandedShips", recommandedShips);			

			foreach(ShipClass shipClass in shipClasses)
			{
				List<ShipDef> shipList = DefDatabase<ShipDef>.AllDefs.Where(
					x => shipClass.MinCRInclusive  <= x.combatPoints && x.combatPoints < shipClass.MaxCRExclusive && IsPickableShip(x)).ToList();
				AddGroup(parentNode, shipClass.NameKey.Translate(), shipList);
			}

			/*List<ShipDef> fighters = new List<ShipDef>();
			fighters.AddRange(DefDatabase<ShipDef>.AllDefs.Where(x => fighterCR <= x.combatPoints && x.combatPoints < bomberCR && IsPickableShip(x)));
			AddGroup(parentNode, "SoS.StashedShip.Fighters".Translate(), fighters);

			List<ShipDef> bombers = new List<ShipDef>();
			bombers.AddRange(DefDatabase<ShipDef>.AllDefs.Where(x => bomberCR <= x.combatPoints && x.combatPoints < corvetteCR && IsPickableShip(x)));
			AddGroup(parentNode, "SoS.StashedShip.Bombers".Translate(), bombers);

			List<ShipDef> corvettes = new List<ShipDef>();
			corvettes.AddRange(DefDatabase<ShipDef>.AllDefs.Where(x => corvetteCR <= x.combatPoints && x.combatPoints < frigateCR && IsPickableShip(x)));
			AddGroup(parentNode, "SoS.StashedShip.Corvettes".Translate(), corvettes);

			List<ShipDef> frigates = new List<ShipDef>();
			frigates.AddRange(DefDatabase<ShipDef>.AllDefs.Where(x => frigateCR <= x.combatPoints && x.combatPoints < destroyerCR && IsPickableShip(x)));
			AddGroup(parentNode, "SoS.StashedShip.Frigates".Translate(), frigates);

			List<ShipDef> destroyers = DefDatabase<ShipDef>.AllDefs.Where(
				x => frigateCR <= x.combatPoints && x.combatPoints < destroyerCR && IsPickableShip(x)).ToList();
			AddGroup(parentNode, "SoS.StashedShip.Destroyers".Translate(), frigates);

			List<ShipDef> frigates = new List<ShipDef>();
			frigates.AddRange(DefDatabase<ShipDef>.AllDefs.Where(x => frigateCR <= x.combatPoints && x.combatPoints < destroyerCR && IsPickableShip(x)));
			AddGroup(parentNode, "SoS.StashedShip.Cruisers".Translate(), frigates);

			List<ShipDef> frigates = new List<ShipDef>();
			frigates.AddRange(DefDatabase<ShipDef>.AllDefs.Where(x => frigateCR <= x.combatPoints && x.combatPoints < destroyerCR && IsPickableShip(x)));
			AddGroup(parentNode, "SoS.StashedShip.Battleships".Translate(), frigates);

			List<ShipDef> carriers = new List<ShipDef>();
			carriers.AddRange(DefDatabase<ShipDef>.AllDefs.Where(x => x.carrier && IsPickableShip(x)));
			AddGroup(parentNode, "Carriers", carriers);*/


			DiaOption optionBack = new DiaOption("GoBack".Translate());
			optionBack.linkLateBind = FactionDialogMaker.ResetToRoot(faction, negotiator);
			parentNode.options.Add(optionBack);
		}

		private static bool AreAllOptionsUnvailable(out DiaOption unavailableOption)
		{
			if (!ResourceBank.ResearchProjectDefOf.ShipBasics.IsFinished)
			{
				DiaOption optionNeedsResearch = new DiaOption(dummyOptionName);
				optionNeedsResearch.Disable("SoS.StashedShip.RequiresTech".Translate());
				unavailableOption = optionNeedsResearch;
				return true;
			}
			const int goodwillNeeded = 40;
			if (faction.PlayerGoodwill < goodwillNeeded)
			{
				DiaOption optionNeedGoodwill = new DiaOption(dummyOptionName);
				// Base game sting key used
				optionNeedGoodwill.Disable("NeedGoodwill".Translate(goodwillNeeded.ToString("F0")));
				unavailableOption = optionNeedGoodwill;
				return true;
			}
			int timeoutLeft = ShipInteriorMod2.WorldComp.LastStashedShipRequestTick + ShipWorldComp.StashedShipRequestInterval - Find.TickManager.TicksGame;
			if (timeoutLeft > 0)
			{
				DiaOption optionOnCooldown = new DiaOption(dummyOptionName);
				optionOnCooldown.Disable("SoS.StashedShip.OnCooldown".Translate(GenDate.TicksToDays(timeoutLeft)));
				unavailableOption = optionOnCooldown;
				return true;
			}
			unavailableOption = null;
			return false;
		}

		private static bool IsSpecificOptionUnvailable(string shipDefName, int shipSilverCost, out DiaOption unavailableOption)
		{
			if (FactionDialogMaker.AmountSendableSilver(map) < shipSilverCost)
			{
				DiaOption optionNeedSilver = new DiaOption("SoS.StashedShip.RequestOptionDummySpecific".Translate(GetLabel(shipDefName)));
				// Base game sting key used
				optionNeedSilver.Disable("NeedSilverLaunchable".Translate(shipSilverCost));
				unavailableOption = optionNeedSilver;
				return true;
			}
			unavailableOption = null;
			return false;
		}

		private static string GetLabel(string shipDefName)
		{
			string shipLablel = shipDefName;
			ShipDef shipDef = DefDatabase<ShipDef>.GetNamedSilentFail(shipDefName);
			if (shipDef != null)
			{
				shipLablel = shipDef.LabelCap;
			}
			else
			{
				Log.Warning($"SOS 2: can't find ship def name {shipDef} when generating stashed ship quest options");
			}
			return shipLablel;
		}

		private static DiaOption RequestStashedShipOption(string shipDefName, int shipSilverCost, int threatPoints)
		{
			string threatString = "";
			if (threatPoints == 0)
			{
				threatString = "SoS.StashedShip.NoSecuroity".Translate();
			}
			else if (threatPoints < 300)
			{
				threatString = "SoS.StashedShip.MinorSceurity".Translate();
			}
			else if (threatPoints < 900)
			{
				threatString = "SoS.StashedShip.AverageSceurity".Translate();
			}
			else
			{
				threatString = "SoS.StashedShip.HighSceurity".Translate();
			}

			string optionName = "SoS.StashedShip.RequestOption".Translate(GetLabel(shipDefName), shipSilverCost, threatString);
			DiaOption requestOption = new DiaOption(optionName)
			{
				action = delegate
				{
					Slate slate = new Slate();
					Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(ResourceBank.QuestScriptDefOf.SoSStashedShipScript, slate);
					quest.tags.Add(GenStep_StashedShip.ShipDefTagName + ":" + shipDefName);
					quest.tags.Add(GenStep_StashedShip.ThreatTagName + ":" + threatPoints);
					QuestUtility.SendLetterQuestAvailable(quest);
					TradeUtility.LaunchThingsOfType(ThingDefOf.Silver, shipSilverCost, map, null);
					ShipInteriorMod2.WorldComp.LastStashedShipRequestTick = Find.TickManager.TicksGame;
				}
			};
			requestOption.resolveTree = true;
			return requestOption;
		}
	}
}