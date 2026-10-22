## Version 1.7.3:

 - Fixed the missing "Fill oxygen" prompt that prevented manual refilling at OxyCharger on the ship (#39).

## Version 1.7.2:

 - Fixed malformed moon config entries causing all host settings to fall back to defaults, including correctly configured green moons.
 - Invalid entries are now skipped with a warning while valid entries and other host settings still apply. This covers green moons, outside and indoor oxygen drain, running multipliers, and underwater oxygen drain.
 - Duplicate moon names keep the first valid value. Invalid and non-finite numbers are ignored, and affected moons use the general setting instead.

## Version 1.7.1:

 - Reduced the default low-oxygen alarm volume from 65% to 10%.
 - Clarified the alarm config options: `lowOxygenAlarm_volume` adjusts volume from 0 to 1, and `enableLowOxygenAlarm = false` completely disables the repeating alarm.
 - Removed the Configuration sync section from the README.

 Existing configs keep their saved volume. Set `lowOxygenAlarm_volume = 0.1` under `Sounds` to use the quieter level.

## Version 1.7.0:

 - Replaced CSync with Oxygen's own host config sync. Large moon configs now sync completely instead of being cut off on clients.
 - Fixed config sync for late joins, live host edits, and reconnecting to a different host. HUD, sound, and notification preferences remain local, and joining a host doesn't overwrite your config file.
 - Added a configurable low-oxygen alarm below 20% oxygen. It becomes faster and higher-pitched below 10% and works even when the HUD is hidden.
 - Added support for moving, rotating, and storing OxyCharger in ship build mode. Use `oxycharger` in the terminal to return it from storage while enabled.
 - Fixed OxyCharger placement sync between players and moved its default position slightly away from the wall.
 - Fixed charger placement and storage state being lost when the host changes settings. Storing or disabling the charger now preserves its remaining oxygen reserve.
 - Fixed the vanilla canisters not being restored when OxyCharger is disabled, and prevented returning the charger from storage while disabled.
 - Fixed simultaneous refills using more oxygen than the charger has available. The host now shares the remaining reserve between requests.
 - Fixed manual refill retries when automatic refill is disabled. Retried requests can't consume or grant oxygen twice, and requests from dead players don't consume the reserve.
 - Fixed automatic ship refilling when OxyCharger is disabled and the ship's oxygen supply is unlimited.
 - Fixed OxyBoost shop availability and price following the wrong settings after joining or changing hosts.
 - Fixed carry weight overlapping the stamina and oxygen bars on ultrawide monitors (#25).
 - Removed repeated missing-player errors during normal lobby loading and disconnects.

 Host and clients must all update to a build with the new config sync. Oxygen no longer requires CSync, but other installed mods may still need it.

## Version 1.6.7:
 - Fixed OxyCharger sound playback bug

## Version 1.6.6:
 - Tested on v81

## Version 1.6.5:
 - Fixed a bug when the greenPlanets config option didn't work if a planet name had a whitespace
 - Now if the AutoFillingOnShip config option set to WhenDoorsClosed and ship's doors are not closed oxygen consumption will continue
 - Added a config option to toggle OxyBoost
 - Added a config option to toggle OxyCharger
 - Fixed compatibility with EladsHUD
 - Fixed compatibility with TooManyEmotes

## Version 1.6.4:
 - Fixed bug when picking up the OxyBoost will increase the player's weight from 0 lbs to 21 lbs
 - Added ability to die early if you can't get out from under the water for a long time (configurable in the config)

## Version 1.6.3:
 - Added the config option to control the 'autoHideHUD' option

## Version 1.6.2:
 - Added the config option to hide HUD without installing ShyHUD if the oxygen value > 75

## Version 1.6.1:
 - Fixed bug caused by disabling OxyCharger

## Version 1.6.0:
 - Added support for LCVR
 - NEW: Weather complications. When travelling to a planet with "eclipsed" weather conditions, the amount of oxygen on the ship is limited until you leave the planet. (configurable in the config). The amount of remained oxygen is synced between players.
 - Fixed compatibility with the "Oops! All Flooded!" mod (didn't add oxygen consumption underwater in the facility)
 - Fixed the problem with inaccurate vanilla HUD value display. (Thanks to the necrowing's DramaMask mod)
 - Reworked oxygen consumption when player is running (now, it always checks whether the player is running and, based on that, subtracts a value calculated from the stamina used)
 - Added auto-refill player's oxygen level when the round has just started
 - Added variance to auto-fill oxygen when the player is on a ship and the doors are closed
 - Updated oxygen HUD with the "EladsHUD" mod
 - Added audio sync between players in OxyCharger
 - Updated the audio logic to play a different sound if the player has just stopped being afraid
 - The "oxygenRunning" variable was replaced on the "runningMultiplier" in the config
 - Changed the name of some config entries (they should automatically migrate to new ones)
 - New variable "AccurateMeter" in the config

## Version 1.5.6:
 - Fixed bug when you could breathe in a facility
 - The "MakeItVanilla" option in the config has become obsolete.
 - Updated dependencies version

## Version 1.5.4:
 - Tested on v50
 - Updated CSync to 4.1.0

## Version 1.5.3:
 - fixed "comma-dots thing" bug in the config parser

## Version 1.5.2:
 - Fixed bug "OxygenUI is null"
 - Removed SFX volume limit

## Version 1.5.1:
 - Added an option in the config to disable automatic oxygen recovery once ship has left

## Version 1.5.0:
 - Quick Fix: If EladsHUD is enabled, the mod will damage you by 0.1 instead of 0.3 amounts of oxygen, and it will give you slightly more oxygen (you can disable this in the config)
 - Different moons now can have different oxygen settings
 - Reworked an audio (Now it depends on the player's state. E.g., if you're standing, the sound won't play. If you're walking, it will, but you can disable it in the config)
 - Support for ImmersiveVisor (if the helmet is severely broken, additional oxygen will be consumed)
 - Now, after the ship leaves, oxygen will be automatically recovered
 - Updated config (some variables were moved)
 - Fixes

## Version 1.4.0:
 - Added the ability to disable custom items for joining vanilla games in the config (it doesn't sync with the host)
 - Added a new way to refill oxygen, it's OxyCharger!

## Version 1.3.1
 - now Oxygen consumption on the company's planet can be disabled
 - Added possibility to disable SFX on "The Company" planet

## Version 1.3.0
 - Fixed HUD position on ultrawide monitors (i hope)
 - Added new item: OxyBoost
 - Moved to CSync fork
 - Reworked the descriptions in the config (two variables were replaced)

## Version 1.2.3
 - Added support for EladsHUD!

## Version 1.2.2
 - Now disabling consumption depends only on player's position (so diversity's rooms should also be included)
 - The "InfinityOxygenInbackrooms" variable in the config has been replaced with InfinityOxygenInModsPlaces.

## Version 1.2.1
- Support for CSync 2.0.0

## Version 1.2.0
 - Now, when a person is in the backrooms, oxygen will stop leaving and not being restored
 - Added config synchronization with host
 - Moved to latest C# version
 - Fixed usage of variable "multiplyDecreasingInFear" when player in fear
 - Added support for ShyHUD (hud disappears if oxygen value > 55)
 - Added new field in config for moving Oxygen HUD
 - Some code refactoring :)

## Version 1.1.0
 - Updated config
 - Changed SFX's
 - fixed statusEffectHUD's position and other problems :)
 - Added oxygen consumption when player in fear (and SFX for that)
 - Added backrooms simplification of gameplay (infinite oxygen in there)
 - OxygenHUD now displays the player's remaining time until death when they fall into water (can cause a problems with other mods)

## Version 1.0.2
 - Quick Fix: Disabled the mod's functionality when the player is underwater due to a bug discovered. This is temporary.

## Version 1.0.1
 - config descriptions corrected

## Version 1.0.0
 - Release
