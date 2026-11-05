using System;
using UnityEngine;

namespace Chelsea.World
{
    // Serialized import format, not a second source of geographic truth.
    // World axes: +X east, +Y elevation, +Z north. All values are metres.
    [Serializable]
    public sealed class GeographicDocument
    {
        public int schemaVersion;
        public string datasetKind;
        public GeographicOrigin origin;
        public GeographicMesh[] meshes;
        public string[] warnings;
    }

    [Serializable]
    public sealed class GeographicOrigin
    {
        public double longitude;
        public double latitude;
    }

    [Serializable]
    public sealed class GeographicMesh
    {
        public string sourceId;
        public string kind;
        public Vector3[] vertices;
        public int[] triangles;
    }
}
