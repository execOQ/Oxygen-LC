# Independent transfer binding — issue #41

Oxygen 1.8.1 exposes **Transfer oxygen** through its existing LethalCompanyInputUtils
control settings. Defaults remain keyboard E and gamepad West. The action is
independent of vanilla Interact. Desktop transfer starts on a fresh press of this
action, continues while held, and stops on release or invalid player/UI state.
The old `Interact_performed` Harmony prefix has been removed. Native LCVR retains
the primary-hand Grip action; this change does not add a separate VR binding UI.

Source: <https://github.com/execOQ/Oxygen-LC/issues/41>.

## Native LAN validation

The final Release DLL SHA-256 is
`E5A2404BAC3F9CE6430AAE8D71433072AD7D47CAA5EAC2B8307B103CCD5186CE`.
The excluded LiveProbe fixture uses real Unity keyboard/gamepad input events and
normal production input polling and NGO messaging on two isolated game processes.
No transfer input/eligibility/network methods are replaced.

- `Assert-TransferBindings.ps1`: 13 assertions passed. On both host and client,
  binding transfer to R leaves Interact on E; E no longer transfers; R hold and
  release work; changing Interact to F leaves transfer on R; rebinding transfer
  while R is held cancels the active session. Menu, chat and terminal block the
  custom transfer key.
- `Assert-DeviceInput.ps1`: 22 assertions passed. Both peers transfer using the
  default gamepad button; release and controller removal stop transfer; tank
  totals are conserved; UI guards work; rebinding transfer to South disables the
  old West button; keyboard input still works after switching from gamepad.
- The 230 portable transfer/ledger checks passed, including 100,000 competing
  transfers. These do not execute Unity input and are reported separately.

Evidence: parent workspace
`.multiplayer-test/issue41-transfer-bindings-20261029/`, including
`assertions-transfer-bindings.json`, `assertions-gamepad.json`, per-peer snapshots
and event logs. The fixture's `rebind`/`padbind` commands now change the dedicated
transfer action; `vanillabind` changes vanilla Interact independently.

PushCompanyFork itself was not installed for this test. Its shared-key conflict
can be avoided by selecting a different transfer key. Physical LCVR testing and
the separate post-firing prompt report quoted from issue #40 are not claimed as
fixed or validated by this input-binding change.
