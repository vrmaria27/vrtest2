# Geographic core

`Chelsea.Core` is a dependency-free `netstandard2.1` library, separate from Unity and XR packages. `Chelsea.Core.Tests` is an executable .NET 8 test suite; it returns a nonzero exit code on any failure and prints actual executed test counts. It does not depend on a NuGet test runner.

From `chelsea/`, with the pinned .NET SDK available:

```sh
dotnet run --project dotnet/Chelsea.Core.Tests/Chelsea.Core.Tests.csproj -c Release
```

The synthetic fixtures test algorithms; they are not observations of Chelsea and do not satisfy geographic or headset review gates.

## Contracts

- `GeoCoordinate` stores WGS84 longitude then latitude, in degrees.
- `LocalProjection` uses an explicit fixed origin. `ChelseaOrigin` is `(-0.1589, 51.4905)`. It uses an equirectangular projection on radius 6,378,137 m, rejects coordinates beyond 5 km, and is a documented presentation coordinate system rather than a survey accuracy claim.
- `Point2.X` is east; `Point2.Y` is north, in metres. Meshes map these to Unity X/Z, with Y as elevation.
- `Polygon2D` accepts one simple open or closed ring, validates it, normalizes counterclockwise winding, and treats its boundary as contained. Holes and multipolygons need explicit handling by the importer. Self-touching, intersecting, overlapping, or zero-area rings fail.
- `ClipSegment` handles concave boundaries and returns all nonzero inside portions. Created clipping endpoints are display geometry, not new source road junctions. Polygon-versus-polygon footprint clipping is not implemented; the importer must report cross-boundary buildings rather than silently distorting them.
- `ExtrudeFootprint` produces deterministic flat-roof massing with top, bottom and side faces. It does not generate researched architecture, roofs, textures, pavement access, collision rules, or finished landmarks. Ribbon width is an explicit presentation input, not an inferred surveyed street width.
- `ChunkKey` floors east/north coordinates by chunk size, including negative coordinates. Chunk assignment does not snap geometry.
- `SourceIdentity.Osm` namespaces node/way/relation IDs without dependence on locale. Road graph edges require their own stable segment IDs; two positions coincide only if their source node IDs are actually shared.
- `RoadGraph` detects weak components and computes deterministic shortest directed paths. It is an import connectivity diagnostic, not a pedestrian/traffic simulation. Turn restrictions, lane rules, public access and verified walking surfaces are outside this core gate.

No per-frame loops, external services, hidden network requests, credentials or Unity APIs are present in the library.
