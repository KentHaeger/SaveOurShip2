using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;

namespace SaveOurShip2
{
	public class ShipWorldComp : WorldComponent
	{
		//private int ShipsHaveInsidesVersion;
		public int PlayerFactionBounty;
		public int LastSporeGiftTick;
		public List<string> Unlocks = new List<string>();
		public bool startedEndgame;
		public bool SoSWin = false;
		public bool renderedThatAlready = false;
		public List<Building_ShipSensor> Sensors = new List<Building_ShipSensor>();
		public bool MoveShipFlag = false;
		// If player had already had space map, for tutorial-ish letter.
		private bool hadSpaceMap = false;
		private float? previousThreatScale = null;
		public bool HadSpaceMap => hadSpaceMap;
		public int nextUniqueMissionID = 0;
		public const int StarhipBowTimeout = 720000; // 12 days
		public int LastStarshipBowTick = -StarhipBowTimeout;
		public int LastFoundAmplifierTick = 0;

		public ShipWorldComp(World world) : base(world)
		{
			ShipInteriorMod2.PurgeWorldComp();
			WorldUpdateRadiusHandler.PrurgeLayerRadiusSettings();

        }

		private int nextShipId = 0;
		private int newShipId
		{
			get
			{
				nextShipId++;
				return nextShipId;
			}
		}
		public int AddNewShip(Dictionary<int, SpaceShipCache> ShipsOnMap, Building core)
		{
			int mergeToIndex = ShipInteriorMod2.WorldComp.newShipId;
			if(ShipsOnMap.ContainsKey(mergeToIndex))
            {
				Log.Warning("[SoS2] ShipsOnMap already contains key " + mergeToIndex + " - fixing this.");
				int i;
				for(i=0;i<ShipsOnMap.Count;i++)
                {
					if(!ShipsOnMap.ContainsKey(i))
                    {
						mergeToIndex = i;
						break;
                    }
                }
				for(i=0;i<ShipsOnMap.Count;i++)
                {
					if (i!=mergeToIndex && !ShipsOnMap.ContainsKey(i))
					{
						nextShipId = i;
						break;
					}
				}
            }
			ShipsOnMap.Add(mergeToIndex, new SpaceShipCache());
			ShipsOnMap[mergeToIndex].RebuildCache(core, mergeToIndex);
			return mergeToIndex;
		}
		public override void FinalizeInit(bool fromLoad)
		{
			base.FinalizeInit(fromLoad);
			/*foreach (Faction f in Find.FactionManager.AllFactions)
			{
				Log.Message("fac: " + f + " defName: " + f.def.defName);
			}*/
			if (!Find.FactionManager.AllFactions.Any(f => f.def == FactionDefOf.Mechanoid))
				Log.Error("SOS2: Mechanoid faction not found! Parts of SOS2 will likely fail to function properly!");
			if (!Find.FactionManager.AllFactions.Any(f => f.def == FactionDefOf.Pirate || f.def == FactionDefOf.PirateWaster || f.def.defName.Equals("PirateYttakin")))
				Log.Warning("SOS2: Pirate faction not found! SOS2 gameplay experience will be affected.");
			if (!Find.FactionManager.AllFactions.Any(f => f.def == FactionDefOf.Insect))
				Log.Warning("SOS2: Insect faction not found! SOS2 gameplay experience will be affected.");
		}

		public bool NotorietyActive
		{
			get
			{
				return PlayerFactionBounty > 20;
			}
		}
		public int TicksBetweenNotorietyAttacks
        {
            get
            {
                // Updated formula: bounty hunters attack evry (15 days / Sqrt(notoriety)) with min notoriety 20 causing attachs every ~ 3.4 days,
				// every 2 days at 55 notoriety, every 1.5 days at 100 notoriety
                return (int)Mathf.Max((float)GenDate.TicksPerDay * 15 / Mathf.Sqrt(PlayerFactionBounty), (float)GenDate.TicksPerDay);
            }
        }

		public int BountyPayment
		{
			get
			{
				return 1250 * PlayerFactionBounty;
			}
		}

