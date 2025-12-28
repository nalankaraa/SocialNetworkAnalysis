using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SocialNetworkAnalysis.Core.Models
{
    public class Graph
    {
        // Lookup for Node objects by ID
        public Dictionary<int, Node> Nodes { get; private set; } = new Dictionary<int, Node>();

        // Adjacency List: Key = NodeId, Value = List of Edges from this node
        public Dictionary<int, List<Edge>> AdjacencyList { get; private set; } = new Dictionary<int, List<Edge>>();

        public void AddNode(Node node)
        {
            if (!Nodes.ContainsKey(node.Id))
            {
                Nodes[node.Id] = node;
                AdjacencyList[node.Id] = new List<Edge>();
            }
        }

        public void AddEdge(int sourceId, int targetId, double weight)
        {
            if (!Nodes.ContainsKey(sourceId) || !Nodes.ContainsKey(targetId))
                return; // Or throw exception

            // Add forward edge
            var edgeForward = new Edge { SourceId = sourceId, TargetId = targetId, Weight = weight };
            AdjacencyList[sourceId].Add(edgeForward);

            // Add backward edge (Undirected graph)
            // Check if it already exists to avoid duplicates if iterating both ways?
            // Usually for weighted undirected, we add both.
            var edgeBackward = new Edge { SourceId = targetId, TargetId = sourceId, Weight = weight };
            AdjacencyList[targetId].Add(edgeBackward);
        }

        public void Clear()
        {
            Nodes.Clear();
            AdjacencyList.Clear();
        }
        public int EdgeCount
        {
            get
            {
                return AdjacencyList.Values.Sum(edges => edges.Count) / 2;
            }
        }

    }
}
