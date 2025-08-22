using System.Linq;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	public class Dialog_LoadShipDef : Dialog_RenameShip
	{
		private string ship = "shipdeftoload";
		private Map Map;
		private bool isWreck;
		public Dialog_LoadShipDef(string ship, Map map, bool isWreck = false)
		{
			curName = ship;
			Map = map;
			this.isWreck = isWreck;
		}
		protected override void SetName(string name)
		{
			if (name == ship || string.IsNullOrEmpty(name))
				return;
			if (DefDatabase<ShipDef>.GetNamedSilentFail(name) == null)
			{
				Log.Error("Ship not found in database: " + name);
				return;
			}
			if (!isWreck)
			{
				AttackableShip shipa = new AttackableShip();
				shipa.attackableShip = DefDatabase<ShipDef>.GetNamed(name);
				if (shipa.attackableShip.navyExclusive)
				{
					shipa.spaceNavyDef = DefDatabase<NavyDef>.AllDefs.Where(n => n.spaceShipDefs.Contains(shipa.attackableShip)).RandomElement();
					shipa.shipFaction = Find.FactionManager.AllFactions.Where(f => shipa.spaceNavyDef.factionDefs.Contains(f.def)).RandomElement();
				}
				Map.passingShipManager.AddShip(shipa);
			}
			else
			{
				DerelictShip ship = new DerelictShip();
				ship.wreckLevel = 3;
				ship.derelictShip = DefDatabase<ShipDef>.GetNamed(name);
				if (ship.derelictShip.navyExclusive)
				{
					ship.spaceNavyDef = DefDatabase<NavyDef>.AllDefs.Where(n => n.spaceShipDefs.Contains(ship.derelictShip)).RandomElement();
					ship.shipFaction = Find.FactionManager.AllFactions.Where(f => ship.spaceNavyDef.factionDefs.Contains(f.def)).RandomElement();
				}
				else
				{
					ship.shipFaction = Faction.OfAncientsHostile;
				}
				Map.passingShipManager.AddShip(ship);
			}
		}
	}
}