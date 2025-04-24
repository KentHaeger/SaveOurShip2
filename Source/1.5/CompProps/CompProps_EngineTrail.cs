using System;
using Verse;

namespace SaveOurShip2
{

	public class CompProps_EngineTrail : CompProperties
	{
		//public GraphicData graphicData = new GraphicData();
		public float thrust = 0;
		public int fuelUse = 0;
		public int width = 0;
		public bool takeOff = false;
		public bool energy = false;
		public bool reactionless = false;
		// public List<IntVec2> killOffsetL = { new IntVec2(0, -13), new IntVec2(-13, 0), new IntVec2(0, 7), new IntVec2(7, 0) }.ToList();
		public IntVec2 killOffsetNorthL = new IntVec2(0, -13);
		public IntVec2 killOffsetEastL = new IntVec2(-13, 0);
		public IntVec2 killOffsetSouthL = new IntVec2(0, 7);
		public IntVec2 killOffsetWestL = new IntVec2(7, 0);

		public int killZoneLength = 15;
		public int killZoneWidth = 5;
		public int killZoneExtraOffset = 0;

		public IntVec2 killOffsetL(int rotInt)
		{
			switch (rotInt)
			{
				case Rot4.NorthInt:
					return killOffsetNorthL;
					break;
				case Rot4.EastInt:
					return killOffsetEastL;
					break;
				case Rot4.SouthInt:
					return killOffsetSouthL;
					break;
				case Rot4.WestInt:
					return killOffsetWestL;
					break;
				default:
					return new IntVec2();
			}
		}

		public SoundDef soundWorking;
		public SoundDef soundStart;
		public SoundDef soundEnd;
		public CompProps_EngineTrail()
		{
			this.compClass = typeof(CompEngineTrail);
		}
	}
}

