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
    public class BFSAlgorithm : AlgorithmBase
    {
        public override AlgorithmType Type => AlgorithmType.BFS;

        public List<int> VisitedNodes { get; private set; } = new();

        public int? StartNodeId { get; set; }

        protected override void Execute(Graph graph)
        {
            if (!graph.AdjacencyList.Any())
                return;

            // Clear previous results
            VisitedNodes.Clear();

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
            var queue = new Queue<int>();

            queue.Enqueue(startNode);
            visited.Add(startNode);

            while (queue.Any())
            {
                var current = queue.Dequeue();
                VisitedNodes.Add(current);

                foreach (var edge in graph.AdjacencyList[current])
                {
                    if (!visited.Contains(edge.TargetId))
                    {
                        visited.Add(edge.TargetId);
                        queue.Enqueue(edge.TargetId);
                    }
                }
            }
        }
    }
}
