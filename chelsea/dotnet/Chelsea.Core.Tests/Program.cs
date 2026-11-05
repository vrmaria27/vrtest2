using System.Globalization;
using Chelsea.Core;

var tests = new List<(string Name, Action Test)>();
void Test(string name, Action test) => tests.Add((name, test));
void Check(bool value, string message = "Assertion failed") { if (!value) throw new Exception(message); }
void Near(double actual, double expected, double tolerance = 1e-8) => Check(Math.Abs(actual - expected) <= tolerance, $"Expected {expected:R}, got {actual:R}");
void Throws<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new Exception("Expected " + typeof(T).Name);
}
Polygon2D Ring(params double[] pairs) => new Polygon2D(Enumerable.Range(0, pairs.Length / 2).Select(i => new Point2(pairs[i * 2], pairs[i * 2 + 1])));
var projection = new LocalProjection(LocalProjection.ChelseaOrigin);
Test("Fixed origin maps to zero", () => { var p = projection.Project(LocalProjection.ChelseaOrigin); Near(p.X, 0); Near(p.Y, 0); });
Test("Independent east/north scale fixture", () =>
{
    var p = projection.Project(new GeoCoordinate(-0.1579, 51.4915));
    Near(p.X, 69.3124563762334, 0.001); Near(p.Y, 111.319490793274, 0.001);
});
Test("Coordinate roundtrip distributed within domain", () =>
{
    for (int x = -20; x <= 20; x += 4) for (int y = -20; y <= 20; y += 4)
    {
        var source = new GeoCoordinate(-0.1589 + x * 0.001, 51.4905 + y * 0.001);
        var result = projection.Unproject(projection.Project(source)); Near(result.Longitude, source.Longitude, 1e-12); Near(result.Latitude, source.Latitude, 1e-12);
    }
});
Test("Invalid geographic coordinates rejected", () => { Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(double.NaN, 51)); Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(0, 91)); });
Test("Projection domain enforced", () => Throws<ArgumentOutOfRangeException>(() => projection.Project(new GeoCoordinate(1, 51))));
Test("Unprojection domain and polar origin rejected", () => { Throws<ArgumentOutOfRangeException>(() => projection.Unproject(new Point2(5001, 0))); Throws<ArgumentException>(() => new LocalProjection(new GeoCoordinate(0, 90))); });
Test("Nonfinite local coordinates rejected", () => Throws<ArgumentException>(() => new Point2(double.PositiveInfinity, 0)));
Test("Closed clockwise polygon normalizes", () => { var p = Ring(0,0, 0,10, 10,10, 10,0, 0,0); Check(p.Vertices.Count == 4); Near(p.Area, 100); });
Test("Boundary points count as inside", () => { var p = Ring(0,0, 10,0, 10,10, 0,10); Check(p.Contains(new Point2(0,5))); Check(p.Contains(new Point2(10,10))); Check(!p.Contains(new Point2(11,5))); });
Test("Bowtie and degenerate polygon rejected", () => { Throws<ArgumentException>(() => Ring(0,0, 10,10, 0,10, 10,0)); Throws<ArgumentException>(() => Ring(0,0, 1,0, 2,0)); });
Test("Repeated vertex and adjacent overlap rejected", () => { Throws<ArgumentException>(() => Ring(0,0, 10,0, 10,0, 0,10)); Throws<ArgumentException>(() => Ring(0,0, 10,0, 5,0, 10,10, 0,10)); });
Test("Simple clipping preserves exact intersections", () => { var clips = Ring(0,0, 10,0, 10,10, 0,10).ClipSegment(new Point2(-5,5), new Point2(15,5)); Check(clips.Count == 1); Near(clips[0].Start.X, 0); Near(clips[0].End.X, 10); });
Test("Concave boundary returns separated portions", () =>
{
    var p = Ring(0,0, 10,0, 10,10, 7,10, 7,3, 3,3, 3,10, 0,10);
    var parts = p.ClipSegment(new Point2(-1,5), new Point2(11,5)); Check(parts.Count == 2); Near(parts.Sum(x => x.Length), 6);
});
Test("Collinear boundary and outside segments", () =>
{
    var p = Ring(0,0, 10,0, 10,10, 0,10); var parts = p.ClipSegment(new Point2(-5,0), new Point2(15,0));
    Check(parts.Count == 1); Near(parts[0].Length, 10); Check(p.ClipSegment(new Point2(-5,-1), new Point2(15,-1)).Count == 0);
    Check(p.ClipSegment(new Point2(0,0), new Point2(0,0)).Count == 0);
});
Test("Concave extrusion triangulates exact roof area", () =>
{
    var polygon = Ring(0,0, 10,0, 10,3, 3,3, 3,10, 0,10); var mesh = GeometryMesh.ExtrudeFootprint(polygon, 12);
    double area = 0;
    for (var i = 0; i < mesh.Indices.Count; i += 3)
    {
        var a = mesh.Vertices[mesh.Indices[i]]; var b = mesh.Vertices[mesh.Indices[i+1]]; var c = mesh.Vertices[mesh.Indices[i+2]];
        if (a.Y != 12 || b.Y != 12 || c.Y != 12) continue;
        var signedY = (b.Z - a.Z) * (c.X - a.X) - (b.X - a.X) * (c.Z - a.Z);
        Check(signedY > 0, "Top face points down"); area += signedY / 2;
    }
    Near(area, polygon.Area); Check(mesh.Vertices.Count == 12); Check(mesh.TriangleCount == 20);
});
Test("Collinear footprint triangulates without zero triangles", () =>
{
    var mesh = GeometryMesh.ExtrudeFootprint(Ring(0,0, 5,0, 10,0, 10,10, 0,10), 3);
    for (var i = 0; i < mesh.Indices.Count; i += 3)
    {
        var a = mesh.Vertices[mesh.Indices[i]]; var b = mesh.Vertices[mesh.Indices[i+1]]; var c = mesh.Vertices[mesh.Indices[i+2]];
        var ux=b.X-a.X; var uy=b.Y-a.Y; var uz=b.Z-a.Z; var vx=c.X-a.X; var vy=c.Y-a.Y; var vz=c.Z-a.Z;
        Check(Math.Abs(uy*vz-uz*vy) + Math.Abs(uz*vx-ux*vz) + Math.Abs(ux*vy-uy*vx) > 1e-8);
    }
});
Test("Mesh generation deterministic, bounded and upward ribbon", () =>
{
    var p = Ring(0,0, 10,0, 10,10, 0,10); var a = GeometryMesh.ExtrudeFootprint(p, 4); var b = GeometryMesh.ExtrudeFootprint(p, 4);
    Check(a.Indices.SequenceEqual(b.Indices)); Check(a.Vertices.SequenceEqual(b.Vertices)); Check(a.Indices.All(i => i >= 0 && i < a.Vertices.Count));
    var road = GeometryMesh.CreateRibbon(new[] { new Point2(0,0), new Point2(10,0) }, 4);
    Check(road.TriangleCount == 2); Near(road.Vertices.Min(v => v.Z), -2); Near(road.Vertices.Max(v => v.Z), 2);
    var v0=road.Vertices[road.Indices[0]]; var v1=road.Vertices[road.Indices[1]]; var v2=road.Vertices[road.Indices[2]];
    Check((v1.Z-v0.Z)*(v2.X-v0.X)-(v1.X-v0.X)*(v2.Z-v0.Z)>0);
});
Test("Invalid extrusion and ribbons rejected", () => { Throws<ArgumentOutOfRangeException>(() => GeometryMesh.ExtrudeFootprint(Ring(0,0, 1,0, 0,1), -1)); Throws<ArgumentException>(() => GeometryMesh.CreateRibbon(new[]{new Point2(0,0),new Point2(0,0)}, 3)); });
Test("Chunks floor negative coordinates, preserve geometry", () => { Check(ChunkKey.FromPoint(new Point2(-0.1,-100)).ToString() == "-1:-1"); Check(ChunkKey.FromPoint(new Point2(100,100)).ToString() == "1:1"); Throws<ArgumentOutOfRangeException>(() => ChunkKey.FromPoint(new Point2(0,0), 0)); });
Test("Source identities stable across cultures and namespaces", () =>
{
    var original = CultureInfo.CurrentCulture;
    try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA"); Check(SourceIdentity.Osm("way", 123456) == "osm:way:123456"); Check(SourceIdentity.Osm("node", 1) != SourceIdentity.Osm("way", 1)); }
    finally { CultureInfo.CurrentCulture = original; }
    Throws<ArgumentException>(() => SourceIdentity.Osm("building", 2)); Throws<ArgumentOutOfRangeException>(() => SourceIdentity.Osm("way", 0));
});
RoadNode Node(string id, double x = 0) => new RoadNode(id, new Point2(x, 0));
Test("Road components include isolated coincident source nodes", () =>
{
    var graph = new RoadGraph(new[]{ Node("a"), Node("b",10), Node("other-source-same-position") }, new[]{new RoadEdge("ab","a","b",10)});
    Check(graph.ConnectedComponents().Count == 2); Check(graph.ShortestPath("a","other-source-same-position") == null);
});
Test("Road route respects direction and shortest path", () =>
{
    var graph = new RoadGraph(new[]{Node("a"),Node("b",10),Node("c",20)}, new[]{new RoadEdge("ab","a","b",10,true),new RoadEdge("bc","b","c",10),new RoadEdge("ac","a","c",30,true)});
    var path = graph.ShortestPath("a","c"); Check(path != null); Near(path!.LengthMetres,20); Check(path.EdgeIds.SequenceEqual(new[]{"ab","bc"}));
    Check(graph.ShortestPath("c","a") == null); Near(graph.ShortestPath("a","a")!.LengthMetres,0); Check(graph.ConnectedComponents().Count == 1);
});
Test("Road graph rejects missing/duplicate source IDs", () =>
{
    Throws<ArgumentException>(() => new RoadGraph(new[]{Node("a")},new[]{new RoadEdge("e","a","b",1)}));
    Throws<ArgumentException>(() => new RoadGraph(new[]{Node("a"),Node("a")},Array.Empty<RoadEdge>()));
    Throws<ArgumentException>(() => new RoadGraph(new[]{Node("a"),Node("b")},new[]{new RoadEdge("e","a","b",1),new RoadEdge("e","b","a",1)}));
});

var failed = 0;
foreach (var (name, test) in tests)
{
    try { test(); Console.WriteLine("PASS " + name); }
    catch (Exception e) { failed++; Console.Error.WriteLine("FAIL " + name + ": " + e.Message); }
}
Console.WriteLine($"{tests.Count-failed} passed, {failed} failed, {tests.Count} executed. Synthetic unit fixtures; no Chelsea ground-truth claim.");
return failed == 0 ? 0 : 1;
