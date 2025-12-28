using SocialNetworkAnalysis.Algorithms.Base;
using SocialNetworkAnalysis.Core.Enums;
using SocialNetworkAnalysis.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialNetworkAnalysis.Algorithms.ShortestPath
{
    public class DijkstraAlgorithm : AlgorithmBase
    {
        public override AlgorithmType Type => AlgorithmType.Dijkstra;

        public Dictionary<int, double> Distances { get; private set; } = new();
        public int? StartNodeId { get; set; }
        public int? EndNodeId { get; set; }
        public List<int> Path { get; private set; } = new();
        public Dictionary<int, int> Previous { get; private set; } = new();

        protected override void Execute(Graph graph)
        {
            Distances.Clear();
            Previous.Clear();
            Path.Clear();

            var nodes = graph.AdjacencyList.Keys;
            foreach (var node in nodes)
            {
                Distances[node] = double.PositiveInfinity;
            }

            int startNode;
            if (StartNodeId.HasValue && graph.AdjacencyList.ContainsKey(StartNodeId.Value))
                startNode = StartNodeId.Value;
            else if (nodes.Any())
                startNode = nodes.First();
            else
                return;

            Distances[startNode] = 0;

            var pq = new PriorityQueue<int, double>();
            pq.Enqueue(startNode, 0);

            while (pq.Count > 0)
            {
                // Note: PriorityQueue in .NET 6 doesn't support Remove, so we might process same node multiple times if we re-queued it with better priority.
                // Standard lazy Dijkstra: check if popped distance > current shortest.
                if (pq.TryDequeue(out int current, out double priority))
                {
                    if (priority > Distances[current]) continue;

                    if (EndNodeId.HasValue && current == EndNodeId.Value)
                    {
                        // Found target, can break early if we only care about this path
                        // But if we want full map, continue.
                        // Optimization: break if only path needed.
                        break;
                    }

                    if (graph.AdjacencyList.ContainsKey(current))
                    {
                        foreach (var edge in graph.AdjacencyList[current])
                        {
                            var newDist = Distances[current] + edge.Weight;

                            if (newDist < Distances[edge.TargetId])
                            {
                                Distances[edge.TargetId] = newDist;
                                Previous[edge.TargetId] = current;
                                pq.Enqueue(edge.TargetId, newDist);
                            }
                        }
                    }
                }
            }

            // Reconstruct path if EndNodeId is present and reachable
            if (EndNodeId.HasValue && Distances[EndNodeId.Value] != double.PositiveInfinity)
            {
                var curr = EndNodeId.Value;
                Path.Add(curr);
                while (Previous.ContainsKey(curr))
                {
                    curr = Previous[curr];
                    Path.Add(curr);
                }
                Path.Reverse();
            }
        }
    }
}
