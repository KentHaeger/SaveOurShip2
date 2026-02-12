using System;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;
using Verse.Noise;

namespace SaveOurShip2
{
	public class TWRMath
	{
        // Math stuff
        // Because thrust has order of magnitude 1 or 10 and ship mass typically has order of magnitude of 1000 or 10000
        // literal TWR is a verey small fraction, so a multiplier of 7000 is used to get mor hunman-readable value to display in the UI with order of magnitude 1
        // Multiplier happened to be split into 2
        // Value muliplied by only TWRSmallMultiplier is used as actual intermal TWR for movement, see MoveAtThrustToWeight method.
        public const float TWRSmallMultiplier = 14f;
		public const float TWRLargeMultiplier = 500f;
	}
}

