- need to show role if the player is a fellow traitor, or detective - done
- join / start room (room codes) - done
- role assignment - done
- game state
  - game state UI in top right (ready up prompt, current role, other terrorists if terrorist)
  - ready up prompt - almost done
  - show players their role - done
  - count players alive / dead - done
  - show winners / losers - done
  - show team scores - done
  - timer - done
- scoreboard - done
- close-able doors
- show amounts of ammo on each player - done
- offline mode - done

- button for joining my own game - done
- esc disconnects - done
- lock cursor if lmb - done

- was hit effect (oof sound)
- network revive player - done
- network player arms and current weapon - done
- headshots working - done
- role select is slightly busted (random seed working?) - done
- disallow pickups if not in the round - done
- limit range of item pickup - done
- weapon scatter - done
- shotgun only load anims twice - done

- dont move spectator when alive - done
- show room name on map name slot - done
- show inventory ammo count on player screen - done
- show on the tab screen if a player is readied - done
- detective - done
- waiting for players if <3 - done

- can't play game twice - done
- can't start if a player joins late - done

- make sure two players don't spawn in the same place twice - have a current spawn index to distribute evenly? - done
- sound effect when shot - done
- weapon shot decal - done

- true random spawns - done
- late joining player is put in spectator mode
- tell players they were killed

from playtest:

fix:

- Can’t hear shooting - done
- Titles on pick ups broken - done
- Hunter spawning on top of Joseph might just need more spawns - done
- Escape menu - done
- Give ammo to start off - done
- Number keys for weapons - done

- When you join part way through it makes you traitor - done
- Reduce jump height - done
- If a player leaves you can’t ready up - done
- Get the lighting working - done
- Hit marker sounds broken - done
- say waiting for more players if less than min players - done

Unable to replicate, might be mac specific:

- No shot sounds - done
- Limit camera rotating up
- Mouse smoothing on Mac - done
- Reload. From 13 to 10 - done

- pickup double spawning
- networking color for player
- fix boundaries for houses

- Leave and join respawns
- Not obvious when round starts and ends - done
- Blood spurts on the hit player
- Directionality in sound - done
- Give shotgun its own sound
- Damage falloff to shotgun
- red splash when you’re shot
- Spectator started outside the map - done
- No animations for other players reloading or shooting

- No hit marker sounds
- Still too quiet when close to another player
- Spawned 2 traitors - done
- Set min players to 2 - done
- Uses 1 ammo but hears 2 bullets - done
- Other traitors thing isn’t working
- Died on my screen but not Josephs - done
- Don’t allow shooting when game isn’t in progress - done
- Still loading shotgun 4 times - done
- Team distribution broken - make sure there’s one terrorist and one non terrorist - done
- Can’t see someone reloading or shooting
- Pressed H twice and it assigned roles but said round hasn’t started
- Assigning role but not starting round

- Can’t hear the gun firing, hit marker has no directionality - maybe just replicating to all clients? - done
- Couldn’t hear footsteps - done
- Make rounds shorter - done
- Head shot sound not playing when it should be - done
- No directionality to the sounds - done
- Network reload sounds - done
- Clear objective screen - done
- Hearing the other players oof - done
- Footstep sounds - done
- Falloff to sound - done
- Blocking in sound - done
- Suicide button - done
- 2 traitors and 1 detective with 3 people - done
- Can’t hear gunshots - done
- Not enough bullets - done
- Wanting to climb ladders - done
- Weird visual bugs - red sea - done
- Still double spawning ammo - done
- Switch to pistol automatically on round start - done
- Text names should get longer in the tab menu - done
- Shotgun should be more powerful - 2 shot - done
- Make pistol ammo more common - done
- Couldn’t start game when readied up, just skipped to not ready
- Mouse cursor in middle of screen, not focused totally
- Reduce amount of ammo in shotgun - done
- Room name 100 hp - done
- Shoots when you close the game - done
- Can’t get out of the exit - done

Screens:

- Press H to ready up, show players not ready yet
- You are a Traitor full screen, show your objective
  - if innocent show their objectives
- you were killed by X, a traitor
- objective was fulfilled overlay
- red blood overlay

* Round started and ended that shows who the traitors were

for later:

- screams on death (heard globally, then look at scoreboard to see who's dead)
- icons for gun types, and weapon select UI
- tell players what to do with their role, and to join the game
- numbers on the weapon select
- instructions page
- Make game smaller?
- Color code players
- Terrorist button that halves the countdown

nice tech stuff:

- arbitrary timer class
- log of events / chat box

Should we add pickupable guns back? Might be really valuable to have a way to tell what someone was killed with.

activities:

- find a certain item that spawns randomly, 4-5 times
- press the 'terrorist button', or 3 buttons in series - plays sound effect when pressed

Terrorist button - pressing it doubles the timer
Terrorist detector device - only activated after half the countdown is over

Button location changes

Spawned at random

Log everything and display in window

Innocents have a series of buttons they can press, or pick ups, that when all are used it halves the timer

Reduce ammo spawns and put them in reasonable positions - done

Objectives list in the top left

Buildings from polygon adventure with interiors

Still seeing arms on grave - done
Couldn’t tell who was who - done
Don’t make the same person traitor twice in a row
Make name tags face player - done
Kean’s mouse was un locked and moving outside the screen window - done
Couldn’t see who was shooting who - done
Waiting for N players to ready up - done
All sounds are still too loud - reloading sounds sound like they’re right next to you
Pick ups not disappearing - done
Discrete ‘neighborhoods’ in the game world
Kill and ready up all players buttons

Make detective more obvious
Pistol 8 bullets - done
Update N waiting to be ready count - done
No ammo left and waiting for round prompt when can’t shoot
Double showing prompt of role
AK should do more damage - done
Make timer shorter - done
Make 2 traitor threshold a little higher - more than 4 players

Make it more obvious who detective is - done
Show who’s detective on the score screen - done
Make sure you can see who the other traitors are - done
Make sure detective can see what gun was used to kill someone

Place spawns closer together
Jumping graves - done

Assault rifle OP as shit - done
AK underpowered - done

Individual player scores - done
Show fellow traitor on name tags - done
Flash when you hit someone
Scroll to change weapons
Upload script

- handle if only a single terrorist is left - done
- clear other traitors display if round is over - done
- make sure detective works - i.e. seeing dead players' roles - done

- mouse accuracy slider - done
- shotgun spread - done
- vertical spray - done

- spray lerp
- make bullets easier to see remotely

- lerp head rotation
- lerp player position
- weapon syncing
- more variety in the map - fewer places to hide

- press h not updating - done
- change pickup type on round end
- change map on round end - done

- weird spawn positions sometimes
- lighting broken on pickups that aren't picked up

Release

- multiple maps
- 30fps on every target, 60fps on macbooks and pcs
- configurable map system
- pickups are solid, so are beacons
- tutorial
- gadgets (net gun and moon boots)

- read version number in game (put VERSION in resources folder and bump it before build)
- android / ios version at some point

- weapon switch UI bug when you pick up weapon with pistol enabled
- move spectator camera to a living player - done
- spawned in building
- can end up with 2 pistols