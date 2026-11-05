using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Chelsea.Core;

// Offline diagnostic importer. Neither a navigation mesh nor an accuracy claim.
return Importer.Run(args);

internal static class Importer
{
    private const double DiagnosticRoadWidth = 0.4;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly LocalProjection Projection = new(LocalProjection.ChelseaOrigin);

    internal static int Run(string[] args)
    {
        if (args.Length == 1 && args[0] == "--help")
        {
            Console.WriteLine("Offline diagnostic importer (not a navigation mesh or verified Chelsea model).\nUsage: --input source.osm --boundary boundary.geojson --manifest source.json --output meshes.json\nRequired manifest: datasetKind, licence, sourceUrl, snapshotDate (yyyy-MM-dd), sha256. See tools/import/README.md.");
            return 0;
        }
        try
        {
            var options = Arguments(args);
            var input = Path.GetFullPath(options["input"]);
            var boundaryPath = Path.GetFullPath(options["boundary"]);
            var manifestPath = Path.GetFullPath(options["manifest"]);
            var output = Path.GetFullPath(options["output"]);
            if (new[] { input, boundaryPath, manifestPath }.Contains(output, StringComparer.Ordinal))
                throw new InvalidDataException("Output must not overwrite an input or manifest.");
            var sourceBytes = ReadLimited(input);
            var boundaryBytes = ReadLimited(boundaryPath);
            var sourceHash = Sha256(sourceBytes);
            var manifest = ReadManifest(ReadLimited(manifestPath), sourceHash);
            var boundary = ReadBoundary(boundaryBytes);
            var (nodes, ways, relations) = ReadOsm(sourceBytes);
            var warnings = new SortedSet<string>(StringComparer.Ordinal);
            var meshes = new List<MeshOutput>();
            var roads = new List<object>();
            var buildings = new List<object>();
            var graphNodes = new SortedDictionary<long, object>();
            var edges = new List<GraphEdge>();
            int ignoredWays = 0, outsideWays = 0;
            foreach (var relation in relations)
                warnings.Add($"Unsupported relation {relation}; multipolygons, restrictions and relation geometry require review. Dataset completeness is blocked.");

            foreach (var way in ways.Values.OrderBy(w => w.Id))
            {
                bool building = way.Tags.TryGetValue("building", out var value) && value != "no";
                bool highway = way.Tags.ContainsKey("highway");
                if (!building && !highway) { ignoredWays++; continue; }
                if (way.NodeIds.Count < 2) throw new InvalidDataException($"Way {way.Id} has fewer than two nodes.");
                var positions = way.NodeIds.Select(id => nodes.TryGetValue(id, out var coordinate)
                    ? Projection.Project(coordinate)
                    : throw new InvalidDataException($"Way {way.Id} references missing node {id}; fetch complete ways before import.")).ToArray();
                string id = SourceIdentity.Osm("way", way.Id);
                if (building)
                {
                    if (way.NodeIds.Count < 4 || way.NodeIds[0] != way.NodeIds[^1])
                        throw new InvalidDataException($"Building {id} must be a closed source footprint.");
                    var polygon = new Polygon2D(positions.Take(positions.Length - 1));
                    bool[] inside = polygon.Vertices.Select(boundary.Contains).ToArray();
                    bool edgeInside = Enumerable.Range(0, polygon.Vertices.Count).Any(i =>
                        boundary.ClipSegment(polygon.Vertices[i], polygon.Vertices[(i + 1) % polygon.Vertices.Count]).Count > 0);
                    bool surroundsBoundary = boundary.Vertices.Any(polygon.Contains);
                    if (!inside.Any(x => x) && !edgeInside && !surroundsBoundary) { outsideWays++; continue; }
                    if (!inside.All(x => x) || Enumerable.Range(0, polygon.Vertices.Count).Any(i =>
                        Math.Abs(boundary.ClipSegment(polygon.Vertices[i], polygon.Vertices[(i + 1) % polygon.Vertices.Count]).Sum(s => s.Length)
                            - polygon.Vertices[i].DistanceTo(polygon.Vertices[(i + 1) % polygon.Vertices.Count])) > 1e-5))
                        throw new InvalidDataException($"Building {id} crosses the boundary. Footprint polygon clipping is not implemented; enlarge the source AOI or review boundary, never trim by centroid.");
                    var (height, heightEvidence) = Height(way);
                    var mesh = GeometryMesh.ExtrudeFootprint(polygon, height);
                    meshes.Add(Mesh(id, "building-footprint-extrusion", 0, mesh));
                    var chunk = ChunkKey.FromPoint(polygon.Vertices[0]);
                    buildings.Add(new { sourceId = id, sourceCoordinates = Coordinates(way.NodeIds, nodes), heightMetres = height,
                        heightEvidence, roof = way.Tags.GetValueOrDefault("roof:shape", "unavailable"), chunk = chunk.ToString(),
                        note = "Footprint extrusion only; not a researched facade, terrain elevation, landmark silhouette or measured height." });
                }
                if (highway)
                {
                    int part = 0;
                    var pieces = new List<object>();
                    for (int i = 0; i < positions.Length - 1; i++)
                    {
                        if (way.NodeIds[i] == way.NodeIds[i + 1] || positions[i].DistanceTo(positions[i + 1]) < 1e-8)
                            throw new InvalidDataException($"Road {id} has a zero-length segment.");
                        var clipped = boundary.ClipSegment(positions[i], positions[i + 1]);
                        if (clipped.Count == 0) continue;
                        foreach (var segment in clipped)
                        {
                            meshes.Add(Mesh(id, "diagnostic-road-centerline", part++, GeometryMesh.CreateRibbon(new[] { segment.Start, segment.End }, DiagnosticRoadWidth)));
                            pieces.Add(new { start = P2(segment.Start), end = P2(segment.End), chunk = ChunkKey.FromPoint(segment.Start).ToString() });
                        }
                        long from = way.NodeIds[i], to = way.NodeIds[i + 1];
                        foreach (long node in new[] { from, to })
                        {
                            var position = Projection.Project(nodes[node]);
                            graphNodes[node] = new { sourceId = SourceIdentity.Osm("node", node), longitude = nodes[node].Longitude,
                                latitude = nodes[node].Latitude, position = P2(position), insideBoundary = boundary.Contains(position) };
                        }
                        edges.Add(new GraphEdge(SourceIdentity.Osm("node", from), SourceIdentity.Osm("node", to), id,
                            positions[i].DistanceTo(positions[i + 1]), way.Tags.GetValueOrDefault("oneway", "unspecified"), way.Tags.GetValueOrDefault("access", "unspecified")));
                    }
                    if (pieces.Count == 0) { if (!building) outsideWays++; continue; }
                    roads.Add(new { sourceId = id, sourceCoordinates = Coordinates(way.NodeIds, nodes), highway = way.Tags["highway"],
                        name = way.Tags.GetValueOrDefault("name", ""), pieces,
                        diagnosticWidthMetres = DiagnosticRoadWidth, widthEvidence = "presentation-only; not geographic road width",
                        walkable = false, note = "Graph edges retain complete source node pairs; exterior endpoints are flagged. No access, layer, junction restriction, lane or safe teleport certification." });
                }
            }
            if (meshes.Count == 0) throw new InvalidDataException("No supported geometry intersects the boundary; an empty import is not a readiness result.");
            warnings.Add("All road ribbons are diagnostic centerlines, not pavements, traffic lanes or walkable surfaces.");
            warnings.Add("Building extrusion heights are source tag claims or explicit estimates, never surveyed accuracy or landmark completion.");
            warnings.Add("Terrain, relation geometry, land-use polygons, pedestrian access, layers, turn restrictions, lane placement, and facade research are not implemented.");
            if (manifest.DatasetKind == "synthetic-test-fixture") warnings.Add("SYNTHETIC TEST FIXTURE: not Chelsea geography. Do not ship as a replica or use for accuracy review.");
            var vertexCount = meshes.Sum(m => m.vertices.Length);
            var triangleCount = meshes.Sum(m => m.triangles.Length / 3);
            if (triangleCount > 1_300_000) throw new InvalidDataException("Diagnostic mesh exceeds initial peak 1.3M triangle budget; review coverage before presentation.");
            var result = new
            {
                schemaVersion = 1,
                datasetKind = manifest.DatasetKind,
                source = new { sourceUrl = manifest.SourceUrl, licence = manifest.Licence, snapshotDate = manifest.SnapshotDate, sha256 = sourceHash },
                boundarySha256 = Sha256(boundaryBytes),
                origin = new { longitude = Projection.Origin.Longitude, latitude = Projection.Origin.Latitude },
                coordinates = "WGS84 source; local equirectangular metres; Unity x=east,y=up,z=north; origin fixed; radius<=5km",
                contentHash = Sha256(JsonSerializer.SerializeToUtf8Bytes(meshes)),
                complete = false,
                meshes,
                roads,
                buildings,
                graph = new { nodes = graphNodes.Values.ToArray(), edges, connectedComponents = Components(edges),
                    interpretation = "Undirected source-node topology diagnostic only; grade, access and restrictions are not validated. Coincident coordinates never create a junction." },
                summary = new { importedRoads = roads.Count, importedBuildings = buildings.Count, meshCount = meshes.Count, vertexCount,
                    triangleCount, ignoredNonRoadBuildingWays = ignoredWays, outsideWays, unsupportedRelations = relations.Count },
                warnings = warnings.ToArray()
            };
            var bytes = JsonSerializer.SerializeToUtf8Bytes(result, JsonOptions);
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            string temporary = output + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try { File.WriteAllBytes(temporary, bytes); File.Move(temporary, output, true); }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            Console.WriteLine($"Imported {roads.Count} roads, {buildings.Count} buildings, {triangleCount} triangles. Dataset={manifest.DatasetKind}; completeness=false.");
            return 0;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or IOException or XmlException or JsonException or OverflowException or FormatException or InvalidOperationException or KeyNotFoundException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine("IMPORT FAILED; output not written: " + exception.Message);
            return 1;
        }
    }

