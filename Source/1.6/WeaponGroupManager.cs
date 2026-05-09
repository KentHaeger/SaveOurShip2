using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using RimWorld;

namespace SaveOurShip2
{	
	public class WeaponGroupManager : IExposable
	{
		const int groupCount = 9;
		private List<WeaponGroup> weaponGroups;
		private bool isEnabled = false;

		public int Count
		{
			get => groupCount;
		}

		// Display all occupied groups plus next unoccupiied. So that if weapons are assigned to groups 1 and 2,
		// show only those group plus epty group 3, buut no mere until group 3 is actually used, reducing UI clutter.
		public int CountToDisplay
		{
			get
			{
				int maxOccupiedGroup = -1;
				for (int i = 0; i < weaponGroups.Count; i++)
				{
					if (weaponGroups[i].Count > 0)
					{
						maxOccupiedGroup = i;
					}
				}
				return maxOccupiedGroup + 2;
			}
		}

		public WeaponGroupManager()
		{
			weaponGroups = new List<WeaponGroup>();
			for (int i = 0; i < Count; i++)
			{
				weaponGroups.Add(new WeaponGroup());
			}	
		}

		public WeaponGroup this [int index]
		{
			get 
			{
				Assert.IsTrue(0 <= index && index < Count && index < weaponGroups.Count);
				// Log.Message("Actual Count:" + weaponGroups.Count + " index: " + index + " Group count getter:" + Count);
				return weaponGroups[index]; 
			}
		}

		public virtual void ExposeData()
		{
			Scribe_Values.Look<bool>(ref isEnabled, "isEnabled", defaultValue: false);
			Scribe_Collections.Look<WeaponGroup>(ref weaponGroups, "weaponGroups", LookMode.Deep);
			if (weaponGroups == null)
			{
				weaponGroups = new List<WeaponGroup>();
				for (int i = 0; i < Count; i++)
				{
					weaponGroups.Add(new WeaponGroup());
				}
			}
			if (weaponGroups.Count < Count)
			{
				for (int i = 0; i < Count - weaponGroups.Count; i++)
				{
					weaponGroups.Add(new WeaponGroup());
				}
			}
		}

		public void Select(int index)
		{
			Assert.IsTrue(0 <= index && index < Count && index < weaponGroups.Count);
			weaponGroups[index].Select();
		}

		public bool Enabled
		{ 
			get 
			{ 
				return isEnabled; 
			}
			set
			{
				isEnabled = value;				
			}
		}

	}
}
