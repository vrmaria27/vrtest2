# Geography contract — M0 proposal

## Boundary and exclusions

`data/boundary/chelsea-study.proposed.geojson` is a nonrectangular WGS84 design
proposal, drawn from the brief's descriptive limits, **not a reviewed geographic
baseline**. North follows a rough King's Road corridor envelope; east includes the
Sloane Square / Royal Hospital side; west ends at the Beaufort Street / Battersea
Bridge approach; south contains river context. Vertex precision is storage
precision, not a claim of surveyed accuracy. Boundary/source matching and landmark
inclusion review remain open. Do not use this as permission to import southern-bank
streets or expand into adjacent neighbourhoods. A source-reviewed land/river mask
is required before wider release import; polygon clipping alone does not enforce
public-access or northern-bank semantics.

The separate first-area AOI is a proposed Duke of York Square / immediate King's
Road import envelope. It is not a walking route. The 300–500 m circuit cannot be
selected until actual source graph, pavement geometry and public access are
reviewed. `data/routes/discovery.pending.geojson` deliberately has no features.

All coverage is `unavailable` in `data/coverage.geojson`. Nothing is marked complete
or approximate reconstruction. The eight priority landmark IDs are reserved but
have no fabricated measured coordinates, silhouettes or walkable surfaces.

## Coordinate reference system

Raw OSM nodes retain longitude/latitude in WGS84 (EPSG:4326; GeoJSON longitude
first). Source IDs are namespaced by OSM element type. World units are metres.
Stable origin: longitude **−0.1589**, latitude **51.4905**. This is an intentionally
chosen computational origin, not a measured landmark coordinate.

`LocalProjection` uses an equirectangular local tangent approximation on a sphere,
R=6,378,137 m, at the origin latitude:

```
east  = R * (lon-lon0) * pi/180 * cos(lat0*pi/180)
north = R * (lat-lat0) * pi/180
Unity = (east, elevation, north)
```

It is invertible and deterministic for this small area, and does not claim OSGB36
or survey accuracy. No datum shift or terrain elevation is applied. Distortion vs
an authoritative metric projection must be evaluated over the final boundary
before the proposed 3 m source-fidelity gate can pass. Height is separate metres
above a flat local plane. Footprint height evidence remains distinct from origin
or projection precision. Tabletop scale will be a viewing transform, not a change
to these coordinates. Chunk indices use floor(east/100), floor(north/100), with
correct negative-coordinate handling; chunking never snaps source coordinates.

## Import and gaps

The offline importer accepts pinned OSM XML, an explicit polygon, and provenance
with a matching SHA-256. It is a diagnostic first-area pipeline, not a general OSM
renderer. Consult importer help/documentation for unsupported geometry diagnostics.
Road centreline ribbons are presentation diagnostics, **not measured road widths,
pavements or teleport surfaces**. At-grade connectivity cannot be inferred from
geometric crossings; source node relationships are retained. Layers, turn
restrictions and access semantics need review before ambient routing. Buildings
with no explicit heights are internally labelled estimated.

No geographic snapshot has been downloaded: the official OSM API CONNECT request
was denied by policy on 2026-11-05. Google Maps/Street View was not visually
reviewed. There are no 20-checkpoint, height, landmark or matched-camera accuracy
results. See ACCURACY_REVIEW.md. No invented Chelsea replacement is supplied.
