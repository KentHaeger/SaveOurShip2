using RimWorld.Planet;
using RimWorld;
using System;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using Verse.Sound;
using System.Diagnostics;
using System.Text;
using System.Linq;
using HarmonyLib;
using Vehicles;

namespace SaveOurShip2
{
	public class WorldObjectOrbitingShip : MapParent
	{
		private string nameInt;
		public string Name
		{
			get
			{
				return nameInt;
			}
			set
			{
				nameInt = value;
			}
		}
		public override string Label
		{
			get
			{
				if (nameInt == null)
				{
					return base.Label;
				}
				return nameInt;
			}
		}

		ShipMapComp mapComp => Map?.GetComponent<ShipMapComp>() ?? null;
		//used for orbit transition only
		public override Vector3 DrawPos
		{
			get
			{
				return drawPos;
			}
		}
		public Vector3 drawPos;
		public Vector3 originDrawPos = Vector3.zero;
		public Vector3 targetDrawPos = Vector3.zero;
		public Vector3 NominalPos => Vector3.SlerpUnclamped(WorldObjectMath.vecEquator * 150, WorldObjectMath.vecEquator * -150, 3);
		public void SetNominalPos()
		{
			radius = 150;
			Theta = -3;
		}
		//used in orbit
		public static Vector3 vecPolar = new Vector3(0, 1, 0);
		public OrbitalMovementDirection orbitalMove = new OrbitalMovementDirection();
		public bool preventMove = false;
		private float radius = 150; //altitude ~95-150
		private float phi = 0; //up/down on radius //td change to N/S orbital
		private float theta = -3; //E/W orbital on radius
		public float Radius
		{
			get { return radius; }
			set
			{
				radius = value;
				OrbitSet();
			}
		}
		public float Phi
		{
			get { return phi; }
			set
			{
				phi = value;
				OrbitSet();
			}
		}
		public float Theta
		{
			get { return theta; }
			set
			{
				theta = value;
				OrbitSet();
			}
		}
		void OrbitSet() //recalc on change only
		{
			drawPos = WorldObjectMath.GetPos(phi, theta, radius);
		}
		public override void SpawnSetup()
		{
			if (drawPos == Vector3.zero)
				OrbitSet();

			base.SpawnSetup();
		}

