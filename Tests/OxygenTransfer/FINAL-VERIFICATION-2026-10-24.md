# Final DLL verification — 2026-10-24

**782 automated checks and 198 native assertions passed.** The DLL identified below
completed the matrix documented here. This is a historical build snapshot; subsequent
input fixes and their checks are in [the input emulation report](INPUT-EMULATION-2026-10-25.md).
The existing publication archive remains
outdated; this report does not declare that package ready for upload.

Tested Oxygen.dll SHA-256 on every peer:

`DF3F39CBCB7FF5915A8CAB887E018842D387C391B88765C7B6904776455CC6E8`

## Coverage on this DLL

Automated suites: transfer 230 (including 100,000 competing transactions), charger
lifecycle 168, configuration sync 372 and real BepInEx configuration APIs 12.
Release and excluded LiveProbe builds completed without warnings or errors.
Assembly inspection confirms both asset bundles are embedded, LiveProbe is absent
from Oxygen.dll, and BackpackGauge has no per-frame pose-update method.

| Native phase | Assertions | Result |
| --- | ---: | --- |
| basic | 18 | Passed |
| edges | 6 | Passed |
| network | 14 | Passed |
| suits | 4 | Passed |
| rejoin | 6 | Passed |
| parallel | 9 | Passed |
| refill | 5 | Passed |
| inputs | 5 | Passed |
| soak | 122 | Passed |
| boost | 6 | Passed |
| death | 3 | Passed |
| Total | 198 | Passed |

Network coverage uses an actual loopback UDP proxy: approximately 200 ms RTT,
up to 25 ms jitter each way, 5% loss, then a complete outage and recovery. Rejoin
checks cover removal of the old tank/gauge, a fresh identity, preserved host
oxygen, synchronized snapshots and another successful transfer. More Suits 1.5.4
uses actual Kirby and Skeleton materials on both peers.

Three-player checks cover simultaneous donors, capacity competition, late-client
to earlier-client donation, real OxyCharger and OxyBoost use while donating,
virtual gamepad input, chat/menu blocking and recipient death. The final item
follow-up additionally verifies that native pickup replicates OxyBoost's holder
and previous holder to every peer before use and death.

End-of-frame attachment checks on host and client recorded
744 gauge samples with zero position and
angle error relative to the bone during staged movement, turns and crouching.

## Repeated-transfer run

All 120 cycles passed over 602.56 seconds. Each cycle stages
starting amounts and poses anew, then checks recipient gain, conservation,
bounds, release, agreement across three peers, gauge counts and stopped pumps.
It is not an uninterrupted hold or a manual mission playtest.

Endpoint resource changes after the warmed-up functional phases:

- host: private memory +10.02 MiB; handles -8.
- client: private memory +3.84 MiB; handles +19.
- client2: private memory +3.44 MiB; handles +14.

These endpoint measurements do not establish the absence of memory leaks.

## Retained fixture issues

Initial attempts are retained beside the final raw results. A client closed while
applying saved fullscreen settings; the fixture now forces windowed settings in
memory. Fixed-time waits were replaced with actual replicated crouch, targeting
and position readiness checks. The initial soak timed out starting cycle 10;
the final run waits for valid targeting on both donor and server before input.
The production DLL was unchanged during these fixture corrections.

The initial item fixture populated inventory only on the host, leaving the
remote item holders unset. At the later death/drop RPC, the base game's
TetraChemicalItem.DiscardItem raised NullReferenceException on two clients. The
final boost/death phases were rerun in a fresh three-peer session using the
native pickup RPC and explicit all-peer holder checks. Those final phases
replace the earlier item/death assertions in the count above. The original
exceptions remain in the old logs; the final follow-up has no bare native
exceptions or Error/Fatal entries on any of its three peers. Expected command-file sharing races were
retried by the helper; the full soak completed all cycles.

The external UDP helper raised Windows socket error 10054 during shutdown after
the successful network checks. This is recorded separately from production mod
errors. A source-run startup was interrupted and restarted before three-peer
testing; it is not counted as a passing scenario.

## Publication status and limits

The existing `Oxygen-1.7.3.zip` passes its CRC check but contains an older DLL.
The project/plugin/manifest still use version 1.7.3, README calls the feature a
development build, and changelog entries remain Unreleased. A new release version
and an archive containing the tested implementation must be prepared before
publication. Nothing was pushed or published by this verification.

Not covered: physical gamepads, VR interaction, public internet/NAT/relay,
arbitrary replacement rigs/modpacks, a dedicated nearby-pickup-conflict scenario,
all animation clips or multi-hour play. The existing stills retain their original
capture hash; they show unchanged hardware appearance rather than animation timing.

All native runs were cleaned up. The last cleanup restored and SHA-256 verified
all three original saves. Raw profiles/logs remain in workspace
`.multiplayer-test/release-network-final-20261024` and
`.multiplayer-test/release-three-peer-final-20261024`, plus the final item
follow-up in `.multiplayer-test/release-item-followup-20261024`.

Machine-readable counts, assertion names, hashes, proxy counters and resource
evidence: [FINAL-VERIFICATION-2026-10-24.json](FINAL-VERIFICATION-2026-10-24.json).
