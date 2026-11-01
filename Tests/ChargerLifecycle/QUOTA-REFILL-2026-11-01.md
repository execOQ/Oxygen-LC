# Quota transition refill recovery — issue #42

Source: <https://github.com/execOQ/Oxygen-LC/issues/42>.

## Reproduced cause

The game's `playersFiredGameOver` coroutine disables all colliders on spawned
ship furniture during ejection. OxyCharger is both `alreadyUnlocked` and
`spawnPrefab`, so `ResetShipFurniture` keeps it instead of despawning it, while
the native collider/placement restoration only handles built-in non-prefab
furniture. Oxygen's earlier reset hook restored the station's position but not
its colliders. Collider enabled state is local Unity state, so both peers need
to recover it.

On the published 1.8.1 DLL, SHA-256
`3C2B352133C8E23F1A0F65BB6C2634EE5F0ACF1DA495E94B3C134C4CB3774BBB`,
two real LAN peers could refill before the transition and after the successful
quota RPC. After the complete native firing/revival sequence, both station
colliders stayed disabled on both peers and neither player could target the
refill trigger. The clean successful-quota scenario did not reproduce the
reporter's additional success-path failure.

## Fix

After the native `SetShipReadyToLand` boundary, the host refreshes station
availability and every peer restores only the station's refill trigger/colliders
and build-mode collider. The existing furniture reset hook also attempts this
repair. Recovery is suppressed during firing/suction, while disabled or stored,
and for unspawned or missing stations. It never respawns a healthy station,
changes its oxygen reserve, enables gauge colliders, or enables unrelated
colliders. The ship-ready boundary is shared by normal quota completion and the
end of firing. Networking and oxygen allocation are unchanged.

## Evidence and validation

- Baseline: 14 native LAN assertions, including the reproduced failure on both
  peers, in parent workspace `.multiplayer-test/issue42-baseline-20261101/`.
- Fixed 1.8.2: 16 native LAN assertions in
  `.multiplayer-test/issue42-fixed-20261101/`. Both peers display the real native
  `Fill oxygen` prompt, select the actual charger trigger, and refill from 20% to
  100% by holding the game's vanilla Interact key after firing. The lobby is not
  recreated. Native ejection still disables the colliders during the cutscene.
- The success check invokes the game's `SetNewProfitQuota`/quota-sync RPC; it
  does not stage scrap sales or run a complete Company Building mission.
- 199 lifecycle checks (19 new recovery regressions), 372 configuration checks
  and 230 oxygen ledger/transfer checks passed: 801 portable checks total.
- Release build and NetcodePatcher passed without warnings or errors.
- Fixed DLL SHA-256:
  `1E31FA24A73F32D58D312132B95BC19B6E1E9FB4E93BEF98A15A7E6BC7D5D2FA`.

Reproduce with the isolated host/client launchers and
`Tests/OxygenTransfer/LiveProbe/Assert-QuotaRefill.ps1 -RunRoot <run>`.
Use `-ExpectBroken` for the published 1.8.1 baseline. The helper stages keyboard
input and camera aim but does not replace the game's hover, firing, revival,
collider-reset or refill paths. Game saves are backed up and restored by the
existing launch/stop fixtures.
