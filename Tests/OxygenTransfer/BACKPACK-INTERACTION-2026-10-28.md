# Backpack interaction fix — 2026-10-28

The reported manual failure exposed gaps in the earlier simulation. Both live
peers had registered oxygen tanks, so this was not a missing-network-registration
case. The manual launcher incorrectly retained a **10,000-second consumption
interval**, and a full recipient tank suppressed the interaction entirely.

The implementation also targeted a fixed player-root box at 1.3 metres instead
of the animated tank. Native recordings show upright and VR gauges at different
heights. VR targeting used the gameplay camera rather than LCVR's primary hand
ray, and the vanilla hover hook did not cover LCVR's own late hover pass.

## Changes

- Tank targeting follows the visible gauge's animated attachment.
- Native LCVR uses PrimaryController.InteractOrigin. The optional Harmony postfix
  refreshes oxygen text after VRController.LateUpdate and positions the native
  interaction canvas, preserving its IsHovering state.
- Command v2 carries the interaction ray. The host validates finite/bounded ray
  origins near the sender, rear approach, range, tank intersection and its own
  scene's line of sight. Only the host transfers oxygen; stale intents still expire.
- Full and empty tanks show an explanation instead of silently suppressing a tip.
- Manual launch defaults to five seconds. Conservation tests must explicitly
  request `-OxygenTickSeconds 10000`.

All peers must restart onto the same new DLL because the command format changed.
The latest installed DLL SHA-256 is:

`C16A0A8518322132646BC40BBEED409DABE752CF57E4F6402D20C4B6986BD85E`

## Verification completed

The first fix candidate, SHA-256
`00A443E5AA62BF86B7AE531FAA2272893B7274837A555D21716403A0283E319C`,
passed 18 native basic checks, 6 edge checks, 22 gamepad checks and 6 reported-ray
checks: **52 total**. The fixture now aims at the actual visible gauge. Coverage
includes transfers in both directions, rate/conservation/HUD synchronization,
release, walls, front/side approaches, range, capacity/empty limits, heartbeat
expiry, UI guards, rebinding, and rejection of zero/nonfinite/spoofed ray origins.

The 782 automated checks passed: transfer 230, charger lifecycle 168, config sync
372 and real BepInEx config APIs 12. Both production and excluded helper builds
completed with zero warnings/errors.

The latest DLL differs from that candidate only by allowing non-public native
LCVR hover/canvas members during reflection. It is installed on both current
test peers. Its normal-consumption recording shows the host decreasing from
0.6700003 to 0.6400003 over 17.5 seconds with an effective five-second interval.
The 52-check campaign belongs to the candidate hash above; it is not presented
as a separate complete campaign on the latest DLL.

## Native headset session verified; manual button check remains

After the headset was reconnected, both clients were restarted with the latest
C16A0A85 DLL. LCVR 1.5.0 used SteamVR/OpenXR 2.18.4, reached FOCUSED and created
a gameplay VRSession, running XR display and stereo camera. The probe did not
substitute the VR activation flag (`xrMode=false`).

`LiveProbe/Assert-NativeVr.ps1` completed **12/12 assertions** on that DLL:
native hover state and enabled cursor, VR-to-desktop and desktop-to-VR transfer,
HUD reconciliation, release, conservation, full-tank explanation and restoration
of the normal five-second interval. VR donation changed the donor from 0.8 to
0.6444046 and host from 0.3 to 0.4555954. In reverse, desktop oxygen fell to
0.6496092 while VR oxygen rose to 0.450390667; total oxygen remained 1.1.

The VR test temporarily supplied a diagnostic controller ray and software grip
events through the actual LCVR action asset; desktop input used a virtual gamepad.
It verifies native LCVR integration with a real headset, **not physical controller
button operation or a human visual assessment of the prompt**. Those remain for
the user's manual check. The temporary ray and devices were removed afterwards.

Earlier partial attempts are retained: menu automation ran before the relevant
menu existed; a reverse attempt lost the rear approach while the user turned;
and synthetic keyboard input did not sustain a transfer with hardware active.
The final reverse test passed using the virtual gamepad. No production changes
were made to bypass range, rear approach, line of sight or host validation.

Evidence is retained in workspace `.multiplayer-test`:

- `physical-vr-20261028`: original manual-session snapshots/events and save backup.
- `backpack-aim-fix-20261028`: the 52 candidate assertions and backups; those test
  processes were stopped and the three original saves restored with matching hashes.
- `physical-vr-fixed-20261028`: current installed peer hashes, normal-drain
  recording, earlier failed startup, partial attempts and the final 12 passing native VR assertions. These two interactive processes are
  left open; their original save backup remains available for cleanup after testing.

SteamVR's real headset configuration was not replaced by a null driver. No
release package was published or declared ready by this verification.

## Subsequent manual-session observation

After vrpointoff/vrinputstop and virtual-gamepad removal at 07:57:20Z,
no further synthetic input commands were sent. Hardware action controls resolved
to /OculusTouchControllerOpenXR1/grippressed. Following a setup-only reset
(host 0.35, VR 0.8 at 07:58:13Z), native BeginInteraction succeeded at
07:58:19Z. By 07:58:30Z host oxygen was 0.7346165 and VR oxygen 0.370383829,
consistent with transfer plus restored normal consumption. This supports actual
manual controller operation after removing the diagnostic ray; the later
observation window caught the released state, rather than the held-button frames.
Evidence: transfer-events.log and manual-input-observation.json in the live run.
