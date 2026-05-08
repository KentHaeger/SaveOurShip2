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
		const int groupCount = 3;
		public int Count
		{
			get => groupCount;
		}
		private List<WeaponGroup> weaponGroups;
		private bool isEnabled = false;

		public WeaponGroupManager()
		{
			weaponGroups = new List<WeaponGroup>();
			for (int i = 0; i < Count; i++)
			{
				weaponGroups.Add(new WeaponGroup());
			}	
			// weaponGroups = new List<WeaponGroup>(new WeaponGroup[Count]);
		}

		public WeaponGroup this [int index]
		{
			get 
			{
				Assert.IsTrue(0 <= index && index < Count && index < weaponGroups.Count);
				Log.Message("Actual Count:" + weaponGroups.Count + " index: " + index + " Group count getter:" + Count);
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
