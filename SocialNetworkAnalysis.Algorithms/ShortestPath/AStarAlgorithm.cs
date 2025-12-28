using SocialNetworkAnalysis.Algorithms.Base;
using SocialNetworkAnalysis.Core.Enums;
using SocialNetworkAnalysis.Core.Interfaces;
using SocialNetworkAnalysis.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialNetworkAnalysis.Algorithms.ShortestPath
{
    public class AStarAlgorithm : AlgorithmBase
    {
        private readonly IHeuristicProvider _heuristic;

        public AStarAlgorithm(IHeuristicProvider heuristic)
        {
            _heuristic = heuristic;
        }

        public override AlgorithmType Type => AlgorithmType.AStar;

        public int? StartNodeId { get; set; }
        public int? EndNodeId { get; set; }
        public List<int> Path { get; private set; } = new();
        public Dictionary<int, double> Distances { get; private set; } = new(); // gScore
        public Dictionary<int, int> Previous { get; private set; } = new();

        protected override void Execute(Graph graph)
        {
            Distances.Clear();
            Previous.Clear();
            Path.Clear();

            if (!StartNodeId.HasValue || !EndNodeId.HasValue)
                return; // A* requires goal

            int startNode = StartNodeId.Value;
            int endNode = EndNodeId.Value;

            if (!graph.Nodes.ContainsKey(startNode) || !graph.Nodes.ContainsKey(endNode))
                return;

            // Initialize gScore
            foreach (var node in graph.Nodes.Keys)
            {
                Distances[node] = double.PositiveInfinity;
            }
            Distances[startNode] = 0;

            var pq = new PriorityQueue<int, double>();
            // Initial priority is fScore = g(start) + h(start, end) = 0 + h
            double hStart = _heuristic.Estimate(graph.Nodes[startNode], graph.Nodes[endNode]);
            pq.Enqueue(startNode, hStart);

            while (pq.Count > 0)
            {
                if (pq.TryDequeue(out int current, out double fScore))
                {
                    if (current == endNode)
                    {
                        break; // Reconstruct path
                    }

                    // Strict check: if this path to current is worse than what we found already?
                    // fScore is g + h. Distances is g.
                    // We can check if Distances[current] is somewhat compatible with fScore?
                    // Easier: just expand. If we found a shorter g, we updated Distances.

                    if (graph.AdjacencyList.ContainsKey(current))
                    {
                        foreach (var edge in graph.AdjacencyList[current])
                        {
                            double tentative_g = Distances[current] + edge.Weight;
                            if (tentative_g < Distances[edge.TargetId])
                            {
                                Distances[edge.TargetId] = tentative_g;
                                Previous[edge.TargetId] = current;

                                double h = _heuristic.Estimate(graph.Nodes[edge.TargetId], graph.Nodes[endNode]);
                                double f = tentative_g + h;

                                pq.Enqueue(edge.TargetId, f);
                            }
                        }
                    }
                }
            }

            // Reconstruct
            if (Distances[endNode] != double.PositiveInfinity)
            {
                var curr = endNode;
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
