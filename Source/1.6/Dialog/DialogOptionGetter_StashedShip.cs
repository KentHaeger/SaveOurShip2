using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;

namespace SaveOurShip2
{
	// Quite complicated getter for stashed ship quest dialog option
	[StaticConstructorOnStartup]
	public static class DialogOptionGetter_StashedShip
	{
		private static Pawn negotiator;
		private static Map map;
		private static Faction faction;
		private static readonly string dummyOptionName = "SoS.StashedShip.RequestOptionDummy".Translate();

		static DialogOptionGetter_StashedShip()
		{
		}
		public static void Init(Pawn negotiator, Faction faction)
		{
			DialogOptionGetter_StashedShip.negotiator = negotiator;
			map = negotiator.Map;
			DialogOptionGetter_StashedShip.faction = faction;
		}

		public static void AddGroup(DiaNode parentNode, string groupKey, IEnumerable<ShipDef> ships, bool flipShip, bool devDetails = false)
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
					listNode.options.Add(RequestStashedShipOption(ship.defName, roundedCost, threatPoints, flipShip, devDetails));
				}
			}

			DiaOption optionBack = new DiaOption("GoBack".Translate());
			optionBack.linkLateBind = delegate () { return parentNode; }; // FactionDialogMaker.ResetToRoot(faction, negotiator);
			listNode.options.Add(optionBack);
		}

		private static bool IsPickableNormalShip(ShipDef ship)
		{
			return !ship.startingDungeon &&
				!ship.startingShip &&          // Starting ships, traders and cerriers are a separate categoried
				!ship.tradeShip &&
				!ship.carrier &&
				!ship.neverWreck &&
				!ship.neverRandom &&           // No rare, strange, special ships like Mysterius Archotech Sphere
				!ship.neverAttacks &&
				!ship.spaceSite &&
				ship.defName.Substring(0, 2) != "BP" &&         // Exclude blueprints
				ship != ResourceBank.ShipDefOf.MechPsychicAmp;
		}
		private static bool IsPickableTradeShip(ShipDef ship)
		{
			return !ship.startingDungeon &&
				!ship.startingShip &&
				ship.tradeShip &&
				!ship.carrier &&
				!ship.neverWreck &&
				!ship.neverRandom &&
				!ship.neverAttacks &&
				!ship.spaceSite &&
				!ship.IsBlueprintByName() &&
				ship != ResourceBank.ShipDefOf.MechPsychicAmp;
		}
		private static bool IsPickableCarrier(ShipDef ship)
		{
			// Ignoring trade ship flag here so that trade carriers, if someone makes them, are shown in this category
			return !ship.startingDungeon &&
				!ship.startingShip &&
				ship.carrier &&
				!ship.neverWreck &&
				!ship.neverRandom &&
				!ship.neverAttacks &&
				!ship.spaceSite &&
				!ship.IsBlueprintByName() &&
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

			DiaNode parentNode = new DiaNode("SoS.StashedShip.RequestOptionParent".Translate());
			DiaOption parentOption = new DiaOption("SoS.StashedShip.RequestOptionParent".Translate());
			parentOption.link = parentNode;
			dialogNode.options.Add(parentOption);

			List<ShipDef> recommandedShips = new List<ShipDef>()
			{
				DefDatabase<ShipDef>.GetNamedSilentFail("FastScout"),
				DefDatabase<ShipDef>.GetNamedSilentFail("SmallScienceVessel"),
			};
			AddGroup(parentNode, "SoS.StashedShip.RecommendedStarterShips", recommandedShips, flipShip: false);

			List<ShipDef> starterShips = DefDatabase<ShipDef>.AllDefs.Where(
					x => x.startingShip && !x.startingDungeon && x.defName != ResourceBank.ShipDefNames.Random).ToList();
			AddGroup(parentNode, "SoS.StashedShip.StarterShips", starterShips, flipShip: false);

			List<ShipDef> tradeShips = DefDatabase<ShipDef>.AllDefs.Where(
					x => IsPickableTradeShip(x)).ToList();
			AddGroup(parentNode, "SoS.StashedShip.Traders", tradeShips, flipShip: true);

			List<ShipDef> lowCRShips = DefDatabase<ShipDef>.AllDefs.Where(
					x => x.combatPoints < ShipCatalog.FighterBomberCR && IsPickableNormalShip(x)).ToList();
				AddGroup(parentNode, "SoS.StashedShip.LowCR", lowCRShips, true);

			foreach (ShipClass shipClass in ShipCatalog.ShipClasses)
			{
				List<ShipDef> shipList = DefDatabase<ShipDef>.AllDefs.Where(
					x => shipClass.MinCRInclusive  <= x.combatPoints && x.combatPoints < shipClass.MaxCRExclusive && IsPickableNormalShip(x) &&
					(shipClass.Predicate != null ? shipClass.Predicate(x) : true)).ToList();
				AddGroup(parentNode, shipClass.NameKey, shipList, true);
			}

			List<ShipDef> carriers = DefDatabase<ShipDef>.AllDefs.Where(
					x => IsPickableCarrier(x)).ToList();
			AddGroup(parentNode, "SoS.StashedShip.Carriers", carriers, flipShip: true);

			if(Prefs.DevMode)
			{
				List<ShipDef> allships = DefDatabase<ShipDef>.AllDefs.ToList();
				AddGroup(parentNode, "SoS.StashedShip.DevAllShips", allships, flipShip: false, devDetails: true);
				AddGroup(parentNode, "SoS.StashedShip.DevAllShipsFlipped", allships, flipShip: true, devDetails: true);
			}

			DiaOption optionBack = new DiaOption("GoBack".Translate());
			optionBack.linkLateBind = FactionDialogMaker.ResetToRoot(faction, negotiator);
			parentNode.options.Add(optionBack);
		}
		private static bool AreAllOptionsUnvailable(out DiaOption unavailableOption)
		{
			if (map.IsSpace())
			{
				DiaOption optionNotInSpace = new DiaOption(dummyOptionName);
				optionNotInSpace.Disable("SoS.StashedShip.NotInSpace".Translate());
				unavailableOption = optionNotInSpace;
				return true;
			}
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

		private static DiaOption RequestStashedShipOption(string shipDefName, int shipSilverCost, int threatPoints, bool flip, bool devDetails = false)
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
			if (devDetails)
			{
				optionName += " (" + shipDefName + ")";
			}
			DiaOption requestOption = new DiaOption(optionName)
			{
				action = delegate
				{
					Slate slate = new Slate();
					Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(ResourceBank.QuestScriptDefOf.SoSStashedShipScript, slate);
					quest.tags.Add(GenStep_StashedShip.ShipDefTagName + ":" + shipDefName);
					quest.tags.Add(GenStep_StashedShip.ThreatTagName + ":" + threatPoints);
					quest.tags.Add(GenStep_StashedShip.FlipTagName + ":" + flip.ToString());
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