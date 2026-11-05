# Native alpha API evidence

Checked on 5 November 2026. This records source signatures used for the launch
scaffold. It does **not** establish package resolution, Unity compilation, Android
build success, controller behavior or Quest compatibility. No Unity Editor or
headset was available in this cloud machine.

## Official Unity engine / Editor reference source

The following exact-commit URLs were retrieved successfully (HTTP200) from Unity's
official `Unity-Technologies/UnityCsReference` repository. Commit
`a189316266b40ba267ca77dddf88609ea6d769f9` was the observed `6000.0` branch head. It
is reference-source evidence for that branch, not a claim that its native binaries
are identical to editor `6000.0.66f2`.

Base URL:
https://github.com/Unity-Technologies/UnityCsReference/tree/a189316266b40ba267ca77dddf88609ea6d769f9

| Path | Observed API | SHA-256 of retrieved file |
|---|---|---|
| `Editor/Mono/BuildPipeline.bindings.cs` | `BuildPipeline.IsBuildTargetSupported(BuildTargetGroup, BuildTarget)` | `0feda3fa0ebf59edf0375f484588e3f75c1ce8c3aa45f0663a0071882e22bea8` |
| `Editor/Mono/PlayerSettings.bindings.cs` | `Get/SetScriptingBackend(NamedBuildTarget, ...)`; `Get/SetApplicationIdentifier(NamedBuildTarget, ...)` | `b44035521ad9052d31581a2ae18a22a7811307a9ba3047358af419c41e6cfa24` |
| `Editor/Mono/PlayerSettingsAndroid.bindings.cs` | `PlayerSettings.Android.targetArchitectures`, `minSdkVersion`, `targetSdkVersion`, `useCustomKeystore` | `7f24c39c26edd5f976a7588627b058115ceab560f1a15f3d5edb3b57dab65c36` |
| `Modules/XR/ScriptBindings/XRInput.bindings.cs` | `InputDevices.GetDeviceAtXRNode`; `TryGetFeatureValue`; common tracked pose/button/axis usages | `cbe44629f9f704f875934588633029380085b37d2c7edf9de1e56042b0cf64a8` |

The newer `NamedBuildTarget` PlayerSettings overloads are preferred over obsolete
BuildTargetGroup overloads. The source exposes API properties; it does not establish
current Meta Store submission SDK requirements. Store release requirements must be
checked separately before distribution.

## Pinned package-source mirror evidence

The guessed public `Unity-Technologies/OpenXR-Plugin` and `XR-Management`
repositories were unavailable. Direct Unity documentation/package-registry access
is still blocked by the running network policy. OpenXR1.16.1 `package.json` declares
an internal Unity upstream repository (`xr.sdk.openxr.git`), not an accessible
public origin.

Consequently the exact-version package source below was inspected through
**third-party public `needle-mirror` mirrors**. They contain Unity package source,
but mirror retrieval is not an official package download, authenticated release
verification, licence review or SDK installation. Do not report these as an
independently verified official compatibility matrix. Resolve the actual pinned
packages through Unity Package Manager before the first build.

- XR Management4.5.4 mirror commit:
  https://github.com/needle-mirror/com.unity.xr.management/tree/f0d78614e956a71980c74bc99abd720846f41121
- OpenXR1.16.1 mirror commit:
  https://github.com/needle-mirror/com.unity.xr.openxr/tree/2f7b9fe6816f06d6f3669840ec953e46837e8439

Exact tag identities were checked using read-only `git ls-remote`. Source files were
retrieved from those commits with TLS verification enabled. No mirrored package
binaries were installed or shipped.

### XR Management signatures

`Editor/XRGeneralSettingsPerBuildTarget.cs` exposes:

```csharp
XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android)
```

The returned `XRGeneralSettings` exposes `Manager` and `InitManagerOnStart`.
`Manager.activeLoaders` identifies configured loaders. The OpenXR loader full type
name is `UnityEngine.XR.OpenXR.OpenXRLoader`.

`Editor/Metadata/XRPackageMetadataStore.cs` exposes
`AssignLoader(XRManagerSettings, string loaderTypeName, BuildTargetGroup)` returning
bool. The current build preflight validates existing configuration instead of
using undocumented asset-creation internals or changing loader assignments.

Assembly references: `Unity.XR.Management`, `Unity.XR.Management.Editor`.

### OpenXR settings / interaction profile

Observed source paths:

