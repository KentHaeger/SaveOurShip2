﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	public class Dialog_NameShip : Dialog_RenameShip
	{
		private SpaceShipCache ship;

		public Dialog_NameShip(SpaceShipCache s)
		{
			ship = s;
			curName = s.Name;
		}

		protected override void SetName(string name)
		{
			if (name == ship.Name || string.IsNullOrEmpty(name))
				return;

			ship.Name = name;
		}
	}
}