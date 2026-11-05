using System;
using System.Collections.Generic;
using System.Linq;

namespace Chelsea.Core
{
    public sealed class RoadNode
    {
        public string SourceId { get; }
        public Point2 Position { get; }
        public RoadNode(string sourceId, Point2 position) { SourceId = RequireId(sourceId); Position = position; }
        internal static string RequireId(string id) => string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("A stable source identifier is required.", nameof(id)) : id;
    }

    public sealed class RoadEdge
    {
        public string SourceId { get; }
        public string FromNodeId { get; }
        public string ToNodeId { get; }
        public double LengthMetres { get; }
        public bool OneWay { get; }
        public RoadEdge(string sourceId, string fromNodeId, string toNodeId, double lengthMetres, bool oneWay = false)
        {
            SourceId = RoadNode.RequireId(sourceId); FromNodeId = RoadNode.RequireId(fromNodeId); ToNodeId = RoadNode.RequireId(toNodeId);
            if (!Numbers.Finite(lengthMetres) || lengthMetres <= 0) throw new ArgumentOutOfRangeException(nameof(lengthMetres));
            if (fromNodeId == toNodeId) throw new ArgumentException("Zero-node loops must be split at source nodes.");
            LengthMetres = lengthMetres; OneWay = oneWay;
        }
    }

    public sealed class RoadPath
    {
        public IReadOnlyList<string> NodeIds { get; }
        public IReadOnlyList<string> EdgeIds { get; }
        public double LengthMetres { get; }
        internal RoadPath(List<string> nodes, List<string> edges, double length) { NodeIds = nodes.AsReadOnly(); EdgeIds = edges.AsReadOnly(); LengthMetres = length; }
    }

    /// <summary>Topology uses source node identities, never coordinate proximity.
    /// This is a connectivity diagnostic, not a verified pedestrian/traffic router.
    /// Turn restrictions, public access, lane geometry and crossings require later evidence.</summary>
    public sealed class RoadGraph
    {
        public IReadOnlyDictionary<string, RoadNode> Nodes { get; }
        public IReadOnlyList<RoadEdge> Edges { get; }
        private readonly Dictionary<string, List<(string Node, RoadEdge Edge)>> adjacency;
        public RoadGraph(IEnumerable<RoadNode> nodes, IEnumerable<RoadEdge> edges)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            if (edges == null) throw new ArgumentNullException(nameof(edges));
            var nodeMap = new SortedDictionary<string, RoadNode>(StringComparer.Ordinal);
            foreach (var node in nodes)
            {
                if (node == null) throw new ArgumentException("Null node.", nameof(nodes));
                if (nodeMap.ContainsKey(node.SourceId)) throw new ArgumentException("Duplicate node ID: " + node.SourceId, nameof(nodes));
                nodeMap.Add(node.SourceId, node);
            }
            var edgeList = edges.OrderBy(e => e?.SourceId, StringComparer.Ordinal).ToList();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            adjacency = nodeMap.Keys.ToDictionary(id => id, _ => new List<(string, RoadEdge)>(), StringComparer.Ordinal);
            foreach (var edge in edgeList)
            {
                if (edge == null) throw new ArgumentException("Null edge.", nameof(edges));
                if (!ids.Add(edge.SourceId)) throw new ArgumentException("Duplicate edge ID: " + edge.SourceId, nameof(edges));
                if (!nodeMap.ContainsKey(edge.FromNodeId) || !nodeMap.ContainsKey(edge.ToNodeId)) throw new ArgumentException("Edge references a missing source node: " + edge.SourceId, nameof(edges));
                adjacency[edge.FromNodeId].Add((edge.ToNodeId, edge));
                if (!edge.OneWay) adjacency[edge.ToNodeId].Add((edge.FromNodeId, edge));
            }
            Nodes = new System.Collections.ObjectModel.ReadOnlyDictionary<string, RoadNode>(nodeMap);
            Edges = edgeList.AsReadOnly();
        }

        /// <summary>Weak components, including isolated nodes. One-way streets do not split a weak component.</summary>
        public IReadOnlyList<IReadOnlyList<string>> ConnectedComponents()
        {
            var neighbours = Nodes.Keys.ToDictionary(id => id, _ => new List<string>(), StringComparer.Ordinal);
            foreach (var edge in Edges) { neighbours[edge.FromNodeId].Add(edge.ToNodeId); neighbours[edge.ToNodeId].Add(edge.FromNodeId); }
            var visited = new HashSet<string>(StringComparer.Ordinal); var result = new List<IReadOnlyList<string>>();
            foreach (var root in Nodes.Keys)
            {
                if (!visited.Add(root)) continue;
                var component = new List<string>(); var pending = new Queue<string>(); pending.Enqueue(root);
                while (pending.Count > 0)
                {
                    var node = pending.Dequeue(); component.Add(node);
                    foreach (var next in neighbours[node]) if (visited.Add(next)) pending.Enqueue(next);
                }
                component.Sort(StringComparer.Ordinal); result.Add(component.AsReadOnly());
            }
            return result.AsReadOnly();
        }

        /// <summary>Deterministic Dijkstra with explicit direction. Null means no path.
        /// Intended for small import validation graphs, not per-frame simulation.</summary>
        public RoadPath? ShortestPath(string fromNodeId, string toNodeId)
        {
            if (!Nodes.ContainsKey(fromNodeId) || !Nodes.ContainsKey(toNodeId)) throw new ArgumentException("Unknown source node.");
            var distances = Nodes.Keys.ToDictionary(id => id, _ => double.PositiveInfinity, StringComparer.Ordinal);
            var previous = new Dictionary<string, (string Node, string Edge)>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            distances[fromNodeId] = 0;
            while (true)
            {
                string? current = null; var best = double.PositiveInfinity;
                foreach (var id in Nodes.Keys) if (!visited.Contains(id) && distances[id] < best) { current = id; best = distances[id]; }
                if (current == null) return null;
                if (current == toNodeId)
                {
                    var pathNodes = new List<string> { current }; var pathEdges = new List<string>();
                    while (current != fromNodeId)
                    {
                        var p = previous[current]; pathEdges.Add(p.Edge); current = p.Node; pathNodes.Add(current);
                    }
                    pathNodes.Reverse(); pathEdges.Reverse(); return new RoadPath(pathNodes, pathEdges, best);
                }
                visited.Add(current);
                foreach (var item in adjacency[current])
                {
                    var candidate = best + item.Edge.LengthMetres;
                    if (candidate < distances[item.Node]) { distances[item.Node] = candidate; previous[item.Node] = (current, item.Edge.SourceId); }
                }
            }
        }
    }
}
