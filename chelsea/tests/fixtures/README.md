# Synthetic fixtures, not Chelsea

`synthetic.osm` is original test data dedicated to CC0-1.0. Its WGS84 numbers sit near the fixed coordinate origin solely to exercise the local projection. They are not surveyed or imported streets, buildings, landmarks, public access evidence, a first-area walking route, or an accuracy baseline. Do not ship these meshes as Chelsea.

The fixture contains two roads sharing an explicit node, a separate road with a coincident but different node ID, one square footprint and one deliberately unsupported relation. Expected source topology is two undirected connected components, seven graph nodes and five edges. The footprint's `height=8 m` demonstrates a claimed source tag, not a measured building.

`synthetic-boundary.geojson` is a rectangular test boundary only. The actual proposed study boundary is separately maintained under `data/boundary/`.

`synthetic.manifest.json` freezes the fixture bytes by SHA-256. Failure-case tests should create temporary modified files and independently calculated fixture manifests; never change a real source hash merely to make a failed import pass.