- `Runtime/Settings/OpenXRSettings.cs`: Editor-only
  `OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android)`.
- `Runtime/Settings/OpenXRRenderSettings.cs`: `renderMode` and
  `OpenXRSettings.RenderMode.SinglePassInstanced`.
- `Runtime/Features/OpenXRFeatureSettings.cs`: `GetFeature<T>()`.
- `Runtime/Features/OpenXRFeature.cs`: public `enabled` bool.
- `Runtime/Features/Interactions/OculusTouchControllerProfile.cs`:
  `UnityEngine.XR.OpenXR.Features.Interactions.OculusTouchControllerProfile`;
  feature ID `com.unity.openxr.feature.input.oculustouch`.
- `Runtime/MetaQuest/MetaQuestFeature.cs`:
  `UnityEngine.XR.OpenXR.Features.MetaQuestSupport.MetaQuestFeature`;
  feature ID `com.unity.openxr.feature.metaquest`.

`MetaQuestFeature` is in a separate assembly,
`Unity.XR.OpenXR.Features.MetaQuestSupport`; the Editor assembly needs that explicit
reference when using its type. Settings/profile types otherwise use
`Unity.XR.OpenXR` and Editor code may require `Unity.XR.OpenXR.Editor`.

MetaQuest's `targetDevices` collection and target-device enable methods are
**internal**, not a public configuration API. Quest3's manifest name is `eureka`;
Quest3S uses `quest3s`. The build scaffold instructs the developer to select the
actual devices in Unity's Meta Quest Support UI. It does not pretend to validate
that internal per-device list. Unity's Project Validation must also be run.

Retrieved source SHA-256:

| OpenXR1.16.1 path | SHA-256 |
|---|---|
| `Runtime/MetaQuest/MetaQuestFeature.cs` | `abc300682dbd3f0ba262bc78ba5edfa8a010e5ba94b4b8a139c4b4badb181a62` |
| `Runtime/Settings/OpenXRRenderSettings.cs` | `0a81d78d697e25f649de51d03bb593dd49cdd75eec708454b268b86be60a2e00` |
| `Runtime/Features/OpenXRFeatureSettings.cs` | `e9f2c19e9c015e07d2ea343d0520163f835a6cb822e8736da30f3a5039218891` |
| `Runtime/Features/OpenXRFeature.cs` | `3aec031b892e657312b486d9d471ccb737e85acaac6b8a179ca913ad2b2439e2` |
| `Runtime/MetaQuest/Unity.XR.OpenXR.Features.MetaQuestSupport.asmdef` | `28ef42cced9c6887db00e67d06428e22bc54ddd2d74cf1de2d09dd6a86312455` |

### Input and modules

The pinned OpenXR package includes `Samples~/ControllerXRInput` and its README:

https://github.com/needle-mirror/com.unity.xr.openxr/blob/2f7b9fe6816f06d6f3669840ec953e46837e8439/Samples~/ControllerXRInput/README.md

It demonstrates querying OpenXR devices using Unity's XR Input/Feature API.
`Scripts/ActionToButtonXRInput.cs` explicitly calls
`device.TryGetFeatureValue(usage, out bool)`. `Documentation~/input.md` describes
support for both the new Input System and Feature API. The Input System package is
still required and installed transitively by OpenXR; an enabled interaction profile
is required to supply controller bindings.

For Android, choose Input System (New). A desktop fallback using legacy
`UnityEngine.Input` must be guarded by `ENABLE_LEGACY_INPUT_MANAGER` and must not
make a Quest build depend on enabling both input backends.

Official Meta sample manifest at commit
`71b653caac743b29942a2b4f942320b7b8847972` confirms the module package IDs
`com.unity.modules.physics`, `com.unity.modules.xr` and `com.unity.modules.ui`
(version1.0.0). Engine source module directories named InputLegacy/TextRendering
are not evidence of standalone UPM package IDs. No guessed
`com.unity.modules.inputlegacy` or `com.unity.modules.textrendering` dependencies
should be added.

## Static review limits

`AlphaBuild.cs` loader/settings/profile calls and Editor assembly references were
compared with these sources. No signature defect was found during that comparison.
That is a static source review, not a Unity compilation result. A controller
recovery edge case (controller tracking reconnect with held trigger) was sent to
the input implementation agent for correction. Existing spec gates for hand parity,
MR, safe street mode, actual Chelsea content and on-device evidence remain open.
