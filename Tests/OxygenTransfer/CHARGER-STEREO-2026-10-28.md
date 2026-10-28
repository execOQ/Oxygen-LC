# OxyCharger stereo shader repair

DLL SHA-256: `67BE54A552AFF5A71C0018295AE8D8FE1C1F4AFC369EF1FA0F5B2F61CC483F05`.

The bundled Circle.002 mesh is volumetric (675 vertices, 610 triangles).
Its HDRP/Lit shader contains 228 serialized program entries, none using
STEREO_INSTANCING_ON, UNITY_SINGLE_PASS_STEREO or STEREO_MULTIVIEW_ON.
This is evidence of missing stereo variants, rather than flattened geometry.

ChargerVisuals now clones each compatible charger material on spawn and replaces
its shader with the shader object from the game's stock ship canisters. Direct
reference avoids Shader.Find selecting the bundled shader with the same name.
Material properties stay intact; cloned materials are destroyed with the charger.
The stock renderer is found even after its child object is hidden. No LCVR
dependency or mesh/network/interaction change is introduced.

Verification:

- Production and diagnostic helper builds: zero warnings/errors.
- 168 charger lifecycle checks passed.
- A real desktop game confirmed shader identity matches the stock game shader
  and differs from the bundled shader. Base texture, colour, smoothness and mesh
  references are preserved. World bounds remain volumetric.
- The first diagnostic command ran before the ship existed and produced a
  helper-only null-reference error. Repeating after registration produced the
  successful report; no shader-source warning occurred in production.
- Test game stopped; all three original saves restored with matching hashes.
- Updated DLL copied to both existing physical-VR test profiles.

SteamVR was closed during this check. Visual confirmation was pending at that stage; the native follow-up below now records user confirmation. Previous transfer assertions belong to
the earlier DLL and were not rerun or relabelled for this visual change.

Raw evidence: workspace `.multiplayer-test/charger-stereo-20261028/host/charger-visual-checks.json`,
its Player.log and save records; `.multiplayer-test/oxycharger-vr-asset-inspection.json`.

## Native VR follow-up

Run charger-stereo-vr-20261028 launches two peers with the recorded new DLL.
The physical-headset client reports native InVR, VRSession, running XRDisplay
and a stereo camera. On BOTH peers, chargercheck confirms the stock game shader
is used and the bundled one replaced, with original texture, colour, smoothness
and mesh references retained. The client report has stereo=true. Machine evidence
is copied to CHARGER-STEREO-NATIVE-2026-10-28.json. Human depth assessment is
requested separately and is not inferred from these property checks.

An early automatic join touched an uninitialized menu and failed in the fixture;
the later connection succeeded. Test games remain open for manual viewing; the
original save backup is retained for cleanup after the interactive session.

## User-confirmed result

In the physical-headset follow-up, the user explicitly confirmed:
«Да, теперь объёмный». Corrected OxyCharger depth is therefore confirmed
visually by the user in addition to the native shader/property checks.
