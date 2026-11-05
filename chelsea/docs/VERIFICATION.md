# Verification evidence — 5 November 2026

This is an implementation report for the spec's M0 / first-area M1 kickoff.
It does not certify a finished Chelsea replica. See PLAN.md for milestone gates.

## Executed checks

| Capability | Method and result | Status |
|---|---|---|
| SDK toolchain | .NET8.0.427 Linux x64 archive checked against official Microsoft release-metadata SHA-512, then extracted; executable reports8.0.427 | Verified |
| Retained setup | `bash tools/build/bootstrap-dotnet.sh` from chelsea reused installed pinned SDK successfully | Verified |
| Core algorithms | Release executable suite: **23 executed, 23 passed, 0 failed, 0 skipped** | Verified on synthetic fixtures |
| Importer compilation | Release net8.0 build: **0 warnings, 0 errors**; referenced netstandard2.1 core | Verified |
| Import integration | Python unittest: **14 executed, 14 passed, 0 failed, 0 skipped** | Verified on synthetic fixtures |
| Metadata | JSON and asmdef syntax; proposed boundary envelopes; unavailable coverage; eight IDs; empty pending route; CSV shape | Verified structurally |
| Fixture output | CLI via `tools/build/run-dotnet.sh`: 3 roads, 1 building footprint, 6 meshes, 22 triangles, 2 source graph components, 1 unsupported relation; complete=false | Verified synthetic only |
| Repeatability | Two identical imports yield byte-identical output; reordered source elements retain meshes/hash/topology; bad input preserves prior output | Verified |
| Acquisition helper | Python compilation and `--help` executed; no live successful download | Syntax only; network operation unverified |
| Official stack/ODbL evidence | Exact-commit official GitHub source files actually retrieved; hashes/URLs in TECH_DECISIONS and reference register | Verified source access; not package compatibility |
| Configuration persistence | Draft tool confirmed `status=saved` for network domains, install_script and start_skill; `requires_publish=true` | Saved, not applied/published |

`bash tools/validation/verify.sh` is the complete offline check entry point.
The initial wrapper invocation incorrectly passed a positional SDK path to the
Python test runner; it failed. The wrapper was corrected to use its documented
`--dotnet`/`--dll` arguments and the full suite passed. A malformed-boundary schema
issue and Unity provenance retention issue found in review were corrected before
the final run. This report records the final code, not the initial failures as passes.

Core tests cover origin/independent metric fixture, 121 coordinate roundtrips,
invalid/domain rejection, simple/concave clipping, invalid rings, deterministic
extrusion and roof normals, chunk floor behavior, stable source IDs, directed
routing, weak components and missing/duplicate graph IDs. Those coordinate
fixtures test mathematics; none is a measured Chelsea checkpoint.

Integration tests cover provenance hashes, determinism, source ordering,
connectivity without coordinate snapping, mesh counts/indices, diagnostic width
and height labels, missing source nodes, unsafe XML, malformed boundary schemas,
self-intersecting footprints, boundary-crossing buildings, zero-length roads,
unknown kinds, missing provenance, and atomic failure behavior. Relation warnings
are retained and completeness is false; relations are not reconstructed.

## Blocked or unrun checks

- **OSM data:** official API attempt returned proxy CONNECT403. No extract received,
  no real snapshot frozen and no actual Chelsea meshes generated.
- **Boundary / first circuit:** proposed only. No authoritative positions, source
  clip comparison or public pavement/access review. No approved 300–500 m route.
- **Google Maps / Street View:** not visually reviewed. Direct terms/guidelines
  requests denied; no imagery/mesh/textures were acquired or used. Official OSM
  GitHub licence text was accessible; legal-code/current-direct-guideline review
  remains listed separately in the reference register.
- **Landmark / accuracy:** none of the eight priority settings visually completed;
  20 checkpoint rows are planned, not measured. No <=3 m/height target passed.
- **Unity:** pinned project source/Editor importer only. No editor installed; no
  compilation, package lock, scene import, shader/render or package-matrix test.
  Full JSON is retained as a referenced TextAsset by the source implementation;
  actual save/reopen must be tested in Unity.
- **XR / release:** no MR, street mode, hand/controller parity, settings save/load,
  accessible teleport, ambient simulation, audio or APK. Interfaces do not count
  as working integrations. These are subsequent milestones.
- **Device/user:** no Quest3/3S, thermal/memory/latency tests, recognition reviewer
  or comfort evaluation. No performance or compatibility claim.

## Reproduce and continue

From `/workspace/vrtest2/chelsea`, run `bash tools/validation/verify.sh`.
Generated fixture JSON is retained locally at
`data/processed/synthetic.meshes.json` and ignored by Git; reproduce using README.
No application service or localhost preview is required. No running process is
assumed to survive publication.

Review and save the changes in environment settings, then publish the environment.
Saved custom network domains are: `api.openstreetmap.org`,
`www.openstreetmap.org`, `osmfoundation.org`, `www.google.com`, `about.google`,
`www.rbkc.gov.uk`, `docs.unity3d.com`, `unity.com`, `developers.meta.com`.
Package-manager presets were preserved. Saving the draft did not apply these to
the current proxy or publish a snapshot; no fresh-task restoration was tested.
No API keys or new secrets are needed for the attempted public OSM download.
After a meaningful settings change, retry acquisition and continue the open M0/M1
gates. For Unity/device work, provide the pinned licensed editor/build modules and
actual hardware. Do not substitute a web mockup or invented streets for the brief.

## Subsequent native launch foundation — Mac + Quest

The user requested a native launch path after the initial milestone and confirmed
Mac + Quest hardware. New runtime/controller/Editor build source and Mac launchers
are described in LAUNCH_MAC.md. This authorises diagnostic work beyond kickoff;
it does not complete the first-area geographic or game gates above.

Eight `test-macos-launchers.py` tests passed using **mock executables**, not Unity
or a headset. They check explicit diagnostic opt-in, missing tools/artifacts,
failed-build exit propagation, stale output rejection, unauthorised/multiple
devices and explicit device targeting. The shell scripts pass `bash -n`.
No mocked text file is a produced APK or device result. New native C# was statically
reviewed against the source signatures in NATIVE_API_EVIDENCE.md; the entire Unity
project remains uncompiled/unrendered here. Controller tracking/rearming and
initial/floor-origin issues found by review were addressed in source only.

The diagnostic's actual first run/build/install must occur in the pinned Editor
and on the user's Quest. Hands, MR passthrough/anchors, street teleport, researched
Chelsea art, full accessibility and release performance still need implementation
and validation. No HTML viewer, source interface or build script is a VR launch.
