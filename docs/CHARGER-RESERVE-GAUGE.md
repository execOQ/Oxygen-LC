# Shared reserve gauge

The station carries an opaque, lit 3D gauge on the broad front face of its
cylinders. The case, worn materials, clamp and blue scale are shared with the
backpack gauge. All generated assets are released with their owning component.

The final user-requested mount is on the broad front (-Y in the source asset),
at local `(0.85, -0.22, 0.5)`, upright along the cylinders' +Z axis. The earlier
cabin-facing side mount has been replaced. A fresh desktop screenshot and native
storage/return check of this front mount are in
`.multiplayer-test/reserve-gauge-front-final-20261028/`. The new Release SHA-256 is
`140C44607CF11CC1D822AB20B8DB7B46DCBABCA880314BC32C4950EC662F0C25`.
The station uses the stock canister shader object to retain the game's stereo
shader variants. The gauge is a child of the station and has no colliders.

The blue arc reads `OxyCharger.RemainedOxygenAmount / ReserveCapacity`, clamped
to [0, 1]. `ReserveCapacity` is the server's configured initial round capacity,
preserved through storage/recreation by the existing weather lifecycle. Both
capacity and the active limit mode are server-written NGO network variables;
clients never use their personal tank or local config for this display.
The server refreshes the display metadata on spawn and during updates, including
phase transitions and host config edits. Existing refill allocation is unchanged.

Limited mode shows the remaining fraction; zero reserve leaves the dial blank.
The red empty flag from the initial implementation was removed at the user's
request. Float subtraction residue below one millionth of a personal tank also
displays as an empty dial, without modifying the actual reserve or allocation.
Unlimited mode shows a full blue scale and an infinity symbol, even if the
inactive limited reserve is empty.

## Validation — 2026-10-28

- Release build and NetcodePatcher: no errors or warnings.
- Charger lifecycle: 180 checks, including new gauge-state tests, existing
  automatic/competing refill checks, storage/recreation and session resets.
- Config sync: 372 checks.
- Oxygen transfer: 230 checks, including 100,000 competing transfers.
- Native LAN: host plus two clients, with client2 joining after depletion to 25%.
  All three receive the same remaining reserve, initial capacity and limit mode.
  Two remote refill requests consume the shared reserve and all three displays
  reach zero. Host transition to orbit changes every display to unlimited despite
  clients retaining their local Eclipsed flags. Weather/phase were staged in the
  fixture; this was not a full landed Eclipsed mission.
- Native desktop: checked placement, quarter-full blue arc, red empty flag and
  infinity display. Every gauge renderer uses the stock game shader object.

Workspace evidence is under `.multiplayer-test/reserve-gauge-20261028/`
(network assertions and snapshots) and
`.multiplayer-test/reserve-gauge-visual-20261028/` (final desktop screenshots and
renderer inspection). These paths are relative to the parent workspace, not this
repository. Test saves are backed up and restored by `Stop-Live.ps1`.

Reproduction: build the mod and LiveProbe in Release, start host/client with
`Start-Live.ps1`, issue `limited` and `reserve 0.25` to the host (default capacity
1), then start client2 and run `Assert-ReserveGauge.ps1 -RunRoot <run>`.
For screenshots, `showcase`, `reserveview true` and `filllight 10` stage the view.
The portable gauge fixture copies NGO snapshots; the LAN fixture verifies the
real NGO transport separately.

## Full landing and storage acceptance

`Assert-ReserveLifecycle.ps1` passed 21 native assertions with host and client on
the final build, SHA-256
`5790F1AF95748CB5C171BDB3B2431DA37A3B06751EBB29768586841C56F54953`.
The fixture selects Eclipsed weather on both peers before the game's normal
`StartGameServerRpc`. Level generation and ship landing run normally; landing,
weather and active limitation are observed on both peers. No phase/landed flags
are forced by this test.

Manual refill on each peer updates the remaining reserve and physical arc.
Natural automatic refill on both peers exhausts a half-tank reserve, increases
their combined personal oxygen by exactly that amount (within float tolerance),
and displays the empty flag on both stations. Three native placement/rotation
poses preserve the gauge's local mount, scale and readings. Three native storage
and return cycles respawn exactly one gauge per station with the same consumed
reserve and capacity, no remaining colliders and the stock shader. The final
desktop screenshot after returning from storage was visually inspected.

This exposed and fixed a display issue: two nominal half-tank refills could leave
`1.1920929e-7` in reserve, so the strictly-zero empty flag did not appear. The
display now tolerates this residue; two portable regressions cover it.

Evidence: `.multiplayer-test/reserve-gauge-acceptance-final-20261028/`, including
`assertions-reserve-lifecycle.json` and `host/reserve-after-storage.png`.
`assertions-physical-arc.json` independently checks the final arc vertices in 20
native snapshots against the expected fraction after refill, placement and return.
An initial test attempt retained as `assertions-before-frame-settle.json` sampled
disabled primitive colliders before Unity's deferred end-of-frame destruction;
the final check waits for destruction to settle. Run all phases from a fresh
host/client lobby, or use `-Phase remaining` to repeat automatic refill, placement
and storage after the landing/manual assertions.

## Physical LCVR acceptance remains blocked

LCVR 1.5.0 was launched with normal physical SteamVR drivers and separate VR
config/log directories. No null HMD or simulated VR activation was used.
SteamVR rejected the game with
`VRInitError_Driver_WirelessHmdNotConnected`; LCVR logged that OpenXR could not
initialize and only non-VR features were available. The native probe observed
`InVR == false` and no running XR display, so this is not a stereo acceptance pass.

Evidence: `.multiplayer-test/reserve-gauge-vr-runtime-20261028/steamvr-logs/vrserver.txt`
and `.multiplayer-test/reserve-gauge-vr-acceptance-20261028/host/BepInEx/LogOutput.log`.
To finish: connect the Quest 2 through Steam Link or Air Link, launch the isolated
profile with `Start-Live.ps1 -LcvR`, enter the normal LAN/host menu using `vrmenu lan`
and `vrmenu host`, and confirm `vrActiveNative`, `vrReadyNative`, `xrDisplayRunning`
and `stereoCamera` are true. Observe the station with the tracked headset camera;
do not use `reserveview`, which overrides camera pose for desktop screenshots.
Check partial, empty and unlimited readings, actual depth/lighting in both eyes
and readability from normal standing distance. Only then close the VR criterion.
Task remains In progress solely for this physical VR check.
