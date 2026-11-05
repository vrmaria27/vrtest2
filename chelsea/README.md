# Holopolis: Chelsea — geographic foundation

This repository implements the **M0 + first-area M1 kickoff** in
[the attached design specification](docs/DESIGN_SPEC.md). It is not yet a playable
Chelsea replica or Quest build. See [PLAN.md](PLAN.md) and the exact
[verification evidence](docs/VERIFICATION.md).

## Native launch path (Mac + Quest)

The user requested a native launch path after the initial foundation. See
[LAUNCH_MAC.md](docs/LAUNCH_MAC.md) for options, blockers, the recommended alpha
route, local Editor configuration, build and USB installation commands. New
runtime/controller/build source is **not Unity-compiled or headset-verified**.
The bundled diagnostic is synthetic and has no safe street mode, hands or MR.

## What is here

- A dependency-free .NET Standard 2.1 geographic library: fixed-origin WGS84/local
  conversion, valid simple polygons, concave-boundary segment clipping, connected
  road graphs, stable OSM IDs, deterministic extrusion/ribbon meshes and chunks.
- An offline .NET 8 OSM XML importer with provenance checks and diagnostic geometry.
- Original **synthetic-only** fixtures to test the pipeline without inventing a
  Chelsea dataset.
- A Unity 6 inspection project that imports generated JSON meshes into a saved
  scene with source labels and attribution. No XR rig or teleport colliders exist.
- Explicitly proposed boundary/AOI GeoJSON, unavailable coverage, eight priority
  landmark records, reference register and accuracy-review checklist.

**Current source gap:** the official OSM API request was blocked by proxy policy.
No real extract, Google Maps/Street View review, matched-view check, public walking
circuit or completed landmark model is claimed. The first-area 300–500 m route is
pending source/access review. The study polygon is reviewable, not approved.

## Build and validate

Requirements: Linux x64 bootstrap (or install the matching SDK for your OS), Bash,
Python 3, curl and .NET SDK **8.0.427**. No NuGet packages are used. From this folder:

```sh
bash tools/build/bootstrap-dotnet.sh
bash tools/validation/verify.sh
```

The bootstrap installs to `/workspace/.tools/dotnet` (override with
`CHELSEA_DOTNET_ROOT`) and verifies the SDK's SHA-512 from Microsoft's official
release metadata. Set `CHELSEA_DOTNET` if using a custom executable.
`verify.sh` executes the core test console, builds the importer, runs its end-to-end
cases, and checks metadata. Failures preserve the command's nonzero exit status.
Build output and fixture-derived files are ignored. No network is needed after
SDK installation. The verification report records what actually ran.

### Real-source import

Read [the importer usage](tools/import/README.md) for arguments, source manifest
fields, supported geometry and limits. Download a bounded, permitted extract only
when the configured OSM API domain is usable. Freeze its raw bytes and timestamp,
record the SHA-256, and retain the required attribution/database notices. An explicit acquisition tool is
available (network path still blocked and unverified):

```sh
python3 tools/source/fetch_osm.py --output-dir data/source_local/first-area-v1
```

It refuses to overwrite a retained snapshot and records raw bytes, a hash and
provenance; it is never called during build or in the installed experience. Source
and derived geographic data must not silently refresh on build. Do not overwrite
or label the synthetic test manifest as real source data.

Use the proposed first-area AOI only as an import/review candidate. The 403 denial
requires the saved network settings to be applied before acquisition; it is not a
request for an API key. Wider Chelsea data must wait for boundary review and a
northern-bank/access mask. Unsupported relations, incomplete footprints and
restricted routing need explicit diagnosis rather than invented replacements.

### Unity inspection

Open `unity/` in **6000.0.66f2**, restore its pinned packages, and follow
[unity/README.md](unity/README.md) to create a URP inspection configuration and
import JSON. To retain a fixture for that import:

```sh
bash tools/build/run-dotnet.sh run --project tools/import -c Release -- \
  --input tests/fixtures/synthetic.osm \
  --boundary tests/fixtures/synthetic-boundary.geojson \
  --manifest tests/fixtures/synthetic.manifest.json \
  --output data/processed/synthetic.meshes.json
```

Editor/API source evidence is in [TECH_DECISIONS.md](docs/TECH_DECISIONS.md).
The editor is not installed here; C# core success does not prove Unity compilation.

No Meta SDK methods are guessed. XR interfaces reserve future adapters. Placement,
MR, street exploration, settings, audio, ambient agents, accessibility, APK and
headset profiling remain subsequent gated work, not hidden behind a demo.

## Evidence and scope

[Geography](docs/GEOGRAPHY.md) documents axes, units, projection and limitations.
[Attribution](docs/ATTRIBUTION.md) separates reusable source terms from research
references. [Accuracy review](docs/ACCURACY_REVIEW.md) has 20 planned checks with no
fabricated observations. [Manual QA](docs/MANUAL_QA.md) and
[performance report](docs/PERF_REPORT.md) list unrun release gates.

Continue in this existing checkout; cloud tasks are already isolated. No worktree
is needed. Next: acquire rights-cleared first-area sources, review the boundary,
compare real geometry, run Unity inspection, then enter M2. Do not expand the map
until the recognisable first area and its review gates are complete.
