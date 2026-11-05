using System;
using System.Collections.Generic;
using System.Linq;

namespace Chelsea.Core
{
    public readonly struct Segment2
    {
        public Point2 Start { get; }
        public Point2 End { get; }
        public Segment2(Point2 start, Point2 end) { Start = start; End = end; }
        public double Length => Start.DistanceTo(End);
    }

    /// <summary>One simple ring, with no holes. Closed GeoJSON rings are accepted.
    /// Winding is normalized to counterclockwise. Unsupported rings fail explicitly.</summary>
    public sealed class Polygon2D
    {
        internal const double Epsilon = 1e-8;
        public IReadOnlyList<Point2> Vertices { get; }
        public double Area { get; }
        public Polygon2D(IEnumerable<Point2> vertices)
        {
            if (vertices == null) throw new ArgumentNullException(nameof(vertices));
            var ring = vertices.ToList();
            if (ring.Count > 1 && ring[0].Equals(ring[ring.Count - 1])) ring.RemoveAt(ring.Count - 1);
            if (ring.Count < 3) throw new ArgumentException("A polygon needs at least three distinct vertices.", nameof(vertices));
            for (var i = 0; i < ring.Count; i++)
            {
                if (ring[i].DistanceTo(ring[(i + 1) % ring.Count]) < Epsilon) throw new ArgumentException("Polygon contains a zero-length edge.", nameof(vertices));
                for (var j = i + 1; j < ring.Count; j++)
                {
                    if (j == i + 1 || (i == 0 && j == ring.Count - 1)) continue;
                    if (Intersects(ring[i], ring[(i + 1) % ring.Count], ring[j], ring[(j + 1) % ring.Count]))
                        throw new ArgumentException("Polygon self-intersects or touches itself.", nameof(vertices));
                }
                // Adjacent collinear reversal overlaps even though adjacent edges share a vertex.
                var previous = ring[(i + ring.Count - 1) % ring.Count];
                var current = ring[i]; var next = ring[(i + 1) % ring.Count];
                if (Math.Abs(Cross(previous, current, next)) < Epsilon &&
                    (previous.X - current.X) * (next.X - current.X) + (previous.Y - current.Y) * (next.Y - current.Y) > Epsilon)
                    throw new ArgumentException("Polygon has overlapping adjacent edges.", nameof(vertices));
            }
            double twiceArea = 0;
            for (var i = 0; i < ring.Count; i++) twiceArea += ring[i].X * ring[(i + 1) % ring.Count].Y - ring[(i + 1) % ring.Count].X * ring[i].Y;
            if (Math.Abs(twiceArea) < Epsilon) throw new ArgumentException("Polygon area must be positive.", nameof(vertices));
            if (twiceArea < 0) ring.Reverse();
            Area = Math.Abs(twiceArea) / 2;
            Vertices = ring.AsReadOnly();
        }

        public bool Contains(Point2 point)
        {
            bool inside = false;
            for (var i = 0; i < Vertices.Count; i++)
            {
                var a = Vertices[i]; var b = Vertices[(i + 1) % Vertices.Count];
                if (OnSegment(a, b, point)) return true;
                if ((a.Y > point.Y) != (b.Y > point.Y) && point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
            }
            return inside;
        }

        /// <summary>Returns every portion inside this ring; supports concave rings and boundary-collinear segments.
        /// Clipping creates display endpoints only; never use them as invented road junctions.</summary>
        public IReadOnlyList<Segment2> ClipSegment(Point2 start, Point2 end)
        {
            var length = start.DistanceTo(end);
            if (length < Epsilon) return Array.Empty<Segment2>();
            var dx = end.X - start.X; var dy = end.Y - start.Y;
            var parameters = new List<double> { 0, 1 };
            for (var i = 0; i < Vertices.Count; i++)
            {
                var a = Vertices[i]; var b = Vertices[(i + 1) % Vertices.Count];
                var ex = b.X - a.X; var ey = b.Y - a.Y;
                var ax = a.X - start.X; var ay = a.Y - start.Y;
                var denominator = dx * ey - dy * ex;
                if (Math.Abs(denominator) > Epsilon)
                {
                    var t = (ax * ey - ay * ex) / denominator;
                    var u = (ax * dy - ay * dx) / denominator;
                    if (t >= -Epsilon && t <= 1 + Epsilon && u >= -Epsilon && u <= 1 + Epsilon) parameters.Add(Math.Max(0, Math.Min(1, t)));
                }
                else if (Math.Abs(ax * dy - ay * dx) < Epsilon)
                {
                    var lengthSquared = dx * dx + dy * dy;
                    foreach (var v in new[] { a, b })
                    {
                        var t = ((v.X - start.X) * dx + (v.Y - start.Y) * dy) / lengthSquared;
                        if (t > 0 && t < 1) parameters.Add(t);
                    }
                }
            }
            parameters.Sort();
            var pieces = new List<Segment2>();
            for (var i = 0; i < parameters.Count - 1; i++)
            {
                var t0 = parameters[i]; var t1 = parameters[i + 1];
                if ((t1 - t0) * length < Epsilon) continue;
                var middle = (t0 + t1) / 2;
                if (!Contains(new Point2(start.X + dx * middle, start.Y + dy * middle))) continue;
                var a = new Point2(start.X + dx * t0, start.Y + dy * t0);
                var b = new Point2(start.X + dx * t1, start.Y + dy * t1);
                if (pieces.Count > 0 && pieces[pieces.Count - 1].End.DistanceTo(a) < Epsilon)
                    pieces[pieces.Count - 1] = new Segment2(pieces[pieces.Count - 1].Start, b);
                else pieces.Add(new Segment2(a, b));
            }
            return pieces.AsReadOnly();
        }

        internal static double Cross(Point2 a, Point2 b, Point2 c) => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        internal static bool OnSegment(Point2 a, Point2 b, Point2 p) => Math.Abs(Cross(a, b, p)) < Epsilon &&
            p.X >= Math.Min(a.X, b.X) - Epsilon && p.X <= Math.Max(a.X, b.X) + Epsilon &&
            p.Y >= Math.Min(a.Y, b.Y) - Epsilon && p.Y <= Math.Max(a.Y, b.Y) + Epsilon;
        private static bool Intersects(Point2 a, Point2 b, Point2 c, Point2 d)
        {
            var c1 = Cross(a, b, c); var c2 = Cross(a, b, d); var c3 = Cross(c, d, a); var c4 = Cross(c, d, b);
            return ((c1 > Epsilon && c2 < -Epsilon || c1 < -Epsilon && c2 > Epsilon) &&
                    (c3 > Epsilon && c4 < -Epsilon || c3 < -Epsilon && c4 > Epsilon)) ||
                   OnSegment(a, b, c) || OnSegment(a, b, d) || OnSegment(c, d, a) || OnSegment(c, d, b);
        }
    }
}
