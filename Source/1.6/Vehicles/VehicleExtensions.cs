using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Vehicles;
using Vehicles.World;
using SmashTools.Targeting;
using Verse;

namespace SaveOurShip2.Vehicles
{
    public static class VehicleExtensions
    {
        private static int GetVehicleCrewSkill(this VehiclePawn vehicle, SkillDef skill)
        {
            int maxSkill = 0;
            foreach (Pawn pawn in vehicle.AllPawnsAboard)
            {
                if(CaravanUtility.IsOwner(pawn, vehicle.Faction))
                {
                    int currentSkill = pawn.skills?.GetSkill(skill)?.Level ?? -1;
                    bool hasPilotAssistant = pawn.health?.hediffSet?.hediffs?.Any((Hediff h) => h.def.defName == "PilotAssistant") ?? false;
                    if (currentSkill != -1 && skill == SkillDefOf.Intellectual && ModsConfig.OdysseyActive && hasPilotAssistant)
                    {
                        const int pilotAssistantIntellectualBonus = 2;
                        currentSkill += pilotAssistantIntellectualBonus;
                    }
                    maxSkill = Math.Max(maxSkill, currentSkill);
                }
            }
            return maxSkill;
        }

        private static List<(string upgradeKey, int skillValue)> upgrades = new List<(string, int)>
        {
            ("Droneautopilot", 5),
            ("Aiautopilot", 10),
            ("Archotechautopilot", 15)
        };

        public static int GetPilotIntellectualSkill(this VehiclePawn vehicle)
        {
            int result = vehicle.GetVehicleCrewSkill(SkillDefOf.Intellectual);
            if (upgrades != null && vehicle.CompUpgradeTree?.upgrades != null)
            {
                foreach (var (upgradeKey, skillValue) in upgrades)
                {
                    if (vehicle.CompUpgradeTree.upgrades.Contains(upgradeKey))
                    {
                        result = Math.Max(result, skillValue);
                    }
                }
            }
            return result;
        }

        public static int GetGunnerShootingSkill(this VehiclePawn vehicle)
        {
            int result = vehicle.GetVehicleCrewSkill(SkillDefOf.Shooting);
            if (upgrades != null)
            {
                foreach (var (upgradeKey, skillValue) in upgrades)
                {
                    if (vehicle.CompUpgradeTree.upgrades.Contains(upgradeKey))
                    {
                        result = Math.Max(result, skillValue);
                    }
                }
            }
            return result;
        }

        public static float GetPDDodgeChance(this VehiclePawn vehicle)
        {
            float dodgeChance = vehicle.GetStatValue(ResourceBank.VehicleStatDefOf.SoS2CombatDodgeChance) /
                Mathf.Lerp(120, 80, vehicle.GetPilotIntellectualSkill() / 20f);
            // PD damage to shuttle isn't really high, so should have some chance to hit even against max upgraded shuttle
            // 95%, dodge, resulting in 5% hits would be too few. Like even Cruiser class ships have < 20 PDS, so
            // Less than 1 hit in full PD salvo would be too clse to invincible
            const float maxDodgeChance = 0.85f;
            dodgeChance = Mathf.Clamp(dodgeChance, 0, maxDodgeChance);
            Log.Message("+DodgeChance:" + dodgeChance.ToString("P2"));
            return dodgeChance;
        }
    }
}
