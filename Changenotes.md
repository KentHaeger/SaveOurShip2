Changes published Nov 24 2026

New things and notable changes:
- Quest shuttles can land on Shuttle Bays and Salvage Bays and don't need shuttle landing beacons in this case.
- Larger than 250x250 player ship map is supported when moving to a new planet. Requires initiating move to a new planet command in new version.
- Larger than 250x250 maps are supported for enemy ships.
- When entering enemy shuttles, player no longer looses control of the pawn, shuttle is claimed instead.
- Biotech DLC: kids will now have new learning desire, admiring space. Need some kind of telecope for that. That feature is intended to replace Skydreaming and Nature running which are not possible in space. Those learning desires are fixed, kid won't pick them in space.
- Bug Fix/Rework: Small Cannon and several other turret projectiles will no longer explode pematurely without hull damage.
	- They were doing damage generally, but not always.
	- In space battles, explosion size reduced from 5-tiles cross to 1 tile for small Cannon Turrets to make up for getting guaranteed hits. Their projectile for ground defence mode was not changed.
	- Damage value wasn't changed, as it affects Point Deffece efficieny.
	- Small Cannon efficiensy against hull is expected to stay roughly the same.
	- Same fix applied to shuttle laser which used to almost always explode prematurely and not damage hull.
- Quest (Spoilers): Finding starship bow will now have a timeout, so that scan results aren't spammed by quest location player can't complete in early game.
- Quest (Spoilers): JT Drive event map can be settled after quest is done.
Fixes:
- Fixed shuttles issue causing Null Reference Exceptions for shuttles in transit during ship battle.
- Scanners and science consoles will now automatically end observation for abandoned maps.
- Starship bow won't be found as general wreck, only as special location.
- Wall lights don't block tile for shuttles.
- Faster background rendering by Cn-mjt44
- Animals no longer un-tame when claiming wrecks.
- Fixed performance issue when using Vanilla Achievements Expanded and having large ship.
- Fixed destroying vehicles on map removal, which caused fleeing enemy ship with shuttles going wrong, map not removed.
- Fixed enemy ships fleeing for no reason.
- Can no longer exploit drive quest by doing it without claiming the drive.
- Glittertech salvage bay roof visual fixed.
- Formgel Going off map (such as in pawn lend quest) won't cause errors when decohering.
- Going to new panet: fixed saving minor (non main) player faction ideos. Pawns like Production and Research specialist from ancients should arrive to new planet in non-broken state now.
- Pawn jobs are now cleared when moving to a new planet. 
- Ships with NeverRandom tag won't appear in random fleets.
- Improved flicking airlocks performance.
- Breathable zone won't be automatically created on wrecks, only on ships. As auto-allowing zoned pawns to go to any wreck was dangerous.
- Ships with JT drive won't need RCS thrusters to move around now.
- Glittertech salvage bay will no longer retrieve landing/departing shuttles and other skyfallers.
- Many occupied containers such as Biotech ones like Softscanner/Ripscanner, Biosculpter pods and VFE Draincaskets will work correctly when moving ship, won't drop occupants.
- Fixed some buildings not working on ship/station start. Such as there is Advanced Research bench, but can't research.
- Abandon option is available for landed ship map. For this to work, entering landed ship map in new version is required.
- Restricted area around ship will check for obstacles correctly, showing items and filth. Previously, items and filth were disallowed in restricted yellow tiles around new ship location, but not for ship location itself. Caused real confusion on why can't move/land ship.
- Shuttles that are unable to launch because of rotated will now tell about that in warning.
- Archotech Spore no longer disappears when loading save with shuttle mid-flight.
- Fixed broken wall light causing ship move issues on Old Destroyer, Armed Transport, Firefly class transport. Staring ship battle in new version is required for fix to take effect. 
- Fixed incorrect fallback when being attacked by single enemy ship, that sometimes caused too hard enemies.
- Removed incorrect 2-layer hull plating on damaged satellites. 
- Fixed for the Vehicle related issue preventing shuttles from spawning on enemy ships.
- Dryads won't appear as randomly joining animals. A fix in Harmony patch preventing Archoitech animals there.
- "Need batteries" warning will now be correctly satisfied by ship battery.
- "Need meal source" warning won't be shown in space. 
- When converting airlocks to Archotech, their forbidden status will be preserved.
- When converting hull plating to Archotech, floors will be preserved.
- After pawns and mechs were moved aside to allow ship movement, they won't be stuck.
- Outer airlocks airlocks won't flick on landed ship, as they are not adjacent to vacuum in this case.
- With ship map physics enabled, docking extender floors will be correctly considered ship part and pawns won't fall off  to graveyard map from there.

