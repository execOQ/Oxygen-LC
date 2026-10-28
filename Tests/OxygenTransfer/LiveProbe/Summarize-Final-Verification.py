"""Index final-DLL verification without copying large native logs into Git."""
from pathlib import Path
import hashlib
import json
import zipfile

repo = Path(__file__).resolve().parents[3]
workspace = repo.parent
load = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest().upper()
expected = 'DF3F39CBCB7FF5915A8CAB887E018842D387C391B88765C7B6904776455CC6E8'
assert sha(repo/'bin/Release/netstandard2.1/Oxygen.dll') == expected
groups = {
    'release-network-final-20261024': ['basic', 'edges', 'network', 'suits', 'rejoin'],
    'release-three-peer-final-20261024': ['parallel', 'refill', 'inputs', 'soak'],
    'release-item-followup-20261024': ['boost', 'death'],
}
phases, processes, initial = [], [], []
for name, required in groups.items():
    root = workspace/'.multiplayer-test'/name
    for phase in required:
        path = root/f'assertions-{phase}.json'
        checks = load(path)
        if isinstance(checks, dict): checks = [checks]
        assert checks and all(x['pass'] for x in checks), path
        phases.append({'run': name, 'phase': phase, 'checks': len(checks),
                       'sha256': sha(path), 'assertions': [x['name'] for x in checks]})
    for path in root.glob('*-process.json'):
        record = load(path)
        assert record['dllSha256'] == expected, path
        processes.append({'run': name, 'role': path.stem.removesuffix('-process'), **record})
    for path in root.glob('assertions-*-initial.json'):
        checks = load(path)
        initial.append({'run': name, 'file': path.name, 'sha256': sha(path),
                        'failedAssertions': [x['name'] for x in checks if not x['pass']]})

network = workspace/'.multiplayer-test/release-network-final-20261024'
three = workspace/'.multiplayer-test/release-three-peer-final-20261024'
mount = {role: load(network/role/'mount-check.json') for role in ['host', 'client']}
assert all(x['pass'] for x in mount.values())
soak = load(three/'assertions-soak.json')[-1]['evidence']
assert soak['cycles'] == 120
before = {x['role']: x for x in load(three/'resources-before-soak.json')}
after = {x['role']: x for x in load(three/'resources-after-soak.json')}
resources = {role: {'privateMemoryDeltaMiB': (after[role]['privateBytes']-before[role]['privateBytes'])/1048576,
                    'handleDelta': after[role]['handles']-before[role]['handles'],
                    'before': before[role], 'after': after[role]} for role in before}
archive = workspace/'Oxygen-1.7.3.zip'
with zipfile.ZipFile(archive) as package:
    package_manifest = json.loads(package.read('manifest.json').decode('utf-8-sig'))
    package_dll_hash = hashlib.sha256(package.read('BepInEx/plugins/Oxygen.dll')).hexdigest().upper()
    package_audit = {'file': archive.name, 'crcValid': package.testzip() is None,
                     'manifestVersion': package_manifest['version_number'],
                     'dllSha256': package_dll_hash, 'containsTestedDll': package_dll_hash == expected}
