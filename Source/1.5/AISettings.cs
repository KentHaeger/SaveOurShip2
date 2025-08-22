using RimWorld;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
	// Settings for enemy pawn AI
	public static class AISettings
	{
		private const int ticksPerDay = 60000;
		// For all defending enemy AIs there is now time before assault.
		// Unlike base game, SOS 2 can have enemy map without player pawns, enemy ship, turned into graveyard map on their defeat, can be stabilized.
		public static readonly int TimeBeforeAssault = ticksPerDay*60;
	}
}