    private static Dictionary<string, string> Arguments(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int i = 0; i < args.Length; i += 2)
        {
            if (i + 1 >= args.Length || args[i].Length < 3 || !args[i].StartsWith("--", StringComparison.Ordinal)
                || !new[] { "input", "boundary", "output", "manifest" }.Contains(args[i][2..]) || !result.TryAdd(args[i][2..], args[i + 1]))
                throw new ArgumentException("Usage: --input source.osm --boundary boundary.geojson --manifest source.json --output meshes.json");
        }
        if (result.Count != 4) throw new ArgumentException("All four arguments are required: --input --boundary --manifest --output.");
        return result;
    }

    private static byte[] ReadLimited(string path)
    {
        if (new FileInfo(path).Length > 25_000_000) throw new InvalidDataException("Input exceeds 25 MB first-area limit. Split and review source coverage before importing.");
        return File.ReadAllBytes(path);
    }

    private static string Sha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static Manifest ReadManifest(byte[] bytes, string sourceHash)
    {
        using var document = JsonDocument.Parse(bytes);
        var root = document.RootElement;
        string Field(string name) => root.TryGetProperty(name, out var field) && field.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(field.GetString())
            ? field.GetString()! : throw new InvalidDataException($"Manifest requires nonempty {name}.");
        string kind = Field("datasetKind"), licence = Field("licence"), sourceUrl = Field("sourceUrl"), date = Field("snapshotDate");
        if (kind != "synthetic-test-fixture" && kind != "osm-source") throw new InvalidDataException("Manifest datasetKind must be synthetic-test-fixture or osm-source.");
        if (!string.Equals(sourceHash, Field("sha256"), StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Source SHA-256 does not match manifest. Do not update expected hashes merely to bypass verification.");
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            throw new InvalidDataException("Manifest snapshotDate must be yyyy-MM-dd.");
        if (kind == "osm-source" && (licence != "ODbL-1.0" || !Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https"))
            throw new InvalidDataException("OSM manifests require licence=ODbL-1.0 and an HTTPS source URL; provenance still requires human review.");
        if (kind == "synthetic-test-fixture" && licence != "CC0-1.0") throw new InvalidDataException("Synthetic fixtures require explicit CC0-1.0 dedication.");
        return new Manifest(kind, sourceUrl, licence, date);
    }

    private static Polygon2D ReadBoundary(byte[] bytes)
    {
        using var document = JsonDocument.Parse(bytes);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Boundary root must be a GeoJSON object.");
        if (root.GetProperty("type").GetString() == "FeatureCollection")
        {
            var features = root.GetProperty("features");
            if (features.ValueKind != JsonValueKind.Array || features.GetArrayLength() != 1) throw new InvalidDataException("Boundary must contain exactly one polygon feature.");
            root = features[0];
        }
        if (root.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Boundary feature must be a GeoJSON object.");
        if (root.GetProperty("type").GetString() == "Feature") root = root.GetProperty("geometry");
        if (root.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Boundary geometry must be a GeoJSON object.");
        if (root.GetProperty("type").GetString() != "Polygon") throw new InvalidDataException("Boundary must be a GeoJSON Polygon in WGS84.");
        var rings = root.GetProperty("coordinates");
        if (rings.ValueKind != JsonValueKind.Array || rings.GetArrayLength() != 1) throw new InvalidDataException("Boundary requires one ring; holes are unsupported.");
        if (rings[0].ValueKind != JsonValueKind.Array || rings[0].GetArrayLength() < 4) throw new InvalidDataException("Boundary ring needs at least four positions including closure.");
        var points = rings[0].EnumerateArray().Select(p =>
        {
            if (p.ValueKind != JsonValueKind.Array || p.GetArrayLength() != 2 || p[0].ValueKind != JsonValueKind.Number || p[1].ValueKind != JsonValueKind.Number)
                throw new InvalidDataException("Boundary positions require numeric [longitude, latitude].");
            return Projection.Project(new GeoCoordinate(p[0].GetDouble(), p[1].GetDouble()));
        }).ToArray();
        if (points.Length < 4 || points[0].DistanceTo(points[^1]) > 1e-8) throw new InvalidDataException("GeoJSON boundary ring must be explicitly closed.");
        return new Polygon2D(points.Take(points.Length - 1));
    }

    private static (SortedDictionary<long, GeoCoordinate> nodes, SortedDictionary<long, Way> ways, List<string> relations) ReadOsm(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 25_000_000 });
        var xml = XDocument.Load(reader);
        if (xml.Root?.Name != "osm") throw new InvalidDataException("Expected OSM XML root (not PBF, HTML or an API error).");
        var nodes = new SortedDictionary<long, GeoCoordinate>();
        var ways = new SortedDictionary<long, Way>();
        var relations = new List<string>();
        foreach (var element in xml.Root.Elements())
        {
            if (element.Name == "node")
            {
                long id = PositiveId(element, "id");
                if (!nodes.TryAdd(id, new GeoCoordinate(Number(element, "lon"), Number(element, "lat")))) throw new InvalidDataException($"Duplicate OSM node {id}.");
            }
            else if (element.Name == "way")
            {
                long id = PositiveId(element, "id");
                var tags = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var tag in element.Elements("tag"))
                {
                    string key = Required(tag, "k"), value = Required(tag, "v");
                    if (!tags.TryAdd(key, value)) throw new InvalidDataException($"Duplicate tag {key} on way {id}.");
                }
                if (!ways.TryAdd(id, new Way(id, element.Elements("nd").Select(n => PositiveId(n, "ref")).ToList(), tags)))
                    throw new InvalidDataException($"Duplicate OSM way {id}.");
            }
            else if (element.Name == "relation") relations.Add(SourceIdentity.Osm("relation", PositiveId(element, "id")));
        }
        relations.Sort(StringComparer.Ordinal);
        return (nodes, ways, relations);
    }

    private static string Required(XElement element, string name) => element.Attribute(name)?.Value ?? throw new InvalidDataException($"Missing XML attribute {name}.");
    private static long PositiveId(XElement element, string name)
    {
        if (!long.TryParse(Required(element, name), NumberStyles.None, CultureInfo.InvariantCulture, out long id) || id <= 0)
            throw new InvalidDataException($"OSM {name} must be a positive source ID.");
        return id;
    }
    private static double Number(XElement element, string name) => double.Parse(Required(element, name), NumberStyles.Float, CultureInfo.InvariantCulture);
    private static (double, string) Height(Way way)
    {
        if (way.Tags.TryGetValue("height", out var height)) return (HeightNumber(height, "height"), "source-height-tag; unverified measurement");
        if (way.Tags.TryGetValue("building:levels", out var levels)) return (HeightNumber(levels, "building:levels") * 3, "estimated: source levels multiplied by assumed 3m floor height");
        return (9, "estimated: unsupported 9m diagnostic extrusion; no fidelity claim");
    }
    private static double HeightNumber(string value, string field)
    {
        string normalized = field == "height" && value.EndsWith(" m", StringComparison.Ordinal) ? value[..^2] : value;
        if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || !double.IsFinite(result) || result <= 0 || result > (field == "height" ? 500 : 100))
            throw new InvalidDataException($"Unsupported {field} value '{value}'; supply reviewed metric values, do not guess conversion.");
        return result;
    }

    private static object[] Coordinates(List<long> ids, SortedDictionary<long, GeoCoordinate> nodes) => ids.Select(id => (object)new
        { sourceId = SourceIdentity.Osm("node", id), longitude = nodes[id].Longitude, latitude = nodes[id].Latitude }).ToArray();
    private static object P2(Point2 point) => new { x = point.X, north = point.Y };
    private static MeshOutput Mesh(string id, string kind, int part, MeshData mesh) => new(id, kind, part,
        mesh.Vertices.Select(v => new Vertex(v.X, v.Y, v.Z)).ToArray(), mesh.Indices.ToArray());
    private static int Components(List<GraphEdge> edges)
    {
        var adjacency = new SortedDictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var e in edges)
        {
            if (!adjacency.TryGetValue(e.from, out var a)) adjacency[e.from] = a = new HashSet<string>(StringComparer.Ordinal);
            if (!adjacency.TryGetValue(e.to, out var b)) adjacency[e.to] = b = new HashSet<string>(StringComparer.Ordinal);
            a.Add(e.to); b.Add(e.from);
        }
        var visited = new HashSet<string>(StringComparer.Ordinal); int count = 0;
        foreach (var node in adjacency.Keys)
        {
            if (!visited.Add(node)) continue;
            count++; var queue = new Queue<string>(); queue.Enqueue(node);
            while (queue.TryDequeue(out var current)) foreach (var next in adjacency[current]) if (visited.Add(next)) queue.Enqueue(next);
        }
        return count;
    }

    private sealed record Manifest(string DatasetKind, string SourceUrl, string Licence, string SnapshotDate);
    private sealed record Way(long Id, List<long> NodeIds, Dictionary<string, string> Tags);
    private sealed record Vertex(double x, double y, double z);
    private sealed record MeshOutput(string sourceId, string kind, int part, Vertex[] vertices, int[] triangles);
    private sealed record GraphEdge(string from, string to, string sourceId, double lengthMetres, string oneway, string access);
}
