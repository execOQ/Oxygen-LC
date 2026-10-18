# <img src="https://i.imgur.com/0znYXqH.png" width=40 height=40> Oxygen 

### Information

Adds the need to have oxygen tanks behind your back. Moving around the planet also consumes oxygen, 
since not all planets are habitable. You can also breathe underwater! However, you have a limited amount of oxygen, 
don't forget to replenish the supply of oxygen cylinder on the ship! 

A low-oxygen alarm sounds every 5 seconds below 20% oxygen, becoming faster and higher-pitched
below 10% (every 2 seconds). It stops once oxygen reaches 20% again or the player dies.
The alarm works with the HUD hidden, independently of breathing sounds and notifications.
Under `Sounds` in the config, use `enableLowOxygenAlarm` to toggle it and
`lowOxygenAlarm_volume` to adjust its volume (0–1, default 0.65).

<details>
  <summary>Compatibility with other mods</summary>
  
  #### Compatible with: 
  
  * EladsHUD (screenshot below)
  * ShyHUD (HUD disappears if a oxygen value is > 75)
  * LCVR (screenshot below). Works with Arm HUD enabled and disabled
  * ImmersiveVisor (Additional oxygen consumption if the helmet is cracked)


  > Probably compatible with OopsAllFlooded but you'll find two oxygen canisters. One is from the Oxygen mod (OxyBoost) and one is from the OopsAllFlooded mod (Oxy-Canister). The working one is from this mod and is called OxyBoost.
</details>

<details>
  <summary>Spoiler</summary>
  
  ```
  Check the config file, there are plenty of settings :)
  ```
</details>

## Preview
### HUD
<details open>
  <summary>Vanilla</summary>
  <br>
  &nbsp;&nbsp;&nbsp;&nbsp;<img src="https://i.imgur.com/MAMfxmS.png" width=350 height=auto>
</details>
<details>
  <summary>EladsHUD</summary>
  <br>
  &nbsp;&nbsp;&nbsp;&nbsp;<img src="https://i.imgur.com/awUutxH.png" width=350 height=auto>
</details>
<details>
  <summary>LCVR</summary>
  <br>
  &nbsp;&nbsp;&nbsp;&nbsp;<img src="https://i.imgur.com/q19VwnN.png" width=650 height=auto>
</details>


### Items
<details open>
    <summary>OxyBoost</summary>
    <br>
    &nbsp;&nbsp;<img src="https://i.imgur.com/uvktf0C.png" width=350 height=auto>
  </details>

### Ways to refill oxygen
 - automatic oxygen refilling
    - when the player is on the ship
    - when the player on the ship and doors are closed (default)
 - using OxyBoost
 - using oxygen cylinders located on the ship

    <img src="https://i.imgur.com/UUKqrrt.png" width=350 height=auto>

## Configuration sync

Oxygen syncs gameplay settings directly from the host, including oxygen consumption, moon overrides, damage, refill behavior, compatibility rules, OxyBoost availability and price, OxyCharger availability, weather limits, and the die-early enable switch. Initial joins, late joins, and host edits receive a complete snapshot in chunks. Snapshots support up to 4 MiB; larger snapshots produce an error in the log.

HUD settings, notifications, sounds and volumes, and the die-early UI delay remain local. Host values are applied in memory and cleared when leaving the lobby; client config files keep their local settings. Existing config keys and the `MoonName@value;MoonName@value` format are unchanged.

Snapshots are validated, including every moon list, before becoming active. Rejected live host edits restore the last accepted values in the host's config and gameplay; later valid edits can still sync. If the host starts with invalid settings, the session uses defaults and logs the error without rewriting the startup config. Duplicate moon names and non-finite numeric values are rejected; empty separators are ignored.

Initial sync has a ten-second inactivity timeout. Valid chunks keep a slow transfer alive, and Oxygen avoids requesting another snapshot while one is arriving. A stalled transfer or five rejected snapshots disconnects the client with an explanation instead of leaving it on different gameplay rules.

CSync is no longer required by Oxygen. The host and all clients need a build with the new sync implementation; older builds cannot exchange settings with it. CSync may still be required by other installed mods.

OxyCharger supports ship build mode movement and storage. Use `oxycharger` in the terminal to return it from storage while enabled. Host config edits preserve its placement and storage state, including unsaved moves; disabling it restores the vanilla canisters. Storage and charger availability toggles preserve the remaining finite oxygen reserve.

The host allocates charger refills from the current reserve before clients receive oxygen. Simultaneous refills share the available amount, and retried requests cannot consume or grant oxygen twice. Confirmed grants survive charger storage; pending requests and replies are cleared when leaving a lobby.

Pending requests retry automatically even when automatic refill is disabled. The host rejects new allocations for dead players or unavailable player objects without consuming the reserve.

## Installation 

### R2ModMan or Thunderstore Manager (highly recommended)
 - Go to the Thunderstore page
 - Click Install with Mod Manager

### Manual
 - Go to the Thunderstore page
 - Click Manual Download
 - Unzip files
 - Extract Oxygen.dll into the plugins folder of BepInEx

<br>
<br>

El. Psy. Kongroo.
