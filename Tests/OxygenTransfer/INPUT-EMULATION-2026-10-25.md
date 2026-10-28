# Input emulation verification — 2026-10-25

A subsequent real LCVR 1.5.0/SteamVR launch and additional grip/trigger checks are
documented in the [runtime report](LCVR-RUNTIME-2026-10-25.md). Runtime initialization
succeeded, but gameplay VR remains blocked by an LCVR startup exception reproduced
without Oxygen. The 80 checks below remain the earlier emulation/regression run.

**22 virtual gamepad checks, 34 simulated VR input checks and 24 native regression
checks passed on two real LAN game processes.** All 782 automated checks also
passed. Release and LiveProbe builds completed with zero warnings/errors.

Tested Oxygen.dll SHA-256 on both peers:

`47F154C938588768DE88400C15D0CBC8FD7A2A823EC28F908809206999680E11`

## Defects reproduced and corrected

On the previous DLL (`DF3F39C…`), a virtual Gamepad West press started transfer
but the prompt still displayed E. The game did not update its controller-mode
flag from that interaction alone. Prompts now resolve the actual last Interact
control, including rebound buttons and device removal.

A software XR grip press also activated LCVR's separate action while desktop
Interact stayed false; no transfer began and donor oxygen remained unchanged.
The production input adapter now selects LCVR's Interact action and starts on a
fresh press, since LCVR bypasses the vanilla callback. Holding before aiming at
the backpack does not start a transfer automatically. The normal hold/release,
host validation and network paths handle the rest.

Optional HUD integration now resolves VRSession through reflection: both
`LCVR.Player.VRSession` in the reference DLL and `LCVR.Managers.VRSession` in the
cached 1.4.6 DLL expose the required public static InVR/Instance properties.
Config.DisableArmHUD's property contract was inspected in both assemblies.
The production DLL no longer has an assembly reference to LCVR. Actual VR HUD
placement has not been tested by this emulation.

## Coverage

| Phase | Assertions | Result |
| --- | ---: | --- |
| Virtual Unity Gamepad | 22 | Passed |
| Simulated LCVR grip input | 34 | Passed |
| Native basic regression | 18 | Passed |
| Native edge regression | 6 | Passed |
| Total native assertions | 80 | Passed |

Gamepad tests cover host and remote donation, donor HUD decrease, conservation,
release, stable oxygen after release, removal while held, menu/chat/terminal
blocking and cancellation, West → South rebinding (X → A prompt), rejection of
the old button and switching back to keyboard.

VR input tests cover both donor roles, the genuine right-grip binding, donor and
recipient HUD values, conservation, release, menu/chat/terminal guards, aiming
away, aiming back while already holding, moving out of range, removing the held
controller, and returning to desktop input. Native regression checks additionally
cover side/front/away aim, wall obstruction, recipient turning, full/empty
tanks, remote donation and heartbeat expiry.

Automated suites: transfer 230 (including 100,000 competing transactions), charger
lifecycle 168, config sync 372 and real BepInEx configuration APIs 12.

## What was simulated

The fixture loads the unmodified VRActions input asset from the locally cached
LCVR 1.4.6 bundle. A software device extends Unity's XRController layout with a
gripButton; the original `<XRController>{RightHand}/gripButton` binding is used
without overrides in the passing run. The fixture pairs the software device with
the input asset and substitutes only Oxygen's headset-activation getter.
Production action selection, transfer validation, HUD oxygen values and LAN
messages remain unpatched. Player and gameplay-camera poses are staged by the
existing native fixture.

**LCVR itself and an OpenXR runtime were not running.** These results establish
the simulated input contract, not full VR compatibility. Physical controller
drivers, stereo rendering, head/hand tracking, LCVR's own hand interactions,
comfort and VR HUD placement still need a real VR session. Gamepad testing used
Unity's generic virtual Gamepad; it does not certify every hardware/driver.

## Evidence and cleanup

Raw records are retained in workspace `.multiplayer-test/input-emulation-fixed-20261025`:
`assertions-gamepad.json`, `assertions-vr-input.json`, `assertions-basic.json`,
`assertions-edges.json`, both peers' snapshots/events/Player.log and save backups.
The attached [machine-readable report](INPUT-EMULATION-2026-10-25.json) includes
every assertion. `LiveProbe/Summarize-Input-Emulation.py` validates those files,
peer hashes, error scans and original saves before regenerating it.

Earlier attempts are retained: the initial client connected before the host
finished registering; baseline input defects are in
`.multiplayer-test/input-emulation-final-20261025`; the first unchanged-binding
VR attempt retained PlayerInput's desktop device filter and timed out. Clearing
that fixture filter and explicitly pairing the virtual XR device resolved it.
The 22 gamepad assertions preceded that helper-only restart; both helper versions
tested the identical production DLL above.

Final host/client logs contain no Error/Fatal or native exception entries, and
the probe events contain no ERROR entries. Both test processes were stopped and
all three original saves were restored with matching SHA-256 hashes. No regular
mod profile was changed and no release was published.

The earlier 198-assertion three-player/network/soak matrix belongs to the
[previous DLL](FINAL-VERIFICATION-2026-10-24.md). It was not rerun in full on this
input-fix DLL. The publication archive is still outdated; this report does not
declare that archive ready for upload.
