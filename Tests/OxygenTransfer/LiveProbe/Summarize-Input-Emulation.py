"""Rebuild the report from retained native assertion and cleanup evidence."""
import hashlib
import json
import pathlib
import re

repo = pathlib.Path(__file__).resolve().parents[3]
run = repo.parent / '.multiplayer-test/input-emulation-fixed-20261025'
expected = '47F154C938588768DE88400C15D0CBC8FD7A2A823EC28F908809206999680E11'
dll = repo / 'bin/Release/netstandard2.1/Oxygen.dll'
assert hashlib.sha256(dll.read_bytes()).hexdigest().upper() == expected
groups = {}
for name, count in [('gamepad', 22), ('vr-input', 34), ('basic', 18), ('edges', 6)]:
    checks = json.loads((run / f'assertions-{name}.json').read_text(encoding='utf-8-sig'))
    assert len(checks) == count and all(c['pass'] for c in checks), name
    groups[name] = checks

error_pattern = re.compile(r'^\[(?:Error|Fatal)|^\w*(?:Exception|Error):|^NullReferenceException|^MissingMethodException|^TypeLoadException', re.M)
logs = {}
for role in ['host', 'client']:
    record = json.loads((run / f'{role}-process.json').read_text(encoding='utf-8-sig'))
    assert record['dllSha256'] == expected
    log = (run / role / 'Player.log').read_text(encoding='utf-8-sig', errors='replace')
    events = (run / role / 'transfer-events.log').read_text(encoding='utf-8-sig')
    assert not error_pattern.search(log) and ' ERROR ' not in events, role
    logs[role] = {'playerErrors': 0, 'probeErrors': 0}

originals = json.loads((run / 'save-before.json').read_text(encoding='utf-8-sig'))
save_root = pathlib.Path('C:/Users/execo/AppData/LocalLow/ZeekerssRBLX/Lethal Company')
for original in originals:
    assert hashlib.sha256((save_root / original['path']).read_bytes()).hexdigest().upper() == original['sha256']
cleanup = json.loads((run / 'input-cleanup.json').read_text(encoding='utf-8-sig'))
assert cleanup['runningGames'] == 0 and cleanup['restoredSaves'] == len(originals) == 3

result = {
    'date': '2026-10-25', 'dllSha256': expected, 'nativeAssertions': 80,
    'automatedChecks': {'transfer': 230, 'chargerLifecycle': 168, 'configSync': 372, 'bepInExConfig': 12},
    'groups': groups, 'logs': logs, 'cleanup': cleanup,
    'vrSimulation': {
        'profile': 'Unmodified VRActions asset from cached LCVR 1.4.6 bundle',
        'bundleSha256': hashlib.sha256((run / 'host/lethalcompanyvr').read_bytes()).hexdigest().upper(),
        'device': 'Software XRController subclass; original <XRController>{RightHand}/gripButton binding',
        'activation': 'Only Oxygen VrCompatibility.Active getter overridden by excluded LiveProbe',
        'poses': 'Staged player/gameplay-camera transforms; no XR tracking pipeline',
        'realLcvRPluginRunning': False, 'openXrRuntimeRunning': False,
        'notCovered': ['physical device drivers', 'stereo rendering', 'tracking', 'VR HUD placement', 'LCVR hand interactions'],
    },
    'priorMatrix': {'report': 'FINAL-VERIFICATION-2026-10-24.md', 'nativeAssertions': 198,
                    'dllSha256': 'DF3F39CBCB7FF5915A8CAB887E018842D387C391B88765C7B6904776455CC6E8',
                    'rerunOnCurrentDll': False},
}
output = repo / 'Tests/OxygenTransfer/INPUT-EMULATION-2026-10-25.json'
output.write_text(json.dumps(result, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
print(f'Validated 80 native assertions, two peer DLL hashes, clean logs and three restored saves. {output}')
