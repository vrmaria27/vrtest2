# Unity M1 inspection scaffold

This is source for an inspection project, **not a playable Quest build**. No Unity Editor or headset was available for compilation, scene import, rendering, or device validation. Opening this project does not establish those gates. Synthetic test meshes must never be described as Chelsea.

The editor baseline is Unity `6000.0.66f2`; the package manifest pins URP `17.0.4`, OpenXR `1.16.1`, and XR Management `4.5.4`. See `../docs/TECH_DECISIONS.md` for source evidence and unverified compatibility. Meta packages and adapters are intentionally not installed: XR interfaces exist, but no working MR, input, anchors, audio or locomotion integration is claimed.

## Inspect generated geometry

1. Install the pinned Unity Editor with Android build support if later building for Quest. Open this directory as a Unity project. Allow package restore; do not silently upgrade pinned dependencies.
2. Use `Chelsea > Create inspection URP configuration` if Graphics has no render pipeline assigned. It creates and assigns an inspection pipeline using URP's public Editor factory; an existing pipeline is preserved. Alternatively, create an asset using `Assets > Create > Rendering > URP Asset (with Universal Renderer)` and assign it in `Edit > Project Settings > Graphics > Default Render Pipeline`. Check Quality settings do not override it with a different pipeline. Verify the renderer uses Forward mode. The factory API was checked against authoritative source, but execution remains unverified without the editor.
3. Generate geographic JSON using the CLI documented in the repository README. The JSON must have schema version 1, explicit `datasetKind`, origin, and meshes. With source access blocked, only the clearly named synthetic test fixture is available.
4. Use `Chelsea > Import geographic JSON for inspection…`. The importer validates the document before creating a new scene, asks Unity to save any modified scene, and saves generated assets in a unique `Assets/Chelsea/Generated/Inspection*` directory. Existing inspection assets are preserved.
5. Enter Play to see the source notice and attribution panel. Use the Scene view for inspection. The scene includes an orthographic camera and one directional light; it has no XR rig or navigation controls.

Coordinates are metres: +X east, +Y elevation, +Z north. The importer does not rescale or snap footprints. Triangle budgets are checked before import; these counts are not GPU or headset performance measurements. Render geometry is deliberately not given colliders or teleport permissions because source data does not prove public access or safe walking surfaces.

The M1 scene is generated from offline JSON, with mesh/material assets persisted alongside it. The **exact original JSON bytes** are copied to `ImportedDocument.json` in that generated directory and retained by the scene's `ImportNotice` component as a `TextAsset` reference. Its SHA-256 is also recorded. The retained document preserves the complete source URL/licence/date/hash, boundary hash, source coordinates, per-building height evidence, graph interpretation, and warnings even when fields are not mapped into the rendering DTO. Select the root object and open this referenced asset in the Inspector to audit them. The source panel displays all imported warnings as well as a persistent diagnostic-road/estimated-height notice.

No imagery, streamed map tiles, API keys, or runtime network requests are used. The preview materials are diagnostic flat colours, not finished Chelsea façades. No landmarks, source heights, or streets are invented by the Unity importer.

## Required follow-up validation

- Open in pinned editor, restore and lock packages, fix any actual compiler/import errors, and run an import/reopen smoke test. Confirm the retained JSON's file hash matches the original import document and the scene still references it after reopening.
- Compare real-source geometry against the frozen baseline before completing M1.
- Author reviewed landmark silhouettes and representative façades before claiming M2.
- Implement and test explicit Meta/OpenXR adapters, controller/hand parity, verified teleport surfaces, return, tracking recovery, settings and accessibility at M3.
- Configure Android Vulkan, IL2CPP, ARM64, stereo, and measured quality settings before device profiling. This scaffold does not claim those settings or an APK are verified.