Nov 29 2024
- Fixed ship lasers missing targets/working unreliable in ground defense mode.
- Adaptive Storage Compatibility implemented: on ship move, TotalSlots property for adaptive containers won't be incorrectly changed to 1, but is preserved instead
- Removed Need Warm Clothes Alert for space maps
- Fixed an issue, when pawns tried to reload torpedoes on non-claimed-yet derelict, but failed with error in TryMakePreToilResrvations() and were standing still. Now, pawns will only reload torpedo tubes belonging to their faction.
- Support for mini reactor graphics.

Dec 13 2024
- Archotech lung provides Toxic Environment resistance. That already existed on CE side when that mod is used.
- On ship/station start, map size from games setiings will be used.
- Some wrong animals like insects won't join on Animals join event on surface maps.
- Floor color will be preserved on ship move.
- Publicized turret stuff for Bioship addon.
- Royalty ending quest (hosting a Stellarch on current map) won't be generated for space maps.
- Charlon Whitestone (landed ship) will unfog on arrival, fixing parts of multi-layer walls never unfog.
- Empire reward Destroyer will have dignified throneroom and bedroom for Archon title.
- Heavy lags when Shutle bay or Salvage bay exposed to space fixed.
- When Ship map physics option is off, enemy boarding parties will arrive correcly without roof punching.

Dec 24 2024
- Fixed shield not appearing on player shuttle after save/load
- Heatsink, cloaking device and shields temporary disabled for enemy shuttles - won't generate.
- Fixed yellow log error on loading turrets - they were added with null upgrade key.
- Tox gas immunity for EVA helmets. Different thing from tox environment resistance. That already existed on CE side when that mod is used.
- Letters about found ships/derelicts will now mention submod name if found ship is not a part of Save Our Ship 2 content, but rather comes from ship pack.
Shuttle fixes in this update were observed to reduce amount of vehicle errors dramatically. Like ship battle with enemy Strike Carrier and no shutlle log errors at all, from start to running a game with enemy shuttles left after battle.

Jan 15 2025
- Over-encumbered shuttles should correctly refuse to launch, except for caravans made with Form and Reform Vehicle caravan
- Dev: Launch to specific tile command on ship bridge, when there is no player space map. Lets player pick a tile to be associated with the ship. Solar panels will work according to said tile latitude, quests and Ideology worksites will appear near that tile, also Vanilla drop pod launches to/from ship will likely be calculated using that tile.
- More options for wall attachments: can place them on unpowered hull, also on inactive sides of vents and solar panels.
- Kill all enemies on map and Vanish all enemies on map general dev commands now available.
- XML-ized anti-entropic heatbank recovery rate.
- On enemy ship/fleet defeat, their shuttles faction will be set to null, so they will no longer prevent capturing enemy ship buildings when non-vehicle pawns are deal with.

Jan 31 2025
- Gastronomy Compatibility handles dining tables and cash registers during ship move.
- Ship Capacitor Array HP 100 => 200, still fragile but not the same as small ship capacitor. Build time increased to be times larger than for small one too.
- EVA Helmets weights adjusted, now Heavy one is actually the heaviest, kids one is the lightest.
- Buildings that are visible over roof, specifically ship weapons, also Small and Large heatsinks support roof.
- From ship bridge or core, can now select all outer airlocks for that ship, easy way of forbidding pawns to leave ship and risk walking into engine exhaust.
- Fix for linking landed ship object with map by Ruifung.
- Moon base won't have empty space around Moon surface when larger map is picked. Locked to 250x250 map size due to how scenario is implemented.
- Message box with explanation will appear in case Load Ship scenario is used when there are no ships to load.
- Compatibility with Mechanoid Upgrades.
- Compatibility with Life Support System mod.
- Need RCS thrusters message will correctly say that at least one is needed, not many.

Feb 26 2025
- Many different shuttle errors were fixed.
- Shuttle heat will be saved and loaded correctly, except for off-map shuttles (in combat). Need to save and load in new version.
- Adaptive Storage compatibility fixed
- Outerdoor status will no longer stay short-term cached for airlocks after ship move.
- Buffed anti-entropy heatbank depletion recovery
- Starship bow timeout decreased from 30 to 12 days
- Charlon Whitestone will be clean of rock rubble and chunks inside
- Shuttle autodoc will tend pawns in cargo hold too
- Credits typo fixed
- Compatibility with CE for selecting turets from Tactical Console. Will take effect when CE is updated too

Mar 3 2025:
- Added support for multi-role buildings such as core and life support in one for Bioship addon
- fixed issue with shuttles broken/exploding after battle
- No Sightstealers incident (Anomaly) in space
- Enabled staring mechs, such as those in Mechanitor scenario, to spawn when starting in space
- More CE compatibility for turrets - in progress
- Fixed map size final limit being to low and map size not applied for Leviathan
 
