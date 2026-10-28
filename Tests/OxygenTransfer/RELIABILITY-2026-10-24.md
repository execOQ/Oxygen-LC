# Oxygen transfer reliability verification

Date: 2026-10-24. Lethal Company v81, isolated local LAN profiles.

**Result: 782 automated checks and 113 final native checks passed.** No production
Oxygen defect was found by this rerun, and the production code was not changed
during it. This establishes the coverage below, not a guarantee of failure-free
operation in every environment.

Release Oxygen.dll SHA-256, identical on all tested peers:

`406B766DE984CCD542A1556638A45CF4221080E58B235AE2570A3E88105ABAE5`

The subsequent gauge appearance and mounting revision is recorded separately in
[GAUGE-FIT-2026-10-24.md](GAUGE-FIT-2026-10-24.md). The 113-check run here applies
to the DLL hash above.

The Release build completed with zero warnings and errors. `git diff --check`
passed; Git's existing LF/CRLF conversion notices are not build warnings.

## Automated checks

| Suite | Checks | Coverage |
| --- | ---: | --- |
| OxygenTransfer | 230 | Production ledger/prediction, bounds, revisions, delayed acknowledgements and 100,000 randomized competing transfers |
| ChargerLifecycle | 168 | Charger lifecycle and refill behavior |
| ConfigSync | 372 | Production configuration codec, overrides and transport with engine doubles |
| BepInExConfig | 12 | Real BepInEx configuration APIs |
| Total | 782 | All passed |

These suites do not execute the full game engine or actual NGO transport.

## Native multiplayer checks

| Phase | Checks | Result |
| --- | ---: | --- |
| Basic interaction | 18 | Held Interact, pump playback, synchronized HUD, approximately 5%/s, rear/aim/LOS rules, recipient turn and range stop |
| Tank/state edges | 6 | Empty/full limits, reverse donation, terminal stop and missing-heartbeat expiry |
| Network impairment/input/animation | 14 | Real UDP impairment, complete outage/recovery, new obstacle, rebinding, crouching and donor death |
| Reconnect | 6 | Old identity/tank/gauge removal, preserved host amount, new identity, snapshots and donation after rejoin |
| More Suits 1.5.4 | 4 | Loaded on both peers, actual Kirby/Skeleton material changes and preserved visible gauges |
| Three peers/competing donors | 9 | Simultaneous donors, capacity competition, all gauges on all peers and late-client-to-earlier-client transfer |
| Concurrent OxyCharger | 5 | Finite reserve consumed while donating, player-plus-charger conservation and agreement on all peers |
| Concurrent OxyBoost | 5 | Real item emission and fuel use while donating; total growth equals measured item output |
| Gamepad/chat/menu | 5 | Virtual Gamepad West hold/release; opening chat/menu stops donation |
| Repeated-transfer soak | 38 | 36 alternating cycles plus initial/final checks, lasting 181.51 seconds |
| Recipient death | 3 | Two active donors stop and clear the dead recipient |
| Total | 113 | All final checks passed |

The proxy imposed 100 ms delay each way, up to ±25 ms jitter each way and 5%
packet loss, followed by a separate 100% loss interval. Transfers progressed with
conserved oxygen under impairment. The server expired stale intent during the
outage; recovery did not duplicate oxygen or resume a released transfer. The
proxy recorded 9,323 packets received and 1,465 dropped on its first route across
the entire run, including the deliberate complete outage. A fresh second route
was used for reconnect.

The soak alternated two remote donors to the host, remote-to-remote donation,
and host-to-remote donation. Starting amounts and positions were staged anew
each cycle. Each cycle checked conservation and bounds during that transfer,
recipient gain, release, agreement across three peers, exactly three gauges on
each peer, and no continuing pump playback after release. All 36 cycles passed.

Endpoint private-memory changes were -0.04 MiB on the host, -0.95 MiB on client
and +6.86 MiB on client2; handle counts decreased by 2, 25 and 11 respectively.
These short endpoint samples do not prove the absence of leaks.

## Fixture issues retained in the evidence

Two initial assertions failed because of the new test scripts, and were corrected
without changing Oxygen.dll:

1. The first reconnect donation attempt did not clear the newly joined client's
   interface flags. Normalizing terminal/chat/menu state, as the existing native
   fixture already does, allowed the same real Interact donation to pass. The
   identity, cleanup and preserved-host checks had already passed.
2. The first soak read a diagnostic JSON file while the helper was rewriting it,
   received null and reported null tanks. The live server/clients still held
   matching conserved amounts. Bounded read retries fixed sampling; the complete
   36-cycle test was then rerun successfully.

Original failures remain in `assertions-rejoin-initial.json` and
`assertions-soak-initial.json`; they are not counted as passing assertions. One
diagnostic command-file sharing violation was also logged by the helper; its
next frame retried the command and the completed soak passed. No Error/Fatal or
exception entry was found in the tested peers' BepInEx logs during the scan.

## Evidence and cleanup

Machine-readable final counts, assertion names, file hashes, process/DLL
manifests, proxy counters and resource samples are indexed in
[RELIABILITY-2026-10-24.json](RELIABILITY-2026-10-24.json).

Raw evidence remains outside Git in these workspace directories:

- `.multiplayer-test/reliability-network-20261024/`: 48 checks, More Suits,
  transport impairment, disconnect/reconnect, both attempts and per-peer logs.
- `.multiplayer-test/reliability-concurrent-20261024/`: 65 checks, three peers,
  charger/OxyBoost/input/death phases, soak attempts and resource snapshots.

Both runs stopped their recorded game processes, the local proxy exited, and
all three original save files were restored and SHA-256 verified after each run.
Nothing was pushed, released or installed in the regular user profile.

## Limits

The native fixture uses real InputSystem events, game RPCs, Unity physics and
production NGO messages, but stages positions on a controlled test floor in
orbit. This is not a manual mission playtest. The earlier screenshot session
separately exercised the feature on Experimentation and inside its facility.

Not covered here: multi-hour sessions, public internet/NAT/relay behavior, more
than three simultaneous players, physical gamepad hardware, VR input, arbitrary
replacement rigs/modpacks, and a dedicated nearby-pickup-conflict scenario.
More Suits compatibility here is specifically version 1.5.4. Existing oxygen
consumption/refills remain owner-reported; these tests are not an anticheat audit.
