using System;
using System.Collections.Generic;
using System.Linq;

namespace Chelsea.Core
{
    public sealed class MeshData
    {
        public IReadOnlyList<Point3> Vertices { get; }
        public IReadOnlyList<int> Indices { get; }
        public int TriangleCount => Indices.Count / 3;
        internal MeshData(List<Point3> vertices, List<int> indices) { Vertices = vertices.AsReadOnly(); Indices = indices.AsReadOnly(); }
    }

    public static class GeometryMesh
    {
        /// <summary>Ear-clipped top, bottom and walls. No holes, roofs or landmark-fidelity claim.</summary>
        public static MeshData ExtrudeFootprint(Polygon2D footprint, double heightMetres)
        {
            if (footprint == null) throw new ArgumentNullException(nameof(footprint));
            if (!Numbers.Finite(heightMetres) || heightMetres <= 0) throw new ArgumentOutOfRangeException(nameof(heightMetres));
            var ring = footprint.Vertices;
            var vertices = new List<Point3>(); var indices = new List<int>();
            foreach (var p in ring) vertices.Add(new Point3(p.X, 0, p.Y));
            foreach (var p in ring) vertices.Add(new Point3(p.X, heightMetres, p.Y));
            var n = ring.Count;
            var remaining = Enumerable.Range(0, n).ToList();
            // Remove straight-through vertices only from the triangulation, retaining original walls/source vertices.
            bool removed;
            do
            {
                removed = false;
                for (int i = 0; i < remaining.Count && remaining.Count > 3; i++)
                {
                    if (Math.Abs(Polygon2D.Cross(ring[remaining[(i + remaining.Count - 1) % remaining.Count]], ring[remaining[i]], ring[remaining[(i + 1) % remaining.Count]])) < Polygon2D.Epsilon)
                    { remaining.RemoveAt(i); removed = true; break; }
                }
            } while (removed);
            while (remaining.Count > 2)
            {
                bool found = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    var a = remaining[(i + remaining.Count - 1) % remaining.Count]; var b = remaining[i]; var c = remaining[(i + 1) % remaining.Count];
                    if (Polygon2D.Cross(ring[a], ring[b], ring[c]) <= Polygon2D.Epsilon) continue;
                    if (remaining.Any(v => v != a && v != b && v != c && InsideTriangle(ring[v], ring[a], ring[b], ring[c]))) continue;
                    indices.Add(a + n); indices.Add(c + n); indices.Add(b + n); // Unity X/Z plane: reverse CCW for upward normal.
                    indices.Add(a); indices.Add(b); indices.Add(c);
                    remaining.RemoveAt(i); found = true; break;
                }
                if (!found) throw new ArgumentException("Footprint could not be triangulated reliably.", nameof(footprint));
            }
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                indices.Add(i); indices.Add(i + n); indices.Add(j + n);
                indices.Add(i); indices.Add(j + n); indices.Add(j);
            }
            return new MeshData(vertices, indices);
        }
        private static bool InsideTriangle(Point2 p, Point2 a, Point2 b, Point2 c) =>
            Polygon2D.Cross(a, b, p) >= -Polygon2D.Epsilon && Polygon2D.Cross(b, c, p) >= -Polygon2D.Epsilon && Polygon2D.Cross(c, a, p) >= -Polygon2D.Epsilon;

        /// <summary>Display ribbon around a source centreline. Width must be supplied explicitly; not a surveyed carriageway or walkable pavement.</summary>
        public static MeshData CreateRibbon(IReadOnlyList<Point2> points, double widthMetres)
        {
            if (points == null || points.Count < 2) throw new ArgumentException("A ribbon needs at least two points.", nameof(points));
            if (!Numbers.Finite(widthMetres) || widthMetres <= 0) throw new ArgumentOutOfRangeException(nameof(widthMetres));
            var vertices = new List<Point3>(); var indices = new List<int>();
            // Segment-local quads deliberately avoid moving centreline vertices or fabricating junction geometry.
            for (var i = 0; i < points.Count - 1; i++)
            {
                var a = points[i]; var b = points[i + 1]; var length = a.DistanceTo(b);
                if (length < Polygon2D.Epsilon) throw new ArgumentException("Ribbon has a zero-length segment.", nameof(points));
                var nx = -(b.Y - a.Y) / length * widthMetres / 2; var ny = (b.X - a.X) / length * widthMetres / 2;
                var offset = vertices.Count;
                vertices.Add(new Point3(a.X + nx, 0, a.Y + ny)); vertices.Add(new Point3(a.X - nx, 0, a.Y - ny));
                vertices.Add(new Point3(b.X + nx, 0, b.Y + ny)); vertices.Add(new Point3(b.X - nx, 0, b.Y - ny));
                indices.Add(offset); indices.Add(offset + 2); indices.Add(offset + 1);
                indices.Add(offset + 1); indices.Add(offset + 2); indices.Add(offset + 3);
            }
            return new MeshData(vertices, indices);
        }
    }
}
