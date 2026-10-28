# <img src="https://raw.githubusercontent.com/execOQ/Oxygen-LC/master/docs/images/branding/oxygen-logo.png" width="40" height="40"> Oxygen

Adds oxygen management to Lethal Company. Your oxygen supply is limited — keep an eye on it and refill your tank before it runs out!

<details>
  <summary>Compatibility with other mods</summary>

- **EladsHUD** — alternative oxygen HUD layout.
- **ShyHUD** — oxygen HUD hiding support.
- **LCVR** — VR HUD and controller interaction, with Arm HUD enabled or disabled.
- **ImmersiveVisor** — additional oxygen consumption when the helmet is severely cracked.
- Configurable oxygen protection in special areas added by other mods.

Compatibility with OopsAllFlooded may vary. If both mods are installed, Oxygen's refill item is called **OxyBoost**; OopsAllFlooded adds a separate **Oxy-Canister**.

</details>

## Features

### NEW: Share oxygen with teammates

A gauge on each player's backpack shows their current oxygen level, even when the HUD is hidden.

> Transfers oxygen without loss, at 5% of tank capacity per second by default. The host can change the transfer rate in the config.

![Backpack oxygen gauge inside the facility](https://raw.githubusercontent.com/execOQ/Oxygen-LC/master/docs/images/oxygen-sharing/02-complex-backpack.png)

[More in-game screenshots](https://github.com/execOQ/Oxygen-LC/blob/master/docs/images/oxygen-sharing/README.md)

### Oxygen management

- Oxygen consumption outside and inside facilities.
- Additional consumption while running, scared or underwater.
- Breathe underwater while your tank still has oxygen.
- Configurable consumption rates for individual moons.
- Configurable breathable moons where outdoor oxygen consumption is disabled.
- Running out of oxygen causes damage and visual effects.
- Optional oxygen recovery at the start of a round and when the ship leaves.
- An optional hold-to-die action if you become trapped underwater.

### HUD Changes

Displays your remaining oxygen.

> Supports the vanilla HUD, EladsHUD and LCVR (works with Arm HUD enabled or disabled). Optional automatic hiding above a configurable oxygen level.

<table>
  <tr>
    <td align="center" width="33%">
      <img src="https://raw.githubusercontent.com/execOQ/Oxygen-LC/master/docs/images/hud/vanilla.png" width="300" alt="Vanilla oxygen HUD">
    </td>
    <td align="center" width="33%">
      <img src="https://raw.githubusercontent.com/execOQ/Oxygen-LC/master/docs/images/hud/eladshud.png" width="300" alt="Oxygen with EladsHUD">
    </td>
    <td align="center" width="33%">
      <img src="https://raw.githubusercontent.com/execOQ/Oxygen-LC/master/docs/images/hud/lcvr.png" width="300" alt="Oxygen HUD in LCVR">
    </td>
  </tr>
  <tr>
    <td align="center"><b>Vanilla</b></td>
    <td align="center"><b>EladsHUD</b></td>
    <td align="center"><b>LCVR</b></td>
  </tr>
</table>

### Sounds and warnings

- Breathing sounds respond to walking, running, exhaustion and fear.
- A low-oxygen alarm sounds below 20% oxygen and becomes more frequent below 10%.
- The alarm works even when the HUD is hidden.

> Configurable sound volumes, notifications and alarm controls.

### OxyBoost

A portable oxygen refill item available from the terminal store.

> Configurable price and refill amount. Can be enabled or disabled in the config.

<img src="https://raw.githubusercontent.com/execOQ/Oxygen-LC/master/docs/images/items/oxyboost.png" width="350" alt="OxyBoost">

### OxyCharger

Refill your oxygen using the cylinders on the ship.

- Supports manual refilling and optional automatic refilling.
- Can be moved, rotated and stored using ship build mode (Type `oxycharger` in the terminal to return it from storage).

> Can be enabled or disabled in the config.

<img src="https://raw.githubusercontent.com/execOQ/Oxygen-LC/master/docs/images/items/oxycharger.png" width="350" alt="OxyCharger">

### Automatic refilling

Choose when oxygen automatically refills:

- While the player is on the ship.
- Only while the player is on the ship and the doors are closed – the default.
- Disabled, requiring manual refills using OxyCharger.

> The refill rate is configurable.

### Weather complications

Eclipsed moons can limit the ship's oxygen supply until you leave the moon. The remaining ship reserve is shared between players.

> The oxygen limit and weather complication can be configured.

## Installation

### R2ModMan or Thunderstore Mod Manager

1. Open the mod's Thunderstore page.
2. Click **Install with Mod Manager**.

### Manual

1. Install BepInEx, LethalLib and LethalCompany InputUtils with their required dependencies.
2. Download and extract the Oxygen archive.
3. Place `Oxygen.dll` in `BepInEx/plugins`.

<br>

El. Psy. Kongroo.
