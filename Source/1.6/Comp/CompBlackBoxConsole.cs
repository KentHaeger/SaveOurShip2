using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;

namespace SaveOurShip2
{
	class CompBlackBoxConsole : ThingComp
	{
		bool hacked = false;

		public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
		{
			List<FloatMenuOption> options = new List<FloatMenuOption>();
			foreach (FloatMenuOption op in base.CompFloatMenuOptions(selPawn))
				options.Add(op);
			if(!hacked)
				options.Add(new FloatMenuOption("SoS.BlackBoxHack".Translate(), delegate { Job persuadeAI = new Job(DefDatabase<JobDef>.GetNamed("HackBlackBoxConsole"), this.parent); selPawn.jobs.TryTakeOrderedJob(persuadeAI); }));
			return options;
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<bool>(ref hacked, "hacked");
		}

		public void HackMe(Pawn pawn)
		{
			if (Rand.Chance(0.05f * pawn.skills.GetSkill(SkillDefOf.Intellectual).levelInt))
			{
				Success(pawn);
			}
			else if (Rand.Chance(0.05f * (20 - pawn.skills.GetSkill(SkillDefOf.Intellectual).levelInt)))
			{
				CriticalFailure(pawn);
			}
			else
			{
				Failure(pawn);
			}
		}

		private void Success(Pawn pawn)
		{
			hacked = true;
			pawn.skills.GetSkill(SkillDefOf.Intellectual).Learn(2000);
			if (this.parent.def.defName.Equals("Ship_LabConsole"))
			{
				Messages.Message("SoS.ReactorDisableSuccess".Translate(), null, MessageTypeDefOf.PositiveEvent);
				foreach(Thing thing in this.parent.Map.spawnedThings)
				{
					if (thing.TryGetComp<CompDamagedReactor>() != null)
						thing.TryGetComp<CompBreakdownable>().DoBreakdown();
				}
			}
			else
			{
				Messages.Message("SoS.DoorOpenSuccess".Translate(), null, MessageTypeDefOf.PositiveEvent);
				foreach (Thing t in this.parent.Map.spawnedThings)
				{
					if (t is Building_Door d)
					{
						d.holdOpenInt = true;
						d.StartManualOpenBy(pawn);
					}
				}
				this.parent.Map.fogGrid.ClearAllFog();
			}
		}

		private void Failure(Pawn pawn)
		{
			Messages.Message("SoS.BlackBoxHackFailed".Translate(), null, MessageTypeDefOf.CautionInput);
		}

		private void CriticalFailure(Pawn pawn)
		{
			if (this.parent.def.defName.Equals("Ship_LabConsole"))
			{
				hacked = true;
				Messages.Message("SoS.BlackBoxHackFailedCritical".Translate(), null, MessageTypeDefOf.NegativeEvent);
				foreach (Thing thing in this.parent.Map.spawnedThings)
				{
					if (thing.TryGetComp<CompDamagedReactor>() != null)
						thing.TryGetComp<CompExplosive>().StartWick();
				}
			}
			else
				Messages.Message("SoS.BlackBoxHackFailed".Translate(), null, MessageTypeDefOf.CautionInput);
		}
	}
}
