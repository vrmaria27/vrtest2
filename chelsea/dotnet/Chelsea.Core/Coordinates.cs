using System;
using System.Globalization;

namespace Chelsea.Core
{
    public readonly struct GeoCoordinate
    {
        public double Longitude { get; }
        public double Latitude { get; }
        public GeoCoordinate(double longitude, double latitude)
        {
            if (!Numbers.Finite(longitude) || longitude < -180 || longitude > 180) throw new ArgumentOutOfRangeException(nameof(longitude));
            if (!Numbers.Finite(latitude) || latitude < -90 || latitude > 90) throw new ArgumentOutOfRangeException(nameof(latitude));
            Longitude = longitude; Latitude = latitude;
        }
    }

    public readonly struct Point2 : IEquatable<Point2>
    {
        public double X { get; }
        public double Y { get; }
        public Point2(double x, double y)
        {
            if (!Numbers.Finite(x) || !Numbers.Finite(y)) throw new ArgumentException("Coordinates must be finite.");
            X = x; Y = y;
        }
        public double DistanceTo(Point2 other) { var dx = X - other.X; var dy = Y - other.Y; return Math.Sqrt(dx * dx + dy * dy); }
        public bool Equals(Point2 other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object? obj) => obj is Point2 other && Equals(other);
        public override int GetHashCode() => X.GetHashCode() * 397 ^ Y.GetHashCode();
        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "({0:R}, {1:R})", X, Y);
    }

    public readonly struct Point3
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        public Point3(double x, double y, double z)
        {
            if (!Numbers.Finite(x) || !Numbers.Finite(y) || !Numbers.Finite(z)) throw new ArgumentException("Coordinates must be finite.");
            X = x; Y = y; Z = z;
        }
    }

    /// <summary>Local equirectangular presentation projection; X east, Y north, metres.
    /// Radius 6378137 m. Not OSGB36, a survey projection or ground-truth accuracy claim.
    /// Importers must limit coverage to 5 km from the fixed origin.</summary>
    public sealed class LocalProjection
    {
        public const double EarthRadiusMetres = 6378137;
        public static readonly GeoCoordinate ChelseaOrigin = new GeoCoordinate(-0.1589, 51.4905);
        public GeoCoordinate Origin { get; }
        private readonly double longitudeScale;
        private const double Radians = Math.PI / 180;
        public LocalProjection(GeoCoordinate origin)
        {
            if (Math.Abs(origin.Latitude) > 85) throw new ArgumentException("Local projection is unsupported near poles.", nameof(origin));
            Origin = origin;
            longitudeScale = EarthRadiusMetres * Radians * Math.Cos(origin.Latitude * Radians);
        }
        public Point2 Project(GeoCoordinate coordinate)
        {
            var p = new Point2((coordinate.Longitude - Origin.Longitude) * longitudeScale,
                (coordinate.Latitude - Origin.Latitude) * EarthRadiusMetres * Radians);
            if (p.DistanceTo(new Point2(0, 0)) > 5000) throw new ArgumentOutOfRangeException(nameof(coordinate), "Coordinate exceeds the documented 5 km projection domain.");
            return p;
        }
        public GeoCoordinate Unproject(Point2 point)
        {
            if (point.DistanceTo(new Point2(0, 0)) > 5000) throw new ArgumentOutOfRangeException(nameof(point));
            return new GeoCoordinate(Origin.Longitude + point.X / longitudeScale,
                Origin.Latitude + point.Y / (EarthRadiusMetres * Radians));
        }
    }

    public readonly struct ChunkKey : IEquatable<ChunkKey>
    {
        public int X { get; }
        public int Y { get; }
        public ChunkKey(int x, int y) { X = x; Y = y; }
        public static ChunkKey FromPoint(Point2 point, double chunkSizeMetres = 100)
        {
            if (!Numbers.Finite(chunkSizeMetres) || chunkSizeMetres <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSizeMetres));
            return new ChunkKey(checked((int)Math.Floor(point.X / chunkSizeMetres)), checked((int)Math.Floor(point.Y / chunkSizeMetres)));
        }
        public bool Equals(ChunkKey other) => X == other.X && Y == other.Y;
        public override bool Equals(object? obj) => obj is ChunkKey other && Equals(other);
        public override int GetHashCode() => X * 397 ^ Y;
        public override string ToString() => X.ToString(CultureInfo.InvariantCulture) + ":" + Y.ToString(CultureInfo.InvariantCulture);
    }

    public static class SourceIdentity
    {
        public static string Osm(string elementType, long id)
        {
            if (elementType != "node" && elementType != "way" && elementType != "relation") throw new ArgumentException("OSM element type must be node, way or relation.", nameof(elementType));
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            return "osm:" + elementType + ":" + id.ToString(CultureInfo.InvariantCulture);
        }
    }

    internal static class Numbers
    {
        internal static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
