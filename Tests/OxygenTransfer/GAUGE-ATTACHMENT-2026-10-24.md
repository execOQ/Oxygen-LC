# Fixed gauge attachment verification

The gauge was parented to the spine but also recalculated its world pose in
LateUpdate. Later bone updates could therefore change its effective local
attachment and make it appear to move separately from the tank.

The bind-pose calibration now runs once at creation. Position and rotation stay
fixed in the bone's local space; the hierarchy applies subsequent animation and
IK directly. There is no per-frame positioning or interpolation in BackpackGauge.
The player-root fallback also uses a fixed local attachment.

Release DLL SHA-256, identical on host and client:

`DF3F39CBCB7FF5915A8CAB887E018842D387C391B88765C7B6904776455CC6E8`

Both production and LiveProbe builds passed with zero warnings/errors.
`Assert-Mount.ps1` drove staged movement, turns and real crouch transitions on
both players. The probe sampled both gauges with WaitForEndOfFrame, after
animation, LateUpdate and rendering, comparing their world poses against the
initial local attachment transformed by the current bone pose.

| Peer | Rendered frames | Gauge samples | Maximum position error | Maximum angle error |
| --- | ---: | ---: | ---: | ---: |
| Host | 564 | 1128 | 0 m | 0 degrees |
| Client | 492 | 984 | 0 m | 0 degrees |

The anchor rotated through 180 degrees. Initial remote teleport interpolation
also contributes to recorded anchor travel; this is staged coverage, not a
manual walking playtest. These measurements verify the spine attachment, not
every vertex deformation or arbitrary replacement rig.

All 18 native basic and 6 native edge transfer checks passed on this exact DLL.
Raw results remain in workspace `.multiplayer-test/gauge-rigid-mount-20261024`:
`host/mount-check.json`, `client/mount-check.json`, `assertions-basic.json` and
`assertions-edges.json`. Cleanup restored and verified three original saves.

The existing GitHub stills retain their original capture DLL hash: hardware
appearance is unchanged, and still images cannot establish animation timing.
The earlier visual revision is documented in
[GAUGE-FIT-2026-10-24.md](GAUGE-FIT-2026-10-24.md).
