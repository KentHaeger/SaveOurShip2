using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;

namespace SaveOurShip2
{
	// Quite complicated getter for stashed ship quest dialog option
	public static class DialogOptionGetter_StashedShip
	{
		private static Pawn negotiator;
		private static Map map;
		private static Faction faction;
		private static readonly string dummyOptionName = "SoS.StashedShip.RequestOptionDummy".Translate();

		public static void Init(Pawn negotiator_a, Faction faction_a)
		{
			negotiator = negotiator_a;
			map = negotiator.Map;
			faction = faction_a;
		}

		public static void AddOptionsToNode(ref DiaNode dialogNode)
		{
			DiaOption unavailableOption;
			if (AreAllOptionsUnvailable(out unavailableOption))
			{
				dialogNode.options.Add(unavailableOption);
				return;
			}

			var shipList = new List<(string ShipDefName, int SilverCost, int ThreatPoints)> {
				("FastScout", 3000, 0),
				("SmallScienceVessel", 7000, 250),
				("StartShipD", 9000, 250),           // Small Trade ship
				("StartShipC", 9000, 400),           // Exploration vessel
				("StartShipTachinante", 9000, 400),  // Old Martian Corvette
				("StartShipL", 12000, 500),          // Ardent Class Corvette
				("StartShipF", 20000, 2000)          // Heavy frigate
			};

			DiaNode parentNode = new DiaNode("SoS.StashedShip.RequestOptionParent".Translate());
			DiaOption parentOption = new DiaOption("SoS.StashedShip.RequestOptionParent".Translate());
			parentOption.link = parentNode;
			dialogNode.options.Add(parentOption);

			foreach (var (ShipDefName, SilverCost, ThreatPoints) in shipList)
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
			}
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