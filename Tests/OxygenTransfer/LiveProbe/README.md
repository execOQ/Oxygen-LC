# Native acceptance fixture

The launcher now defaults to the normal five-second oxygen interval for manual
sessions. Use **`-OxygenTickSeconds 10000` explicitly** for conservation/rate tests;
the old unconditional test interval hid normal consumption during manual VR checks.
`Assert-NativeVr.ps1` requires an actual running LCVR gameplay session and stereo
camera. It stages a diagnostic hand ray/button while leaving headset activation
and LCVR's native hover pass unpatched, checks donation in both directions, and
restores the five-second interval afterwards. `aimchecks` rejects nonfinite,
zero-direction and out-of-body reported rays. Automatic aim now targets the
visible gauge rather than assuming a fixed height of 1.3 metres.

`Start-Live.ps1 -LcvR` installs the downloaded official LCVR 1.5.0 package from
workspace `.multiplayer-test/lcvr-runtime-20261025/packages/LCVR` into the isolated
profile. It disables startup shortcuts and the obsolete TypeLoadExceptionFixer
there. The `native-vr.txt` marker prevents the fixture from patching Oxygen's
VR-activation getter. Native InVR, session readiness and XRDisplaySubsystem state
are captured in snapshots. `vrmenu lan` and `vrmenu host` invoke the normal menu
buttons. Native-runtime results, including the LCVR-only control failure, are in
`LCVR-RUNTIME-2026-10-25.md`.

`Assert-VrTrigger.ps1` is a separate simulation using the LCVR 1.5.0 bundle copied
into each ordinary LAN profile. It checks both triggerButton and analog trigger
against gripButton for both donor roles. It uses simulated headset activation
and does not claim an end-to-end LCVR runtime pass.

`Assert-DeviceInput.ps1` checks virtual Unity Gamepad input on two real LAN peers:
hold/release, donor HUD, conservation, removal, UI guards, rebinding and switching
back to keyboard. `Assert-VrInput.ps1` loads the unmodified VRActions input asset
from the locally cached LCVR 1.4.6 bundle and sends gripButton events from a virtual
XRController. The optional fixture patches only Oxygen's VR activation getter to
represent an attached headset; production input selection, validation, oxygen and
networking remain unpatched. PlayerInput's desktop device filter is cleared and
the virtual controller is paired explicitly. Camera/player poses are staged by
the existing fixture, without an XR tracking pipeline.

Copy the cached `lethalcompanyvr` bundle into each isolated profile before running
the VR script. No bundle or third-party DLL is redistributed by these tests.
This checks the LCVR input contract, not a running LCVR plugin/OpenXR runtime,
stereoscopic rendering, physical tracking, hand interaction or VR HUD layout.
Results and precise build hashes are in `INPUT-EMULATION-2026-10-25.md`.

The helper forces windowed settings in memory because DevelopmentStartup can
reload fullscreen preferences after command-line options. The originals are
restored by the normal save cleanup. This behavior is excluded from Oxygen.dll.

The repeated-transfer test waits until both donor and server see valid targeting
before sending input. Crouch and reconnect checks also wait for replicated state,
instead of relying on a fixed animation delay. `Assert-Mount.ps1` checks the
attachment after rendering, while `Assert-Soak.ps1 -Cycles 120` runs the extended
repetition campaign. The probe records start eligibility and interaction events
to distinguish fixture timing from production failures.

This optional helper is excluded from Oxygen.dll and the portable ledger tests.
It requires the installed game, BepInEx and a development LAN profile. Build it
against the actual installed game API, rather than the older reference DLL in
Assets:

```powershell
dotnet build LiveProbe.csproj -c Release -p:GameManagedDir="E:/Games/SteamLibrary/steamapps/common/Lethal Company/Lethal Company_Data/Managed"
```

Start-Live.ps1 uses this workspace's existing `.multiplayer-test/host/BepInEx`
profile as a seed, including LethalLib, InputUtils, FastStartup configured for LAN
and DevelopmentStartup. It creates a fresh run directory inside the workspace,
copies profiles, installs only the development DLL and this helper there, backs
up the game's saves and records process IDs/start times. Both startup roles need
AutoJoinLAN enabled so DevelopmentStartup's host mutex is created.

Run with the full native process view; a restricted shell may hide the running
game from duplicate-process guards. Background windows stay hidden. Start host,
then client, then optional client2, with the same absolute RunRoot. `-Restart` reuses a test profile and
the original save backup. `-ProxyPort 17778` on the client routes it through the
workspace UDP proxy; network assertions require that proxy to be running.

Assert-Live.ps1 has `basic`, `edges` and `network` phases. It controls player poses
using game RPCs, queues real Interact input, creates a floor/obstacle in the test
scene and writes assertions/snapshots. The network phase tests a rebound key,
crouching and finally native death, enabling the game's orbit death guards only
for that controlled test. It restores the proxy rule even after a failed check.

Assert-Concurrent.ps1 runs `parallel`, `refill`, `boost`, `inputs` and `death`
phases with three peers. Run them in that order; death is destructive to the test
session. These phases use native charger/item methods and virtual Gamepad input.
Before the death phase, Assert-Soak.ps1 runs 36 five-second cycles by default:
two remote donors to the host, late-client-to-earlier-client transfers, and host
to remote transfers. Every cycle checks conservation, tank bounds, release,
agreement across all three peers, gauge counts and stopped pump sources. This
is a three-minute repetition test, not an hours-long stability or leak proof.

Assert-Rejoin.ps1 validates a replacement client after the network phase. Record
the old client ID and host amount in `rejoin-before.json`, quit the old client,
and record the successful old-tank/gauge removal in `assertions-rejoin.json`.
Restart the client with the same RunRoot and a fresh proxy route, then run the
script. It checks identity replacement, preserved/synchronized host oxygen,
current gauges, and another native transfer. `-TransferOnly` is for repeating the
last check after fixture preparation; preserve the original failed attempt.
Start-Live.ps1 `-MoreSuits` copies this workspace's cached More Suits 1.5.4 package
into each isolated profile. Assert-Suits.ps1 selects Kirby/Skeleton on both peers
and checks gauge preservation; follow with the basic/edges transfer phases.

For presentation captures, `-Width 1920 -Height 1080` sets the initial window;
the helper's `showcase` command also increases the gameplay render texture.
`land` runs native landing with seed 24680, `noon` fixes daytime, and `enter` uses
the real facility entrance. `pose` locks a composition without creating the orbit
test floor in showcase mode. `aimheight`, `hud`, `crouch`, `filllight` and `scout`
help frame readable native captures. Supplemental lights and staged tank amounts
must be disclosed when sharing these presentation shots. The helper is not shipped.
`english` switches only the game thread to the US layout and queues a native
InputSystem configuration refresh; it does not replace prompt text. The original
thread layout is restored when the helper is destroyed.

Always run Stop-Live.ps1 afterward with the same RunRoot. It closes only recorded
test processes, preserves post-test save versions, restores originals and verifies
their hashes. Stop the separate proxy as well. The fixture's game/save defaults
are local to this development machine; adjust them explicitly for another setup.