        // Will show that letter once per save in order not to annoy players
        private bool difficultyLetterShown = false;
		public override void WorldComponentTick()
		{
			if (Find.TickManager.TicksGame % GenTicks.TickRareInterval == 0)
            {
				if (ShipInteriorMod2.FindPlayerShipMap() != null)
				{
					hadSpaceMap = true;
				}
				if (previousThreatScale != null && previousThreatScale != Find.Storyteller.difficulty.threatScale && hadSpaceMap && !difficultyLetterShown)
                {
					difficultyLetterShown = true;
					Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoS.Letter.DifficultyChanged"),
						TranslatorFormattedStringExtensions.Translate("SoS.Letter.DifficultyChangedDesc"), LetterDefOf.PositiveEvent);

				}
				previousThreatScale = Find.Storyteller.difficulty.threatScale;
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look<string>(ref Unlocks, "Unlocks", LookMode.Value);
			Scribe_Values.Look<int>(ref PlayerFactionBounty, "PlayerFactionBounty", 0);
			Scribe_Values.Look<int>(ref LastSporeGiftTick, "LastSporeGiftTick", 0);
			Scribe_Values.Look<bool>(ref startedEndgame, "StartedEndgame");
			Scribe_Values.Look<int>(ref nextUniqueMissionID, "UniqueMissionID");
			Scribe_Values.Look<int>(ref LastStarshipBowTick, "LastStarshipBowTick", -StarhipBowTimeout);
			// Finding amplifier is forced so for old saves last found apmplifier tick should be set to current tick
			// In this case it won't be found immediately, only after find interval is passed
			Scribe_Values.Look<int>(ref LastFoundAmplifierTick, "LastFoundAmplifierTick", Find.TickManager.TicksGame);
			Scribe_Values.Look<bool>(ref hadSpaceMap, "hadSpaceMap");
			Scribe_Values.Look<bool>(ref difficultyLetterShown, "difficultyDialogShown");

			if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				ShipInteriorMod2.PurgeWorldComp();
                WorldUpdateRadiusHandler.PrurgeLayerRadiusSettings();
            }
			// Devmode-only flag should be reset to false if devmode is not enabled after loading a save where it is set to true
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				previousThreatScale = Find.Storyteller.difficulty.threatScale;
			}
			/*if (Scribe.mode!=LoadSaveMode.Saving)
			{
				if(Unlocks.Contains("JTDrive")) //Back-compatibility: unlock JT drive research project if you got it before techprints were a thing
				{
					Find.ResearchManager.FinishProject(ResearchProjectDef.Named("SoSJTDrive"));
					Unlocks.Remove("JTDrive");
				}
				if (!Unlocks.Contains("JTDriveToo")) //Legacy compatibility for back when policies were different and a certain developer's head was still outside his own ass
				{
					if (!Unlocks.Contains("JTDriveResearchChecked") && Find.ResearchManager.GetProgress(ResearchProjectDef.Named("SoSJTDrive")) >= 4000) //Hey, if you've already finished this research, you deserve special commemmoration!
					{
						Unlocks.Add("JTDriveToo");
						GiveMeEntanglementManifold();
					}
					else //Let's check another way!
					{
						foreach (FieldInfo field in typeof(ResearchManager).GetFields()) //Let's randomly look at fields inside the research manager!
						{
							if (field.FieldType == typeof(Dictionary<ResearchProjectDef, int>)) //Hmm, any sort of dictionary of research projects and integers must be important!
							{
								if (((Dictionary<ResearchProjectDef, int>)field.GetValue(Find.ResearchManager)).ContainsKey(ResearchProjectDef.Named("SoSJTDrive"))) //Hey, if the JT drive gets mentioned in such an important place, maybe it means you already found one!
								{
									Unlocks.Add("JTDriveToo");
									GiveMeEntanglementManifold();
									((Dictionary<ResearchProjectDef, int>)field.GetValue(Find.ResearchManager)).Remove(ResearchProjectDef.Named("SoSJTDrive")); //Remove the NASTY EVIL FORBIDDEN DATA!
								}
							}
						}
					}
					if(!Unlocks.Contains("JTDriveResearchChecked"))
						Unlocks.Add("JTDriveResearchChecked");
				}
			}
			//recover from incorrect savestates
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (!ShipCombatManager.InCombat && !ShipCombatManager.InEncounter)
				{
					if (ShipCombatManager.EnemyShip == null
						&& (ShipCombatManager.CanSalvageEnemyShip || ShipCombatManager.ShouldSalvageEnemyShip))
					{
						Log.Error("Recovering from incorrect state regarding enemy ship in save file. If there was an enemy ship, it is now lost and cannot be salvaged.");
						ShipCombatManager.CanSalvageEnemyShip = false;
						ShipCombatManager.ShouldSalvageEnemyShip = false;
						ShipCombatManager.ShouldSkipSalvagingEnemyShip = false;
					}
				}
			}*/
		}

		public void NotifyLaunch()
        {
			hadSpaceMap = true;
        }
	}
}
