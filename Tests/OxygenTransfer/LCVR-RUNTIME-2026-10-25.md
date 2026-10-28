# LCVR runtime and trigger verification — 2026-10-25

**Real LCVR/OpenXR initialization succeeded, but in-game VR verification was
blocked by an LCVR initialization exception reproduced without Oxygen.**
Separately, 16 grip/trigger assertions passed on two real LAN peers using the
unmodified LCVR 1.5.0 action asset with software controller events.
The [machine-readable evidence](LCVR-RUNTIME-2026-10-25.json) records the native
initialization state, blocked gameplay result, every button assertion and cleanup.

Oxygen.dll SHA-256 remained unchanged throughout:

`47F154C938588768DE88400C15D0CBC8FD7A2A823EC28F908809206999680E11`

## Real runtime attempt

Downloaded and installed the official
[LCVR 1.5.0 release](https://github.com/DaXcess/LCVR/releases/tag/v1.5.0) into isolated
test profiles. SteamVR 2.18.4 ran its null HMD driver with a 640 × 640 render target
and separate configuration/log directories under the Oxygen workspace. The
original OpenXR ActiveRuntime registry setting and normal game/mod profiles were
not changed. SteamVR paths were overridden only in the test process environment.

LCVR verified the installed game's hash remotely and reported:

```text
OpenXR runtime being used: SteamVR/OpenXR (2.18.4)
```

The excluded probe observed LCVR's own `InVR == true` and a running native
XRDisplaySubsystem. OpenXR created eye-texture resources. Oxygen's VR activation
getter was not patched in these native-runtime profiles.

The startup scene then failed:

```text
NullReferenceException: Object reference not set to an instance of an object
at LCVR.Managers.InitSceneManager.Awake ()
Source/Managers/InitSceneManager.cs:26
```

Attempting the normal LAN confirmation also produced an exception in
InitSceneManager.FadeScene. No gameplay VRSession was created, so in-game stereo
rendering, actual VR input dispatch and Oxygen's VR HUD placement could not be
validated end to end.

Removing FastStartup, DevelopmentStartup and the obsolete TypeLoadExceptionFixer
did not resolve the startup exception. A separate clean control profile loaded
only LCVR and its required preloader support: its log confirms **1 plugin to
load**, LCVR 1.5.0. Oxygen, InputUtils, LethalLib and LiveProbe were disabled there.
The same InitSceneManager.Awake exception reproduced. This establishes that the
startup failure in this installation/null-HMD setup occurs without Oxygen; it
does not establish whether a physical headset would reproduce it.

## Grip versus trigger

After stopping SteamVR, a separate ordinary two-peer LAN fixture loaded the
LCVR 1.5.0 VRActions asset. It used the existing simulated headset activation and
software XRController events described in the
[input emulation report](INPUT-EMULATION-2026-10-25.md). LCVR/OpenXR was not running
during these button assertions. Original binding paths were preserved.

| Control | Actual action | Oxygen transfer |
| --- | --- | --- |
| Right gripButton | Interact | Starts while aimed at the teammate's back |
| Right triggerButton | Use / ActivateItem | Does not start |
| Right analog trigger | Use / ActivateItem | Does not start |

Eight assertions passed for each donor role (host and remote client), 16 total:
the original right-grip Interact binding, digital/analog trigger action activity
without Interact, no oxygen change for either trigger, grip independent of item
activation, donor HUD decrease, stopping on grip release, and conservation.
The tested input bundle SHA-256 is:

`CFFA56DF6EFD76C4625F681C756AF441B262DBBE38A483025A2840EA1215150B`

This confirms the current control contract. It does not replace the blocked
end-to-end gameplay VR check and does not certify physical controller drivers.

## Retained evidence and cleanup

Workspace `.multiplayer-test` directories:

- `lcvr-runtime-20261025`: official package, isolated SteamVR settings/logs,
  recorded owned processes, original configuration hashes and cleanup records.
- `lcvr-native-20261025`: first real-runtime attempt with startup helper mods.
- `lcvr-native-clean-20261025`: real-runtime attempt without startup shortcuts;
  snapshots record native InVR and XRDisplaySubsystem state.
- `lcvr-baseline-no-oxygen-20261025`: clean LCVR-only control and the same exception.
- `lcvr-trigger-contract-20261025`: all 16 button assertions, exported profiles,
  peer DLL hashes, snapshots and logs. Run `LiveProbe/Assert-VrTrigger.ps1` to repeat.

All test game and SteamVR processes were stopped. All three original saves were
restored with matching hashes. The user's original SteamVR settings and OpenVR
path registry files match their pre-test SHA-256 hashes. Official LCVR files remain
installed in the isolated test profiles; no release was published.