Mar 25 2025:
- Additional logging for ship move
- Added abandon wreck confirmation
- Handled possible issues when moving things to graveyard with ship physics
- Fixed shuttle issue when AI moves one of their fleet ships to graveyard
- Fixed issue with moving to occupied graveyard tiles. Such as recovered hullfoam parts may fall off multiple times from the same location
- Fixed AI logic for Mechanoid Asteroid Base start
- Fixed texture path error message when Biotech DLC is not available
- Removed manning ship bridges for enemy pawns at graveyards, as that has no practical use, but affects performance on larger wrecks such as Starship Bow
- Added caching for Mental break thresholds, which significantly improves performance at Starship Bow
- Improved roof drawing performance, mostly affecting FPS at large wrecks such as Starship Bow
- Shuttle bays compatibility with small bays for personal shuttles. The change was missing from dev branch, but was in Steam release
- Fix for player ship with huge CR attacking pre-Moon base ship. The change was missing from dev branch, but was in Steam release. These two fixes by thecaffiend
- Fix for Mysterious Archotech Sphere to be able to be found via scanning
- Ideology system inactive option (from Ideology DLC startup settings) is supported in travel to new planet and load ship, will no longer force-activate ideology in that scenario

Apr 1 2025:
- Pawns won't get hypothermia in shuttles
- Fixed exploit: Archotech Thrusters always worked even when run out of fuel
- Clarified derelict message when it has bridges and claiming/deconstruction is not available because of that
- Fixed exploit: with Invisibility from Royalty, there was a very easy strategy for Starship Bow
- Technical improvement in mental break thresholds cache
- Fixed issue with destruction of multi-tile buildings in spinal weapon "wipeout zone" at source map

Apr 8 2025:
- Added letter for when pirates immediately attack player ship because of no powered comms console
- Win ship battle command
- Can select Ship Cryptosleep Casket occupant without ejecting them
- Increased hullfoam stack limit from 10 to 20
- Added bonuses from control sublink, which they can't install to formgel Mechanitors directly: AI formgel gets buffs like they have lvl 3 sublink, Archoech formgel like they have lvl 6 sublink
- Fixed oversized UI icon for RCS Thruster
- Added message for non-evident thing, purge port not purging due to being covered by active shield from other heat net/ship. Those aren't disabled automatically
- CE Compatibility, hopefully temporary fix: Enemies won't retreat based on threat calc because that does not work properly now, will still retreat because of other reasons. Only applicable when CE is enabled
- CE Compatibility, hopefully temporary fix: Enemy shuttles won't spawn with plasma turrets, because that caused issues. Only applicable when CE is enabled

May 3 2025:
- All EVA gear is now tradeable, but with decreased price
- Added Stabilize Forever Dev command for graveyard maps
- Engine exhaust zone offset moved to XML, as well as spinal kill zone width and offset from barrel center
- Small fix for immediate ememy retreat at the start of ship battle
- PDs will target first shulle instead of random one, that helps in more efficient ant-shuttle defense
- Enemy ship turrets won't beep continually when can't fire
- Fixed compatibility issue with Kurin Deluxe Edition [Vehicles]
- When player turret can't fire due to not enough energy/heat capacity, notification changed from repeating beeping message to repeating silent message
- Humanlikes are not allowed to use critterslep caskets, those are for animals
- Faster roofs update on unfog

May 15 2025:
- New feature: can move ship north/south on the world map

May 17 2025
- Fixed heat net update for ships directly at the edge of the map

May 28 2025
- Fixed loading resources from incorrect thread error in new scenario

June 1 2025
- Fixed ship buildings coordinates when rotating ship with certain buildings: non-rotatable, but having non-default rotation, such as Neat Storage large crate
- Fixed exlposion ticking error spam caused by exlposion moved to graveyard map
- Added missing purge worldComp when starting new game. Previously, it was possible to use old world comp from old game world.
- Fixed section layers update errors when landing ship
- Fixed null key use error and broken world view after orbital failure
- Fixed enemy boarding mechanoids not attacking player ship when thee are no exposed pawns/weapons
- Fixed time update when travelling to a new planet
- Fixed flipping double Torpedo Launchers (6x3) when flipping ship/wreck

June 3 2025
- Added support for unlocking all ships as starting ship options in submod. Nothing to use yet, wait for submod release.

June 11 2025
- Allow select outer airlocks command on planet surface, as requested by players.
- Fixed error when enemy boarding party arrived to defeated player ship.
- Stop forced target command made available for forced target in ground defense mode.
- Spinal weapons will auto-update and find their amplifiers and capacitor, no longer needed to select-to-update.
- Fixed several in-game texts
- Shuttle Laser and Plasma turrets can now auto-fire, toggle-able on turret gizmo. With this ability, they are too strong, so aiming time changed from 1.5 to 3.5 seconds.