		protected override void Tick()
		{
			base.Tick();
			//move ship to next pos if player owned, on raretick, if nominal, not durring shuttle use
			if (orbitalMove.IsStopped())
				return;

			Theta = Theta + 0.0001f * orbitalMove.Theta;
			Phi = Phi + 0.0001f * orbitalMove.Phi;

			if (Find.TickManager.TicksGame % 60 == 0)
			{
				if (mapComp.ShipMapState != ShipMapState.nominal)
				{
					orbitalMove.Stop();
					return;
				}
				foreach (TravellingTransporters obj in Find.WorldObjects.TravellingTransporters)
				{
					int initialTile = obj.initialTile;
					if (initialTile == Tile || obj.destinationTile == Tile)
					{
						orbitalMove.Stop();
						return;
					}
				}
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<float>(ref theta, "theta", -3, false);
			Scribe_Values.Look<float>(ref phi, "phi", 0, false);
			Scribe_Values.Look<float>(ref radius, "radius", 150f, false);
			// Save game comaptibility for orbital move
			// Old saves: only orbitalMove field mening theta angle movement
			// New saves: orbitalMove field mening theta angle, also orbitalMovePhi
			Scribe_Values.Look<int>(ref orbitalMove.Theta, "orbitalMove", 0, true);
			Scribe_Values.Look<int>(ref orbitalMove.Phi, "orbitalMovePhi", 0, true);
			Scribe_Values.Look<string>(ref nameInt, "nameInt", null, false);
			Scribe_Values.Look<Vector3>(ref drawPos, "drawPos", Vector3.zero, false);
			Scribe_Values.Look<Vector3>(ref originDrawPos, "originDrawPos", Vector3.zero, false);
			Scribe_Values.Look<Vector3>(ref targetDrawPos, "targetDrawPos", Vector3.zero, false);
		}

		public override void Print(LayerSubMesh subMesh)
		{
			float averageTileSize = Find.WorldGrid.AverageTileSize;
			WorldRendererUtility.PrintQuadTangentialToPlanet(DrawPos, 1.7f * averageTileSize, 0.015f, subMesh, false, 0f, true);
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo g in base.GetGizmos())
			{
				yield return g;
			}
			if (HasMap)
			{
				yield return new Command_Action
				{
					defaultLabel = "CommandShowMap".Translate(), // Core\GameplayCommands.xml
					defaultDesc = "CommandShowMapDesc".Translate(), // Core\GameplayCommands.xml
					icon = ShowMapCommand,
					hotKey = KeyBindingDefOf.Misc1,
					action = delegate
					{
						Current.Game.CurrentMap = Map;
						if (!CameraJumper.TryHideWorld())
						{
							SoundDefOf.TabClose.PlayOneShotOnCamera(null);
						}
					}
				};
				if (def.canBePlayerHome)
				{
					yield return new Command_Action
					{
						defaultLabel = "SoS.AbandonHome".Translate(),
						defaultDesc = "SoS.AbandonHomeDesc".Translate(),
						icon = ContentFinder<Texture2D>.Get("UI/ShipAbandon_Icon", true),
						action = delegate
						{
							Map map = this.Map;
							if (map == null)
							{
								Destroy();
								SoundDefOf.Tick_High.PlayOneShotOnCamera();
								return;
							}

							foreach (TravellingTransporters obj in Find.WorldObjects.TravellingTransporters)
							{
								int initialTile = (int)Traverse.Create(obj).Field("initialTile").GetValue();
								if (initialTile == this.Tile || obj.destinationTile == this.Tile)
								{
									Messages.Message("SoS.ScuttleShipPods".Translate(), this, MessageTypeDefOf.NeutralEvent);
									return;
								}
							}
							StringBuilder stringBuilder = new StringBuilder();
							IEnumerable<Pawn> source = map.mapPawns.PawnsInFaction(Faction.OfPlayer).Where(pawn => !pawn.InContainerEnclosed || (pawn.ParentHolder is Thing && ((Thing)pawn.ParentHolder).def != ResourceBank.ThingDefOf.Ship_CryptosleepCasket));
							if (source.Any())
							{
								StringBuilder stringBuilder2 = new StringBuilder();
								foreach (Pawn item in source.OrderByDescending((Pawn x) => x.IsColonist))
								{
									if (stringBuilder2.Length > 0)
									{
										stringBuilder2.AppendLine();
									}
									stringBuilder2.Append("	" + item.LabelCap);
								}
								stringBuilder.Append("ConfirmAbandonHomeWithColonyPawns".Translate(stringBuilder2)); // Core\Dialogs_Various.xml
							}
							PawnDiedOrDownedThoughtsUtility.BuildMoodThoughtsListString(
								source, PawnDiedOrDownedThoughtsKind.Died, stringBuilder, null,
								"\n\n" + "ConfirmAbandonHomeNegativeThoughts_Everyone".Translate(), // Core\Dialogs_Various.xml
								"ConfirmAbandonHomeNegativeThoughts");
							if (stringBuilder.Length == 0)
							{
								Destroy();
								SoundDefOf.Tick_High.PlayOneShotOnCamera();
							}
							else
							{
								Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(stringBuilder.ToString(), delegate
								{
									Destroy();
								}));
							}
						}
					};
					if (!preventMove && mapComp.ShipMapState == ShipMapState.nominal && mapComp.ShipMapState != ShipMapState.burnUpSet)
					{
						Command_Action burnNorth = new Command_Action
						{
							action = delegate ()
							{
								orbitalMove = OrbitalMovementDirection.North;
							},
							defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.MoveNorth"),
							defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.MoveNorthDesc"),
							icon = ContentFinder<Texture2D>.Get("UI/Ship_Icon_North", true)
						};
						Command_Action burnWest = new Command_Action
						{
							action = delegate ()
							{
								orbitalMove = OrbitalMovementDirection.West;
							},
							defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.MoveWest"),
							defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.MoveWestDesc"),
							hotKey = KeyBindingDefOf.Misc2,
							icon = ContentFinder<Texture2D>.Get("UI/Ship_Icon_On_slow", true)
						};
						Command_Action burnStop = new Command_Action
						{
							action = delegate ()
							{
								orbitalMove = new OrbitalMovementDirection();
							},
							defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.MoveStop"),
							defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.MoveStopDesc"),
							hotKey = KeyBindingDefOf.Misc1,
							icon = ContentFinder<Texture2D>.Get("UI/Ship_Icon_Stop", true)
						};
						Command_Action burnEast = new Command_Action
						{
							action = delegate ()
							{
								orbitalMove = OrbitalMovementDirection.East;
							},
							defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.MoveEast"),
							defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.MoveEastDesc"),
							hotKey = KeyBindingDefOf.Misc3,
							icon = ContentFinder<Texture2D>.Get("UI/Ship_Icon_On_slow_rev", true)
						};
						Command_Action burnSouth = new Command_Action
						{
							action = delegate ()
							{
								orbitalMove = OrbitalMovementDirection.South;
							},
							defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.MoveSouth"),
							defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.MoveSouthDesc"),
							icon = ContentFinder<Texture2D>.Get("UI/Ship_Icon_South", true)
						};
						if (preventMove)
						{
							burnWest.disabled = true;
							burnStop.disabled = true;
							burnEast.disabled = true;
							burnNorth.disabled = true;
							burnSouth.disabled = true;
						}
						else if (orbitalMove.IsStopped())
						{
							burnStop.disabled = true;
						}
						else
						{
							// Disable currently active movemnt command
							if (orbitalMove == OrbitalMovementDirection.West)
							{
								burnWest.disabled = true;
							}
							if (orbitalMove == OrbitalMovementDirection.East)
							{
								burnEast.disabled = true;
							}
							if (orbitalMove == OrbitalMovementDirection.North)
							{
								burnNorth.disabled = true;
							}
							if (orbitalMove == OrbitalMovementDirection.South)
							{
								burnSouth.disabled = true;
							}
						}
						yield return burnNorth;
						yield return burnWest;
						yield return burnStop;
						yield return burnEast;
						yield return burnSouth;

					}
					if (Prefs.DevMode)
					{
						yield return new Command_Action
						{
							action = delegate ()
							{
								orbitalMove.Stop();
								// -1 is closer to being above partially generated planet surface than 0.
								Theta = -1f;
								Phi = 0f;
							},
							defaultLabel = "SoS.Dev.ShipPositionReset".Translate(),
							defaultDesc = "SoS.Dev.ShipPositionResetDesc".Translate(),
						};
					}
				}
				if (mapComp.ShipMapState == ShipMapState.isGraveyard && !mapComp.IsGraveOriginInCombat && mapComp.ShipMapState != ShipMapState.burnUpSet)
				{
					yield return new Command_Action
					{
						action = delegate
						{
							StringBuilder sb = new StringBuilder();
							sb.Append(TranslatorFormattedStringExtensions.Translate("SoS.LeaveGraveyardConfirmation1"));
							sb.Append(" ");
							// #bb8fo4 color commonly used in XML text highlihts
							sb.Append(Label.Colorize(new Color(0.733f, 0.561f, 0.016f)));
							sb.Append(" ");
							int colonistCount = mapComp.map.mapPawns.ColonistCount;
							int buildingCount = mapComp.map.listerBuildings.allBuildingsColonist.Count + mapComp.map.listerBuildings.allBuildingsNonColonist.Count;
							sb.Append(TranslatorFormattedStringExtensions.Translate("SoS.LeaveGraveyardConfirmation2", buildingCount));
							if (colonistCount > 0)
							{
								sb.Append("\n\n");
								sb.Append(TranslatorFormattedStringExtensions.Translate("SoS.LeaveGraveyardConfirmationColonistsParagraph", colonistCount));
							}
							Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(sb.ToString(), delegate
							{
								mapComp.ShipMapState = ShipMapState.burnUpSet;
							}));
						},
						defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.LeaveGraveyard"),
						defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.LeaveGraveyardDesc"),
						hotKey = KeyBindingDefOf.Misc5,
						icon = ContentFinder<Texture2D>.Get("UI/ShipAbandon_Icon", true)
					};
				}
				if (Prefs.DevMode && mapComp.ShipMapState != ShipMapState.burnUpSet)
				{
					yield return new Command_Action
					{
						defaultLabel = "SoS.Dev.RemoveShip".Translate(),
						defaultDesc = "SoS.Dev.RemoveShipDesc".Translate(),
						action = delegate
						{
							mapComp.ShipMapState = ShipMapState.burnUpSet;
						}
					};
				}
			}
		}
		public override string GetInspectString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			string inspectString = base.GetInspectString();
			if (!inspectString.NullOrEmpty())
			{
				stringBuilder.AppendLine(inspectString);
			}
			if (Prefs.DevMode)
			{
				stringBuilder.AppendLine("SoS.Dev.OrbitingShipInfo".Translate(mapComp.ShipMapState, mapComp.Altitude, radius, theta, phi, DrawPos, originDrawPos, targetDrawPos));
				// stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.Dev.OrbitingShipInfo", mapComp.ShipMapState, mapComp.Altitude, radius, theta, phi, DrawPos, originDrawPos, targetDrawPos));
				// Couldn't resolve translate method ambiguity
			}
			return stringBuilder.ToString().TrimEndNewlines();
		}
		public override void Destroy()
		{
			if (mapComp != null && mapComp.ShipMapState == ShipMapState.inCombat)
				mapComp.EndBattle(Map, false);
			if (Map != null && Map.mapPawns.AnyColonistSpawned)
			{
				Find.GameEnder.CheckOrUpdateGameOver();
			}
			base.Destroy();
			//base.Abandon();
		}

		public override MapGeneratorDef MapGeneratorDef
		{
			get
			{
				return DefDatabase<MapGeneratorDef>.GetNamed("EmptySpaceMap");
			}
		}

		public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
		{
			return new List<FloatMenuOption>();
		}

		[DebuggerHidden]
		public override IEnumerable<FloatMenuOption> GetTransportersFloatMenuOptions(IEnumerable<IThingHolder> pods, Action<PlanetTile, TransportersArrivalAction> launchAction)
		{
			return new List<FloatMenuOption>();
		}

		public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject) //on tick check to remove
		{
			if (mapComp.ShipMapState == ShipMapState.burnUpSet)
			{
				//td recheck all of this after VF, generally pods need origin to exist till they land
				foreach (TravellingTransporters obj in Find.WorldObjects.TravellingTransporters)
				{
					int initialTile = obj.initialTile;
					if (initialTile == Tile) //dont remove if pods in flight from this WO
					{
						alsoRemoveWorldObject = false;
						return false;
					}
					else if (obj.destinationTile == Tile) //divert from this WO to initial //td might not work
					{
						obj.destinationTile = initialTile;
						alsoRemoveWorldObject = false;
						return false;
					}
				}

				// Kill vehicles first, in order to avoid reservation issues when killing both normal pawns and vehicles using one pass
				List<VehiclePawn> vehiclesToKill = new List<VehiclePawn>();
				foreach (Thing t in Map.spawnedThings)
				{
					if (t is VehiclePawn v)
					{
						vehiclesToKill.Add(v);
					}
				}
				foreach (VehiclePawn v in vehiclesToKill)
				{
					v.Kill(new DamageInfo(DamageDefOf.Bomb, 99999));
				}

				//kill off pawns to prevent reappearance, tell player
				List<Pawn> toKill = new List<Pawn>();
				foreach (Thing t in Map.spawnedThings)
				{
					if (t is Pawn p)
						toKill.Add(p);
				}
				foreach (Pawn p in toKill)
				{
					p.Kill(new DamageInfo(DamageDefOf.Bomb, 99999));
				}
				if (toKill.Any(p => p.Faction == Faction.OfPlayer))
				{
					string letterString = "SoS.PawnsLostReEntry".Translate() + "\n\n";
					foreach (Pawn deadPawn in toKill.Where(p => p.Faction == Faction.OfPlayer))
						letterString += deadPawn.LabelShort + "\n";
					Find.LetterStack.ReceiveLetter("SoS.PawnsLostReEntryDesc".Translate(), letterString,
						LetterDefOf.NegativeEvent);
				}

				alsoRemoveWorldObject = true;
				return true;
			}
			alsoRemoveWorldObject = false;
			return false;
		}
	}

	public class OrbitalMovementDirection
	{
		// Compared to traditional spherical coordinates, these are exchanged
		// 0 is stop, -1 is move backwards, 1 is move forward
		public int Phi;
		public int Theta;
		public static readonly OrbitalMovementDirection West = new OrbitalMovementDirection(0, 1);
		public static readonly OrbitalMovementDirection East = new OrbitalMovementDirection(0, -1);
		public static readonly OrbitalMovementDirection North = new OrbitalMovementDirection(1, 0);
		public static readonly OrbitalMovementDirection South = new OrbitalMovementDirection(-1, 0);
		public OrbitalMovementDirection()
		{
		}
		public OrbitalMovementDirection(int phi, int theta)
		{
			Phi = phi;
			Theta = theta;
		}
		public void Stop()
		{
			Phi = 0;
			Theta = 0;
		}
		public bool IsStopped()
		{
			return Phi == 0 && Theta == 0;
		}
	}
}

