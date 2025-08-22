using System;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;
using HarmonyLib;

namespace SaveOurShip2
{
	public class PawnPublicizer
	{
		// Makes tick, which was hidden in 1.6 update available again for mod purposes
		public static void DoTick(Pawn pawn)
		{
			MethodInfo tickMethod = typeof(Pawn).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic);
			tickMethod.Invoke(pawn, new object[] { });
		}
	}
}

