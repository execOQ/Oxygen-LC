# Oxygen transfer validation

The latest full-matrix run on the fixed gauge DLL is recorded in
[FINAL-VERIFICATION-2026-10-24.md](FINAL-VERIFICATION-2026-10-24.md), including
network impairment, reconnect, More Suits, three peers and 120 repeated cycles.

Source task: [Player-to-player oxygen transfer and backpack tank gauge](https://app.notion.com/p/402799f1b2ba81b79fb5df488b409fa3).

Run from the repository directory:

```powershell
dotnet run --project Tests/OxygenTransfer -c Release
dotnet run --project Tests/ChargerLifecycle -c Release
dotnet run --project Tests/ConfigSync -c Release
dotnet run --project Tests/BepInExConfig -c Release
dotnet build Oxygen.csproj --no-restore -c Release
```

The new executable tests compile the production ledger and prediction classes.
They check tank bounds, duplicate registrations/mutations, invalid numbers,
competing donors and recipients, empty/full tanks, delayed acknowledgements,
session resets and conservation over 100,000 randomized transfers. They do not
execute Unity physics, input, rendering or NGO transport.

Implementation notes:

- A server ledger stores normalized tank amounts. Existing owner-side consumption,
  refill, weather and recovery code submits sequenced changes through the Percent
  setter. These existing mechanics remain owner-reported; this is not an anticheat
  rewrite. Transfer amounts and eligibility are calculated on the server.
- HUD prediction retains unacknowledged changes; authoritative snapshots cannot
  undo a donation by replaying an older absolute oxygen report.
- The server updates transfers at up to 10 Hz, requiring renewed intent every
  350 ms. Elapsed time is capped at that lease to avoid a long catch-up burst. Reliable ordered
  messages carry registration, mutations, start/stop intent and snapshots.
- Start checks require a rear cone of 60 degrees either side of directly behind,
  a ray into an analytic back box, distance and an unobstructed ray. The box has
  no physics collider, avoiding interference with movement and cosmetic rigs.
- Ongoing transfers recheck distance, visibility, death and tank capacity, without
  rechecking rear angle or aim. Releasing Interact sends an immediate stop.
- Gauge geometry/materials are generated at runtime. The dial follows spine.003's
  animation through a fixed local attachment calibrated from its bind pose, with
  a player-root fallback. No per-frame world-pose correction is applied.
  No asset bundle rebuild is required.
- Interact uses the same global action asset as the game's input callback, avoiding
  a stale PlayerInput copy after rebinding.
- Player lookup uses spawned players' NGO OwnerClientId, including earlier players
  whose historical actualClientId is not available on a late-joining client.

Native verification is documented in [LIVE-RESULTS.md](LIVE-RESULTS.md). The live
probe compiles separately against the installed game, with no test code included
in Oxygen.dll. It uses real player input, physics, RPCs and NGO custom messages.
The launch/cleanup scripts use copied profiles, back up saves and verify restored
files by SHA-256. Run those scripts with the full process view so duplicate game
instances cannot hide from their guards.

The transfer reliability rerun is recorded in
[RELIABILITY-2026-10-24.md](RELIABILITY-2026-10-24.md), with exact DLL hash,
network conditions, repeated-transfer duration and remaining coverage limits.

Verified in native multiplayer:

- [x] Host and remote donation, approximately 5% per second, conserved amounts and synchronized HUDs.
- [x] Front/side/aim-away and wall rejection, release, range, terminal, death and empty/full stops.
- [x] Recipient turns, newly obstructed paths and crouched recipients.
- [x] Rebound keyboard input, localized binding text and animation-following gauge placement.
- [x] Quiet pump loop starts during confirmed transfer; standing dial visually inspected.
- [x] Actual 200 ms RTT with jitter and 5% packet loss, complete outage expiry and safe recovery.
- [x] Reconnect after death/disconnect, removal of old tanks and preservation of existing host oxygen.

- [x] Two remote donors, three gauges on every peer and late-client-to-earlier-client donation.
- [x] Concurrent native OxyCharger refill and OxyBoost activation while donating.
- [x] Virtual gamepad Interact press/hold/release, chat/menu stops and recipient death.
- [x] More Suits 1.5.4 with Kirby/Skeleton costumes, preserved gauges and basic transfer checks.

Coverage boundaries:

- Physical gamepad/VR hardware and nearby pickup-item conflicts were not tested.
- Arbitrary replacement rigs and cosmetics beyond More Suits remain unverified.
- Broad lighting and subjective audio evaluation still need manual playtesting.

The reliability build passed 113 native checks and all 782 portable checks.
The later gauge material and mounting revision is documented separately in
[GAUGE-FIT-2026-10-24.md](GAUGE-FIT-2026-10-24.md). See each report for its exact DLL hash.
The subsequent animation attachment fix and end-of-frame drift measurements are
recorded in [GAUGE-ATTACHMENT-2026-10-24.md](GAUGE-ATTACHMENT-2026-10-24.md).
No release or installation in the user's regular mod profile is included.
