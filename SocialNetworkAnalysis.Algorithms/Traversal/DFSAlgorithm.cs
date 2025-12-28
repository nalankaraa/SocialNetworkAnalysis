using SocialNetworkAnalysis.Algorithms.Base;
using SocialNetworkAnalysis.Core.Enums;
using SocialNetworkAnalysis.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialNetworkAnalysis.Algorithms.Traversal
{
    public class DFSAlgorithm : AlgorithmBase
    {
        public override AlgorithmType Type => AlgorithmType.DFS;

        public List<int> VisitedNodes { get; private set; } = new();

        public int? StartNodeId { get; set; }

        protected override void Execute(Graph graph)
        {
            if (!graph.AdjacencyList.Any())
                return;

            int startNode;
            if (StartNodeId.HasValue && graph.AdjacencyList.ContainsKey(StartNodeId.Value))
            {
                startNode = StartNodeId.Value;
            }
            else
            {
                startNode = graph.AdjacencyList.Keys.First();
            }

            var visited = new HashSet<int>();

            DFS(startNode, graph, visited);
        }

        private void DFS(int nodeId, Graph graph, HashSet<int> visited)
        {
            visited.Add(nodeId);
            VisitedNodes.Add(nodeId);

            foreach (var edge in graph.AdjacencyList[nodeId])
            {
                if (!visited.Contains(edge.TargetId))
                    DFS(edge.TargetId, graph, visited);
            }
        }
    }
}
