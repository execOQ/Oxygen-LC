"""Index the 2026-10-24 reliability run without copying large raw logs into Git."""
from pathlib import Path
import hashlib
import json

workspace = Path(__file__).resolve().parents[4]
repo = Path(__file__).resolve().parents[3]
expected = '406B766DE984CCD542A1556638A45CF4221080E58B235AE2570A3E88105ABAE5'
load = lambda path: json.loads(path.read_text(encoding='utf-8-sig'))
sha = lambda path: hashlib.sha256(path.read_bytes()).hexdigest().upper()
assert sha(repo/'bin/Release/netstandard2.1/Oxygen.dll') == expected
phases, initial, manifests, resources = [], [], [], {}
for name in ('reliability-network-20261024', 'reliability-concurrent-20261024'):
    root = workspace/'.multiplayer-test'/name
    for path in sorted(root.glob('assertions-*.json')):
        checks = load(path)
        if isinstance(checks, dict): checks = [checks]
        if 'initial' in path.name:
            initial.append({'file':f'.multiplayer-test/{name}/{path.name}', 'sha256':sha(path),
                            'failedAssertions':[c['name'] for c in checks if not c['pass']]})
            continue
        assert checks and all(c['pass'] for c in checks), path
        phases.append({'run':name,'file':path.name,'sha256':sha(path),'checks':len(checks),
                       'failed':0,'assertions':[c['name'] for c in checks]})
    for path in sorted(root.glob('*-process.json')):
        record = load(path)
        assert record['dllSha256'] == expected, path
        manifests.append({'file':f'.multiplayer-test/{name}/{path.name}',**record})
    for path in root.glob('resources-*.json'): resources[path.stem] = load(path)
proxy = load(workspace/'.multiplayer-test/reliability-network-20261024/proxy-17778-stats.json')
soak = load(workspace/'.multiplayer-test/reliability-concurrent-20261024/assertions-soak.json')[-1]['evidence']
assert sum(p['checks'] for p in phases) == 113
result = {'date':'2026-10-24','game':'Lethal Company v81','oxygenDllSha256':expected,
          'automated':{'OxygenTransfer':230,'ChargerLifecycle':168,'ConfigSync':372,'BepInExConfig':12,'total':782},
          'nativeFinalChecks':113,'nativeFinalFailed':0,'phases':phases,
          'initialFixtureFailuresRetained':initial,'processManifests':manifests,
          'proxyFirstRouteFinalCounters':proxy,'soak':soak,'resources':resources,
          'cleanup':'Both runs stopped recorded games and restored/verified three original saves; proxy exited.',
          'limitations':'Controlled native LAN fixture; 181.51-second repetition test, not multi-hour/manual/internet/VR/modpack certification.'}
out=repo/'Tests/OxygenTransfer/RELIABILITY-2026-10-24.json'
out.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(f'Indexed {len(phases)} final phases, 113 passing native checks, 2 retained initial fixture failures; DLL hash verified.')
