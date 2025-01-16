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

Jan 15 2017
- Over-encumbered shuttles should correctly refuse to launch, except for caravans made with Form and Reform Vehicle caravan
- Dev: Launch to specific tile command on ship bridge, when there is no player space map. Lets player pick a tile to be associated with the ship. Solar panels will work according to said tile latitude, quests and Ideology worksites will appear near that tile, also Vanilla drop pod launches to/from ship will likely be calculated using that tile.
- More options for wall attachments: can place them on unpowered hull, also on inactive sides of vents and solar panels.
- Kill all enemies on map and Vanish all enemies on map general dev commands now available.
- XML-ized anti-entropic heatbank recovery rate.
- On enemy ship/fleet defeat, their shuttles faction will be set to null, so they will no longer prevent capturing enemy ship buildings when non-vehicle pawns are deal with.