assembly = load(three/'assembly-audit.json')
assert not assembly['testHelperIncluded'] and not assembly['gaugeHasPerFramePoseMethod']
item_log_check = load(workspace/'.multiplayer-test/release-item-followup-20261024/item-log-check.json')
assert item_log_check['pass']
checks = sum(x['checks'] for x in phases)
assert checks == 198, checks
save_root = Path('C:/Users/execo/AppData/LocalLow/ZeekerssRBLX/Lethal Company')
restored = load(three/'save-before.json')
assert len(restored) == 3 and all(sha(save_root/x['path']) == x['sha256'] for x in restored), 'Restore saves before finalizing'
result = {
    'date': '2026-10-24', 'game': 'Lethal Company v81', 'oxygenDllSha256': expected,
    'automatedChecks': {'transfer': 230, 'charger': 168, 'configSync': 372, 'bepInExConfig': 12, 'total': 782},
    'nativeAssertionChecks': checks, 'nativeFinalFailed': 0, 'phases': phases,
    'mountChecks': mount, 'soak': soak, 'resources': resources, 'processes': processes,
    'restoredOriginalSavesVerified': len(restored),
    'initialAttemptsRetained': initial, 'assemblyAudit': assembly,
    'finalItemLogCheck': item_log_check,
    'proxyCounters': {str(port): load(network/f'proxy-{port}-stats.json') for port in [17778, 17779]},
    'existingPackageAudit': package_audit,
    'releasePackageReady': False,
    'releaseBlockers': ['Existing ZIP contains an older DLL.',
                        'Source/plugin/package version remains 1.7.3; new release version and package are not prepared.',
                        'README still describes a development build; changelog entries remain Unreleased.'],
    'fixtureNotes': ['Initial client startup stopped while applying saved fullscreen settings; later fixture builds force windowed mode.',
                     'Crouch assertion now waits for replicated animation instead of a fixed 900 ms delay.',
                     'Reconnect input waits for targeting and replicated positions instead of a fixed 1500 ms delay.',
                     'Initial soak timed out starting cycle 10; final fixture checks both donor and server targeting before pressing Interact.',
                     'UDP proxy raised Windows socket error 10054 during shutdown after network checks completed.',
                     'Initial item fixture assigned OxyBoost inventory locally, leaving remote holders unset. Base-game drop RPC raised NullReferenceException at recipient death. Final boost/death phases use the native pickup RPC and verify holders on all three peers.',
                     'The first full soak logged 85 transient command-file sharing retries in the helper; every final cycle completed. Later helper builds retry expected read sharing races without logging ERROR.'],
    'limits': ['Staged local LAN testing, not public internet/NAT/relay certification.',
               'Virtual gamepad only; physical controllers and VR input untested.',
               '120 staged cycles with reset starting amounts, not hours-long gameplay or a leak proof.',
               'More Suits 1.5.4 tested; arbitrary replacement rigs/modpacks and nearby pickup conflicts untested.'],
}
out = repo/'Tests/OxygenTransfer/FINAL-VERIFICATION-2026-10-24.json'
out.write_text(json.dumps(result, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print(f'Indexed {checks} passing native assertions, {sum(x["samples"] for x in mount.values())} mount samples; DLL hashes match.')
print('Soak:', soak)
print('Resource deltas:', {r: (v['privateMemoryDeltaMiB'], v['handleDelta']) for r,v in resources.items()})
print('Existing ZIP contains current DLL:', package_audit['containsTestedDll'])
rows = '\n'.join(f'| {p["phase"]} | {p["checks"]} | Passed |' for p in phases)
memory = '\n'.join(f'- {role}: private memory {v["privateMemoryDeltaMiB"]:+.2f} MiB; handles {v["handleDelta"]:+d}.' for role,v in resources.items())
report = f'''# Final DLL verification — 2026-10-24

**782 automated checks and {checks} native assertions passed.** The current DLL
completed the full available matrix. The existing publication archive remains
outdated; this report does not declare that package ready for upload.

Tested Oxygen.dll SHA-256 on every peer:

`{expected}`

## Coverage on this DLL

Automated suites: transfer 230 (including 100,000 competing transactions), charger
lifecycle 168, configuration sync 372 and real BepInEx configuration APIs 12.
Release and excluded LiveProbe builds completed without warnings or errors.
Assembly inspection confirms both asset bundles are embedded, LiveProbe is absent
from Oxygen.dll, and BackpackGauge has no per-frame pose-update method.

| Native phase | Assertions | Result |
| --- | ---: | --- |
{rows}
| Total | {checks} | Passed |

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
{sum(x['samples'] for x in mount.values())} gauge samples with zero position and
angle error relative to the bone during staged movement, turns and crouching.

## Repeated-transfer run

All 120 cycles passed over {soak['elapsedSeconds']:.2f} seconds. Each cycle stages
starting amounts and poses anew, then checks recipient gain, conservation,
bounds, release, agreement across three peers, gauge counts and stopped pumps.
It is not an uninterrupted hold or a manual mission playtest.

Endpoint resource changes after the warmed-up functional phases:

{memory}

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
'''
(repo/'Tests/OxygenTransfer/FINAL-VERIFICATION-2026-10-24.md').write_text(report, encoding='utf-8')
