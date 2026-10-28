# Native oxygen transfer verification

Date: 2026-10-24. Source task: [Player-to-player oxygen transfer and backpack tank gauge](https://app.notion.com/p/402799f1b2ba81b79fb5df488b409fa3).

## Latest complete reliability rerun

The final visual DLL `406B766DE984CCD542A1556638A45CF4221080E58B235AE2570A3E88105ABAE5`
has now passed all 782 automated checks and 113 final native checks, including
the network, reconnect, More Suits, three-player, charger/OxyBoost and death
scenarios, plus 36 repeated-transfer cycles over 181.51 seconds. See
[the current-build report](RELIABILITY-2026-10-24.md) for evidence, two corrected
test-fixture issues and remaining coverage limits. Both runs were cleaned up
and original saves restored with verified hashes.

## Visual follow-up and GitHub captures

The subsequent visual build uses elongated marks across the band and a single active-device
Interact binding. Native facility captures confirmed and corrected the left/right
camera convention used by the standalone preview. The final DLL SHA-256 is
`406B766DE984CCD542A1556638A45CF4221080E58B235AE2570A3E88105ABAE5`.

Seven unretouched 1920 × 1080 screenshots on Experimentation and inside its
generated facility are in [the screenshot gallery](../../docs/images/oxygen-sharing/README.md).
The host's native held Interact moved the recipient from 20% to about 60%,
conserved the combined amount, and stopped after release. A separate fill ended
at 100% with no session or transfer prompt. The donor-empty path also stopped.
The five elongated/short edge marks and left-to-right arc were visually inspected
in the game. Presentation shots use an English US layout so the native prompt
shows E. The inside-only marks were an unselected design exploration and are
not used by this build or the final screenshot gallery.
Both copied profiles used the same DLL; both processes were closed and three
original saves were restored with verified hashes. This visual follow-up does
not rerun the full 55-check compatibility suite below.

## Prior acceptance build

Production DLL SHA-256:

`81B38B7BB8AB7E94B8097B9F13EEC8D0762A649DF68A1427970A84684BDF4F9F`

All 55 native checks pass on this build in Lethal Company v81:

- 9 three-peer checks: two remote donors, competing capacity limits, conservation,
  all three gauges on every peer and late-client-to-earlier-client donation.
- 5 charger checks: native manual refill while donating, finite shared reserve,
  conserved player-plus-reserve total and reserve agreement across peers.
- 5 OxyBoost checks: native item activation while donating, fuel consumption and
  total tank growth matching measured item output (0.06799945 of a tank).
- 5 input checks: Unity virtual Gamepad West starts/holds/releases Interact;
  chat and menu state stop donation.
- 3 recipient-death checks: native death stops both remote donors and clears targets.
- 4 More Suits 1.5.4 checks: loaded on both peers, Kirby/Skeleton materials selected
  on both avatars and both gauges retained with enabled renderers.
- 18 basic interaction/geometry and 6 tank/state checks rerun with More Suits active.

The new three-peer test exposed incomplete historical `actualClientId` data on a
late client. Player lookup now uses the replicated NGO `OwnerClientId` of spawned
player objects. This fixes the missing earlier-player gauge and remote-to-remote
donation. The latest standing screenshot was visually inspected: the white dial,
blue arc and rim remain visible with the cosmetic mod active.

Evidence roots outside the Git repository:

- `../.multiplayer-test/transfer-concurrent-final-20261024/`: assertions for
  `parallel`, `refill`, `boost`, `inputs` and `death` (27 checks).
- `../.multiplayer-test/transfer-suits-final-20261024/`: assertions for `suits`,
  `basic` and `edges` (28 checks), plus `host/backpack-rear.png`.

Both roots retain per-peer snapshots/logs, DLL/process manifests and save backups.
All test processes were stopped; original saves were restored and SHA-256 verified.
Release builds pass without warnings/errors, and all 782 portable checks pass
(230 transfer, 168 charger, 372 configuration sync and 12 real BepInEx API checks).

## Earlier network regression build

The following 42 checks ran before the late-client player-lookup fix; they were
not all rerun on the current DLL. Tested with a host and one remote LAN client,
matching Oxygen Release DLLs, LethalLib, InputUtils and isolated development
profiles. Production DLL SHA-256:

`E2C23FBC612089809EFA16AABD2F8ADC089D4DF5FB65C4918D3260085529860D`

### Results

All 42 native checks pass: 18 interaction/geometry checks, 6 tank/state checks,
14 transport/input/animation checks and 4 final reconnect checks. The helper controls player positions and
view using actual game RPCs, queues native keyboard events and observes production
custom messages and Unity physics. This is not a manual walking/controller playtest.

- Host-to-client and client-to-host donation, both HUDs, visible gauge renderers
  and pump playback agree with confirmed server state.
- Three seconds donated 0.1504626 of a tank (15.05%) at the configured 5% per second.
  Donor plus recipient oxygen remains conserved during transfers.
- Front, side, aim-away and obstructed starts fail. Releasing Interact, leaving
  range, entering a terminal, emptying the donor and filling the recipient stop.
- Recipient turns preserve an existing transfer. Adding a wall during transfer
  interrupts it. A donor's native death also stops it; the test explicitly enables
  the game's normal death path, which is disabled in orbit.
- A real loopback UDP proxy applies 100 ms delay each way, up to 25 ms jitter each
  way and 5% packet loss. Donation progresses, amounts remain conserved and release
  reaches the host. A complete outage expires the host's intent lease, halts
  donation and does not resume a released transfer when connectivity returns.
- Rebinding the actual game Interact action to physical F changes the localized
  prompt and still donates through the native input callback.
- After donor death and disconnect, a fresh client reconnects with two current
  gauges/tanks on both peers. The disconnected client's tank is removed, host
  oxygen is preserved and the joining client sees that existing amount. The test
  proxy uses its second route for the new UDP connection.
- A crouched recipient remains reachable. The gauge tracks the animated spine's
  lower position. The standing screenshot was visually inspected; the crouched
  screenshot is overexposed by the artificial test scene and is not evidence of
  crouched visual readability.

### Fixes found by the live tests

- The first gauge sat partly inside the tank. It now follows the animated spine
  while facing outward, beyond the tank surface. The standing dial is visible
  with its white face, blue arc and metal rim.
- Capping each update at exactly 100 ms reduced transfer speed between uneven
  frames. Integration now uses elapsed time capped by the 350 ms intent lease.
- PlayerInput can hold a different action asset from the global asset used by
  PlayerControllerB's input callbacks. Both polling and binding text now use that
  actual game Interact action, with a PlayerInput fallback.

### Evidence

Raw output is retained outside the Git repository in the workspace directory
`../.multiplayer-test/transfer-verified-20261024/`:

- `assertions-basic.json`, `assertions-edges.json`, `assertions-network.json` and
  `assertions-rejoin.json`.
- Per-peer `transfer-states.jsonl`, `transfer-events.log`, `Player.log` and
  `BepInEx/LogOutput.log`.
- `host/backpack-rear.png` and `host/backpack-crouched.png`.
- Proxy rule/counter files, process manifests and save backup hashes.

Earlier fixture attempts are in separate `transfer-live-*` and `transfer-final-*`
workspace directories. Only the matching hash and final assertions above support
the earlier-build results. The helper's API references, duplicate-process guards,
orbit pose synchronization and proxy rule-file retries were corrected during
fixture preparation; these were test-harness issues.

## Coverage boundaries

The harness controls poses through game RPCs and sends real InputSystem events;
it is not a manual walking or physical-controller playtest. VR input, interactions
with nearby pickup items, arbitrary replacement rigs, cosmetic mods beyond More
Suits 1.5.4, broad lighting conditions and subjective pump volume remain unverified.
The artificial crouched screenshot above is not visual-readability evidence.

The requested implementation and acceptance checks are complete within the stated
coverage. No release, repository push or installation in the user's regular mod
profile was performed.

Cleanup completed: the proxy and both game instances were stopped, and all three
original save files were restored and SHA-256 verified. Test save versions are
retained separately in `save-after`.
