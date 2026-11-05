# Offline OSM diagnostic importer

From `chelsea/`, use the repository's SDK wrapper (it selects SDK 8.0.427 and writable CLI/cache paths):

```sh
tools/build/run-dotnet.sh run --project tools/import/Chelsea.Import.csproj -- \
  --input tests/fixtures/synthetic.osm \
  --boundary tests/fixtures/synthetic-boundary.geojson \
  --manifest tests/fixtures/synthetic.manifest.json \
  --output data/processed/synthetic.meshes.json
```

Run integration tests after a Release build with:

```sh
tools/build/run-dotnet.sh build tools/import/Chelsea.Import.csproj -c Release
python3 tools/import/test_import.py --dotnet /workspace/.tools/dotnet/dotnet \
  --dll tools/import/bin/Release/net8.0/Chelsea.Import.dll
```

For other SDK locations, pass that executable through `--dotnet`. The tests use temporary original synthetic files only; no network or Unity Editor is required. `tools/validation/verify.sh` runs the core and importer checks together.

This example produces **synthetic tests**, not a Chelsea model. For a frozen OSM XML extract, provide a reviewed manifest with `datasetKind: "osm-source"`, `licence: "ODbL-1.0"`, HTTPS `sourceUrl`, ISO `snapshotDate` (`yyyy-MM-dd`) and the exact lowercase `sha256` of the raw bytes. Synthetic manifests use `datasetKind: "synthetic-test-fixture"` and `licence: "CC0-1.0"`. Hash matching establishes byte identity, not rights, location accuracy, completeness or trustworthiness; provenance remains a human review requirement. No download occurs at runtime.

Boundary input is a WGS84 GeoJSON Polygon, Feature or one-feature FeatureCollection with one explicitly closed ring. Holes are rejected. Nodes outside the documented 5 km local-projection domain fail. Input limits are 25 MB per file. XML DTDs and external entities are prohibited.

The importer preserves source WGS84 positions and source IDs, clips road segments against the polygon, generates thin 0.4 m diagnostic centerline ribbons and extrudes wholly contained simple building footprints. Ribbon widths are presentation only. Heights are source tag claims; absent height tags use source levels × an assumed 3 m, or a labelled unsupported 9 m diagnostic estimate. Unknown height units, invalid/self-intersecting polygons, missing source nodes, zero-length road segments, duplicate source IDs, malformed manifests, and boundary-crossing buildings fail before output is written. Polygon footprint clipping is not implemented. **Do not move a release boundary merely to hide source errors.** Review an appropriate import AOI independently of the release boundary when retaining entire source footprints is necessary.

Source-node graph diagnostics include original endpoints outside the boundary for intersecting edges, explicitly flagged. Distinct source nodes never merge merely because their positions coincide. Graph connectivity is undirected and diagnostic; oneway/access tags are retained but unvalidated. The graph is not a traffic router or walkable navigation mesh, and does not resolve grade separation, restrictions or access permissions. Relation geometry is not imported: all relation IDs are listed as unsupported, and `complete` is always `false`.

Output is deterministic for identical bytes: sorted source features, warnings and nodes; no wall-clock timestamps or random data. A temporary file replaces the requested output only after full validation. Existing output is preserved if an import fails; exit status is nonzero and stderr states that no output was written. `contentHash` is the SHA-256 of compact serialized mesh records, while source/boundary hashes separately identify inputs. Generated mesh records contain vertices (Unity X=east, Y=up, Z=north), triangle indices, source ID, kind and part number. Roads, footprints, source coordinates, chunks, topology, budget counts and limitations remain in the JSON.

Unsupported: land-use/terrain, road surfaces and pavements, researched façades/landmarks, footprint holes/multipolygons, relation restrictions, safe public walking routes and all XR interaction. A successful import verifies the supported conversion, not these features or a milestone exit gate.
