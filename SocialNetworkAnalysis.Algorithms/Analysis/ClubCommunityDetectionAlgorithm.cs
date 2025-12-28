using SocialNetworkAnalysis.Algorithms.Base;
using SocialNetworkAnalysis.Analysis.Results;
using SocialNetworkAnalysis.Core.Enums;
using SocialNetworkAnalysis.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SocialNetworkAnalysis.Algorithms.Analysis
{
    public class ClubCommunityDetectionAlgorithm : AnalysisAlgorithmBase
    {
        public override AnalysisAlgorithmType AnalysisType => AnalysisAlgorithmType.ClubCommunityDetection;

        // Custom Results list needed since Base doesn't provide it
        public List<AlgorithmStepResult> Results { get; private set; } = new List<AlgorithmStepResult>();

        // Map for UI Visualization
        public Dictionary<int, int> NodeCommunityMap { get; private set; } = new Dictionary<int, int>();

        protected override void Execute(Graph graph)
        {
            if (graph == null) return;
            Results.Clear();
            NodeCommunityMap.Clear();

            var visited = new HashSet<int>();
            int communityId = 0;

            var sortedNodes = graph.Nodes.Values.OrderBy(n => n.Id).ToList();

            foreach (var node in sortedNodes)
            {
                if (!visited.Contains(node.Id))
                {
                    communityId++;
                    var communityNodes = new List<Node>();
                    var communityEdges = new HashSet<string>();

                    string clubType = node.Club;
                    string safeClubName = string.IsNullOrWhiteSpace(clubType) ? "(No Club)" : clubType;

                    var queue = new Queue<Node>();
                    queue.Enqueue(node);
                    visited.Add(node.Id);
                    NodeCommunityMap[node.Id] = communityId;

                    while (queue.Count > 0)
                    {
                        var current = queue.Dequeue();
                        communityNodes.Add(current);

                        if (graph.AdjacencyList.ContainsKey(current.Id))
                        {
                            foreach (var edge in graph.AdjacencyList[current.Id])
                            {
                                var neighbor = graph.Nodes[edge.TargetId];

                                // CRITICAL: Only traverse if Clubs match
                                if (neighbor.Club == current.Club)
                                {
                                    string edgeKey = current.Id < neighbor.Id ? $"{current.Id}-{neighbor.Id}" : $"{neighbor.Id}-{current.Id}";
                                    communityEdges.Add(edgeKey);

                                    if (!visited.Contains(neighbor.Id))
                                    {
                                        visited.Add(neighbor.Id);
                                        NodeCommunityMap[neighbor.Id] = communityId;
                                        queue.Enqueue(neighbor);
                                    }
                                }
                            }
                        }
                    }

                    // Add to Results
                    Results.Add(new AlgorithmStepResult
                    {
                        StepNumber = communityId,
                        NodeId = communityId,
                        NodeName = $"Community #{communityId}",
                        Description = $"{communityNodes.Count} Nodes, {communityEdges.Count} Edges (Type: {safeClubName})"
                    });
                }
            }
        }
    }
}
