﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SaveOurShip2
{
	class Thought_IsArchotechSpore : Thought_SituationalSocial
	{
		public override float OpinionOffset()
		{
			if (ThoughtUtility.ThoughtNullified(pawn, def))
			{
				return 0f;
			}
			else if (ModsConfig.IdeologyActive && otherPawn.ideo?.Ideo != null && otherPawn.ideo.Ideo.memes.Any(def => def == ResourceBank.MemeDefOf.Structure_Archist))
				return 100;
			else if (otherPawn.story.traits.HasTrait(TraitDefOf.BodyPurist))
				return -50;
			else if (otherPawn.story.traits.HasTrait(TraitDefOf.Transhumanist))
				return 50;
			return 20;
		}
	}
}
