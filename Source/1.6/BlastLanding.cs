using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SaveOurShip2
{
	// Plasma "blast landing": a SOS2 ship carrying enough plasma firepower can land on an obstructed
	// spot - bombarding it on approach to destroy obstacles, blast off roofs, and fuse the ground to
	// volcanic glass. An original SOS2 mechanic - uses no Odyssey code and adds no Odyssey dependency.
	public static class BlastLanding
	{
		static TerrainDef glassTerrainCached;
		static bool glassTerrainResolved;
		public static TerrainDef GlassTerrain
		{
			get
			{
				if (!glassTerrainResolved)
				{
					glassTerrainCached = DefDatabase<TerrainDef>.GetNamedSilentFail("ShipVolcanicGlass");
					glassTerrainResolved = true;
				}
				return glassTerrainCached;
			}
		}

		// True if the map is space - SOS2 outer space, or Odyssey's own orbital void. A SOS2 ship
		// "landing" on either is a space arrival, not a ground landing, so blast landing never applies.
		public static bool IsSpaceLanding(Map map)
		{
			if (map == null)
				return false;
			if (map.IsSpace())
				return true;
			if (ModsConfig.OdysseyActive && map.Biome != null
				&& (map.Biome.defName == "Space" || map.Biome.defName == "Orbit"))
				return true;
			return false;
		}

		// --- capability: does the ship pack enough plasma firepower and energy capacity ---
		public static bool CanBlastLand(SpaceShipCache ship)
		{
			if (ship == null)
				return false;
			int firepower = 0;
			foreach (Building_ShipTurret t in ship.Turrets)
			{
				if (!IsPlasma(t))
					continue;
				if (t.spinalComp != null)
					firepower += 3; //spinal plasma lance
				else if (t.def.defName.Contains("Large"))
					firepower += 2;
				else
					firepower += 1;
			}
			if (firepower < 2) //soft bar: one large or spinal plasma weapon, or two small plasma turrets
				return false;
			//reasonable energy capacity to dump into the bombardment
			return ship.Buildings.Any(b => b.TryGetComp<CompPowerBattery>() != null);
		}

		static bool IsPlasma(Building_ShipTurret t)
		{
			if (t == null)
				return false;
			if (t.def.defName.Contains("Plasma"))
				return true;
			return t.gun != null && t.gun.def != null && t.gun.def.defName.Contains("Plasma");
		}

		// --- target analysis ---
		// Cells that can never be landed on, even with a bombardment.
		public static bool CellIsHardBlocked(IntVec3 c, Map map)
		{
			if (map.roofGrid.RoofAt(c) == RoofDefOf.RoofRockThick) //overhead mountain - indestructible
				return true;
			//scripted no-landing zones (Odyssey quest sites use these; field exists in Core, list empty otherwise)
			if (map.landingBlockers != null)
			{
				foreach (CellRect rect in map.landingBlockers)
				{
					if (rect.Contains(c))
						return true;
				}
			}
			foreach (Thing t in c.GetThingList(map))
			{
				if (t is Building_SteamGeyser)
					return true;
				//indestructible building (e.g. ancient quest walls) - blast can't clear it,
				//so reject the cell rather than pound it forever every salvo
				if (t is Building && !t.def.destroyable)
					return true;
				//things flagged as never-land-on (e.g. monoliths, special items) - respect even if destroyable,
				//to match vanilla gravship-landing intent
				if (t.def.preventGravshipLandingOn)
					return true;
			}
			return false;
		}

		// Cells that are landable only after a bombardment clears them.
		// Only cells a normal landing genuinely cannot use - so blast landing engages only when needed.
		public static bool CellNeedsBlast(IntVec3 c, Map map)
		{
			if (map.roofGrid.Roofed(c)) //any roof (overhead mountain is treated as hard-blocked separately)
				return true;
			if (!c.GetAffordances(map).Contains(TerrainAffordanceDefOf.Heavy)) //water, marsh, soft ground
				return true;
			//constructed floor (concrete, carpet, wood, etc.) - heavy enough to land on, but blast
			//landing fuses everything to volcanic glass, so floors in the LZ get vaporized too
			if (c.GetTerrain(map).IsFloor)
				return true;
			foreach (Thing t in c.GetThingList(map))
			{
				//buildings, walls, rock, enemy structures block a landing. Trees do NOT - a normal
				//SOS2 landing already wipes plants as it sets down, so trees never trigger a blast.
				if (t is Building && !(t is Building_SteamGeyser))
					return true;
			}
			return false;
		}

		public static bool FootprintNeedsBlast(IEnumerable<IntVec3> footprint, Map map)
		{
			foreach (IntVec3 c in footprint)
			{
				if (c.InBounds(map) && !CellIsHardBlocked(c, map) && CellNeedsBlast(c, map))
					return true;
			}
			return false;
		}

		// --- the bombardment ---
		// The target-map half of one salvo - a few glassing rounds rain on the LZ, plus scattered
		// flames. ShipMapComp delivers this ~10s after the ship-side FireShipTurrets that launched it,
		// simulating the distance the plasma must cross.
		public static void BlastTargetSalvo(Map targetMap, HashSet<IntVec3> footprint)
		{
			if (targetMap != null && footprint != null && footprint.Count > 0)
			{
				List<IntVec3> cells = footprint.Where(c => c.InBounds(targetMap)).ToList();
				if (cells.Count > 0)
				{
					//cells still holding an obstacle - building, wall, rock, roof or unsuitable terrain.
					//Hammer these first every salvo so the landing zone is reliably cleared, not just
					//randomly glassed. As obstacles fall the list shrinks and rounds spread to glass.
					List<IntVec3> obstacles = cells.Where(c => CellNeedsBlast(c, targetMap)).ToList();
					ThingDef proj = DefDatabase<ThingDef>.GetNamedSilentFail("Proj_BlastLandingGlass");
					int rounds = Rand.RangeInclusive(4, 8);
					for (int i = 0; i < rounds; i++)
					{
						IntVec3 target = obstacles.Count > 0 ? obstacles.RandomElement() : cells.RandomElement();
						if (proj != null)
						{
							//launch from above and aside so the round visibly arcs down onto the zone
							IntVec3 src = target + new IntVec3(Rand.Range(-7, 8), 0, Rand.Range(10, 22));
							if (!src.InBounds(targetMap))
								src = target;
							Projectile p = (Projectile)GenSpawn.Spawn(proj, src, targetMap);
							p.Launch(null, src.ToVector3Shifted(), target, target, ProjectileHitFlags.All);
						}
						else //fallback if the projectile def is missing - glass the area directly
						{
							foreach (IntVec3 c in GenRadial.RadialCellsAround(target, 3f, true))
								GlassCell(targetMap, c);
						}
					}
				}
			}
			ScatterFlames(targetMap, footprint);
		}

		static SoundDef plasmaFireSound;
		// Plasma turrets carry their fire sound on the verb (soundCast ShipCombatPlasma), not on the
		// heat comp's singleFireSound - which is null for them, hence the silent turrets.
		static SoundDef PlasmaFireSound => plasmaFireSound ?? (plasmaFireSound = DefDatabase<SoundDef>.GetNamedSilentFail("ShipCombatPlasma"));

		// Ship side of the bombardment: the ship's plasma turrets visibly fire. The camera sits on the
		// ship by default; the two maps can't be seen at once, so this needn't sync with the rain on
		// the target map - turret count and timing are deliberately loose (fake muzzle fire).
		public static void FireShipTurrets(SpaceShipCache ship)
		{
			if (ship == null || ship.Map == null)
				return;
			Map map = ship.Map;
			ThingDef bolt = DefDatabase<ThingDef>.GetNamedSilentFail("Proj_BlastLandingMuzzle");
			//fire aligned with the engines so the volley reads as the ship shooting "forward"
			ShipMapComp mc = map.GetComponent<ShipMapComp>();
			int engineRot = (mc != null && mc.EngineRot >= 0) ? mc.EngineRot : 0;
			IntVec3 dir = new Rot4(engineRot).FacingCell;
			foreach (Building_ShipTurret t in ship.Turrets)
			{
				if (!IsPlasma(t) || !t.Spawned)
					continue;
				//hold the gun aimed in the engine direction - the turret's own tracking swings it there
				t.SetBlastAim(new LocalTargetInfo(t.Position + dir * 30));
				if (!Rand.Chance(0.6f)) //only some turrets loose a bolt each salvo
					continue;
				(t.heatComp?.Props?.singleFireSound ?? PlasmaFireSound)?.PlayOneShot(t);
				FleckMaker.ThrowFireGlow(t.Position.ToVector3Shifted(), map, 2f);
				if (bolt != null)
				{
					//streak a plain bolt in the engine direction - a visible muzzle flash leaving the ship
					IntVec3 raw = t.Position + dir * 50 + new IntVec3(Rand.Range(-2, 3), 0, Rand.Range(-2, 3));
					IntVec3 target = new IntVec3(Mathf.Clamp(raw.x, 0, map.Size.x - 1), 0, Mathf.Clamp(raw.z, 0, map.Size.z - 1));
					Projectile proj = (Projectile)GenSpawn.Spawn(bolt, t.Position, map);
					proj.Launch(t, t.DrawPos, target, target, ProjectileHitFlags.None);
				}
			}
		}

		static void StripRoof(Map map, IntVec3 c)
		{
			if (map.roofGrid.Roofed(c) && map.roofGrid.RoofAt(c) != RoofDefOf.RoofRockThick)
				map.roofGrid.SetRoof(c, null);
		}

		// Carves and glasses a single cell - called by each glassing round on impact.
		public static void GlassCell(Map map, IntVec3 c)
		{
			if (map == null || !c.InBounds(map) || CellIsHardBlocked(c, map))
				return;
			TerrainDef glass = GlassTerrain;
			bool firstGlassing = glass == null || map.terrainGrid.TerrainAt(c) != glass;
			StripRoof(map, c);
			foreach (Thing t in c.GetThingList(map).ToList())
			{
				//never destroy geysers, the ship's own blueprint/frames, or motes -
				//destroying the heat-haze motes every overlapping blast kills the molten look
				if (t is Mote || t is Building_SteamGeyser || t is Blueprint || t is Frame)
					continue;
				//never wreck a SOS2 ship part - guards against a stray late round hitting the landed ship
				if (t is Building bld && bld.TryGetComp<CompShipCachePart>() != null)
					continue;
				//pawns standing in a glassing round take plasma burn damage, but are never Vanished -
				//that would skip the death event (no corpse, no notify). Let damage kill them properly.
				if (t is Pawn p)
				{
					if (!p.Dead)
						p.TakeDamage(new DamageInfo(DamageDefOf.Burn, 12f, instigator: null));
					continue;
				}
				if (t.def.destroyable && !t.Destroyed)
					t.Destroy(DestroyMode.Vanish);
			}
			if (glass != null)
				map.terrainGrid.SetTerrain(c, glass);
			//heat haze off the freshly fused glass - only on a cell's first glassing, to bound mote count
			if (firstGlassing && Rand.Chance(0.4f))
			{
				ThingDef heat = DefDatabase<ThingDef>.GetNamedSilentFail("Mote_BlastLandingHeat");
				if (heat != null)
					MoteMaker.MakeStaticMote(c.ToVector3Shifted(), map, heat, Rand.Range(1.4f, 2.2f));
			}
		}

		// Kindles a few fires on vegetated ground OUTSIDE the blast area - inside it everything is
		// destroyed, so a fire set there has no fuel and dies instantly.
		public static void ScatterFlames(Map map, HashSet<IntVec3> footprint)
		{
			if (map == null || footprint == null || footprint.Count == 0)
				return;
			List<IntVec3> fp = footprint.ToList();
			int fires = Rand.RangeInclusive(2, 4);
			for (int i = 0; i < fires; i++)
			{
				for (int attempt = 0; attempt < 8; attempt++)
				{
					//step well clear of the footprint (and its glassed bleed) onto living vegetation
					IntVec3 c = fp.RandomElement() + GenAdj.AdjacentCells[Rand.Range(0, 8)] * Rand.Range(5, 10);
					if (c.InBounds(map) && !footprint.Contains(c) && c.GetPlant(map) != null)
					{
						FireUtility.TryStartFireIn(c, map, Rand.Range(0.2f, 0.5f), null);
						break;
					}
				}
			}
		}

		// Each glassing impact panics nearby pawns into PanicFlee mental state, so survivors of
		// the first hit run clear of the LZ before subsequent salvos land - mirrors how pawns flee
		// from the Ideology hacked ancient drone before it self-destructs.
		public static void PanicFleeNearby(Map map, IntVec3 center, float radius)
		{
			if (map == null)
				return;
			foreach (Pawn p in map.mapPawns.AllPawnsSpawned.ToList())
			{
				if (p.Dead || p.Downed || p.mindState == null)
					continue;
				if (!p.Position.InHorDistOf(center, radius))
					continue;
				//already panic-fleeing - leave them be, don't restart the state every salvo
				if (p.InMentalState && p.MentalStateDef == MentalStateDefOf.PanicFlee)
					continue;
				p.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee,
					reason: null, forced: false, forceWake: false, causedByMood: false,
					otherPawn: null, transitionSilently: true, causedByDamage: true);
			}
		}

		// Clears blast-landing heat motes and fires from the cells the ship now occupies.
		public static void ClearLandingZone(Map map, IEnumerable<IntVec3> cells)
		{
			if (map == null || cells == null)
				return;
			ThingDef heat = DefDatabase<ThingDef>.GetNamedSilentFail("Mote_BlastLandingHeat");
			foreach (IntVec3 c in cells)
			{
				if (!c.InBounds(map))
					continue;
				foreach (Thing t in c.GetThingList(map).ToList())
				{
					if (t is Fire || (heat != null && t.def == heat))
						t.Destroy(DestroyMode.Vanish);
				}
			}
		}
	}

	// A plasma bombardment round that fuses the ground it strikes into volcanic glass. Glassing is
	// driven by the impact itself, so the landing scar forms causally and raggedly from where rounds land.
	public class Projectile_BlastLandingGlass : Projectile_Explosive
	{
		protected override void Impact(Thing hitThing, bool blockedByShield = false)
		{
			Map map = Map;
			IntVec3 pos = Position;
			base.Impact(hitThing, blockedByShield); //plasma explosion - recognizable plasma blast and damage
			if (map == null)
				return;
			foreach (IntVec3 c in GenRadial.RadialCellsAround(pos, 3.4f, true))
				BlastLanding.GlassCell(map, c);
			//panic-flee pawns in a wider radius than the glassing - survivors of the first hit run
			//before the next salvo arrives. Radius covers the LZ shoulder, not just the impact center.
			BlastLanding.PanicFleeNearby(map, pos, 12f);
		}
	}
}
