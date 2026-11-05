# Technical decisions and version evidence

Evidence checked on 5 November 2026. Current deliverable: M0 and first-area M1 foundation. There is no Unity Editor, Android build toolchain, or Quest device in this environment. Unity package resolution, compilation, rendering and headset compatibility remain **Unverified**.

## Candidate Unity baseline

Use Unity `6000.0.66f2` (revision `b20bc5da3050`) as the candidate Unity 6 LTS baseline. This exact editor version is pinned in Meta's official Shared Spatial Anchors sample, whose README recommends installing it. This is stronger evidence than choosing a version from memory, but is not an independent test of the Chelsea project.

Inspected official Meta sample commit: `71b653caac743b29942a2b4f942320b7b8847972`.

- Editor file: https://github.com/oculus-samples/Unity-SharedSpatialAnchors/blob/71b653caac743b29942a2b4f942320b7b8847972/ProjectSettings/ProjectVersion.txt
- Package manifest: https://github.com/oculus-samples/Unity-SharedSpatialAnchors/blob/71b653caac743b29942a2b4f942320b7b8847972/Packages/manifest.json
- Sample setup: https://github.com/oculus-samples/Unity-SharedSpatialAnchors/blob/71b653caac743b29942a2b4f942320b7b8847972/README.md

Observed versions in that manifest:

| Component | Version | Chelsea status |
| --- | --- | --- |
| Unity Editor | 6000.0.66f2 | Candidate editor pin; not installed |
| OpenXR `com.unity.xr.openxr` | 1.16.1 | Candidate package pin; not resolved here |
| XR Management | 4.5.4 | Candidate package pin; not resolved here |
| Meta XR Core | 207.0.0 | Integration candidate; not required by core/import tools |
| Meta XR Interaction / Interaction OVR | 207.0.0 | Integration candidate; no SDK calls implemented |

Do not copy the sample's Photon, platform, shared-anchor or account workflow. Chelsea is offline and single-player. The sample's declaration of Quest 3/3S support does not verify this project's headset performance or controller/hand parity.

URP `17.0.4` is observed with OpenXR `1.16.1` in Unity's official AR Foundation sample, branch `6.0`, commit `5cf26c7360a872e8db7391452160e093739c640f`:

https://github.com/Unity-Technologies/arfoundation-samples/blob/5cf26c7360a872e8db7391452160e093739c640f/Packages/manifest.json

This second sample uses editor `6000.0.70f1`, not the Meta sample's editor. Consequently URP 17.0.4 with the candidate editor/Meta combination remains a package-resolution and render-validation gate. Do not claim an exact combined supported matrix until tested. Unity's current XR Interaction Toolkit example uses editor 6000.3.0f1, while AR Foundation main uses beta 6000.5.0b2; neither moving branch was selected as the baseline.

Direct `docs.unity3d.com` requests were denied by the proxy. Official GitHub reference files above returned HTTP 200, including exact-commit rechecks. Main/branch URLs were used for discovery only; the evidence links are frozen commits.

## Architecture

Geographic truth lives in testable C# without Unity/Meta references. Keep source coordinates and IDs; presentation uses local metric coordinates and separate chunking. Unity presents generated data through a small import adapter. Do not generate invented replacement roads when source acquisition fails. Unknown building heights, access rights and landmark massing remain explicit gaps.

Target rendering configuration from the brief: URP Forward, Vulkan, IL2CPP, ARM64, OpenXR, single-pass stereo; one shadow light, simple water and distance LOD. These are targets, not a claim that player settings or graphics behavior have been verified. All device budgets remain open until on-device evidence exists.

SDK integration must stay behind interfaces for input, placement, passthrough, anchors and audio. No speculative SDK method signatures are called at this milestone. Stage integration against installed and resolved packages, then test both controllers and hands before reporting parity.

## Evidence checksums

Exact-commit reference bytes fetched with TLS verification enabled:

| Reference | SHA-256 |
| --- | --- |
| Meta ProjectVersion.txt | 95ee123e6b804592781c4a0c86df4df626330548729346dccafcdaa21f7720c1 |
| Meta manifest.json | 75bc027e6778bc592c3ee5428803af68545a45094f4b81433706af382d8874e3 |
| Unity AR Foundation 6.0 manifest.json | 2d4adbc2bdf720b78b9b70aacc82c32b97aa46050ce58684e20d3b4658a2e23c |
| OSM website English locale | 6dcd36d588868e499e8d649402cda3f193a24a0f5e8543a6193391a3beaf9792 |

## Editor pipeline creation API evidence

The official Unity Graphics repository was inspected at commit `9808614cd04ae97bda6a5aed91c54b841e585133` (6000.0/staging), including an HTTP 200 exact-commit recheck:

https://github.com/Unity-Technologies/Graphics/blob/9808614cd04ae97bda6a5aed91c54b841e585133/Packages/com.unity.render-pipelines.universal/Runtime/Data/UniversalRenderPipelineAsset.cs

It exposes `UniversalRenderPipelineAsset.Create(ScriptableRendererData rendererData = null)` under `UNITY_EDITOR`. The built-in creation menu is `Assets/Create/Rendering/URP Asset (with Universal Renderer)`. This supports an Editor-only scaffold using the public API; it does not establish successful asset creation, shader inclusion, or rendering in the uninstalled editor. Preserve the separate renderer asset as well as the pipeline asset when generating project configuration.

## C# toolchain

.NET SDK `8.0.427` is pinned in `global.json`. Core targets `netstandard2.1` for a
Unity-compatible API surface; CLI and executable tests target `net8.0`. The SDK
was acquired from Microsoft's official builds host and its archive SHA-512 was
checked against `https://builds.dotnet.microsoft.com/dotnet/release-metadata/8.0/releases.json`
before use. `tools/build/bootstrap-dotnet.sh` pins that published checksum. No
external NuGet packages are needed; NuGet.Config clears network package sources.
The installer's channel discovery attempted blocked ci.dot.net; installing the
explicit version from builds.dotnet.microsoft.com resolved that prerequisite.
