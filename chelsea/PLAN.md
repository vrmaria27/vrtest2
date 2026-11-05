# Milestone plan

Current delivery: **reviewable M0 / first-area M1 foundation; exit gates still open**.
The brief's kickoff instruction limits this iteration to these milestones.

| Milestone | State | Exit evidence still needed |
|---|---|---|
| M0 Scope/evidence | Partial, blocked | Approved source-reviewed boundary; frozen real extract/licence review; permitted visual references; verified 300–500 m circuit |
| M1 First-area foundation | Core/import pipeline verified on synthetic fixtures; real geography/Unity blocked | Source-matched geometry/connectivity, projection comparison, real import and Unity display |
| M2 Recognisable first area | Not started | Researched façades/landmark, matched views, headset capture |
| M3 MR/street exploration | Not started | Actual XR adapters, hands/controllers, safe teleport/return, accessibility |
| M4 First-area review | Not started | Recognisable first area, ambient polish, actual user comfort/recognition review |
| M5 Expansion | Gated | M4 acceptance; approved polygon and source coverage |
| M6 Optimisation/QA | Not started | Measured on-device performance and all release gates |
| M7 Release | Not started | Real APK, offline/install checks, attribution and evidence |

## M0 / M1 report — 5 November 2026

**Done:** Preserved full brief; proposed nonrectangular study boundary and first-area
AOI; unavailable coverage and eight landmark records; reference/licence evidence;
pinned candidate Unity stack; C# geographic library; offline OSM importer; source
acquisition helper; Unity inspection source with preserved provenance.

**Verified:** .NET SDK 8.0.427 SHA-512 against Microsoft metadata. Release build:
zero warnings/errors. Core: 23 passed, zero failed. Import integration: 14 passed,
zero failed. Synthetic fixture output: 3 roads, 1 footprint, 6 meshes, 22 triangles.
Metadata checks and repeatability passed. No NuGet/network needed for validation.

**Unverified:** Real-source acquisition, source/visual matches, Unity compile/import,
APK, MR/street controls, device performance and user recognition/comfort.

**Accuracy gaps:** Boundary unreviewed; no frozen Chelsea data, measured checkpoint,
height, façade or public-access evidence. All coverage unavailable. Synthetic
fixtures are not a substitute.

**Decisions:** Continue testable core work when source/editor/device access is
missing; do not invent Chelsea. No expansion or simulation systems.

**Next steps:** Review/save/publish environment settings; retry source access;
freeze permitted source data; review boundary and first-area circuit; import and
compare geometry; run pinned Unity inspection. Complete M0/M1 gates before M2.

## Native launch work authorised after kickoff

The user requested launch options and action, then confirmed **Mac + Quest**.
The recommended path is a native first-area developer alpha. A separately labelled
synthetic diagnostic now has runtime, controller and Android build source. It
exercises a proposed launch path without marking M0/M1 or the Chelsea game complete.

**Done:** Source-loading runtime, tabletop transforms/selection/lighting/settings,
controller input foundation, focus/tracking recovery source, generated launch
scene/build method, Mac build and USB install scripts; bundled synthetic fixture.

**Verified:** API signatures/source evidence, assembly/JSON structure and shell
syntax. Eight launcher tests use mocked tools, including stale-APK rejection and
failed-build status propagation. The core/import tests remain separate.

**Unverified:** All new Unity compilation/rendering, actual package resolution,
Android build, settings persistence and headset controls. No native APK exists.
Real content and safe street mode, hands, MR and full accessibility are incomplete.

**Next:** Follow docs/LAUNCH_MAC.md locally, return actual build errors/results,
apply source network settings, then complete the reviewed first-area gates.
