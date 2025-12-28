using SocialNetworkAnalysis.Algorithms.Base;
using SocialNetworkAnalysis.Core.Enums;
using SocialNetworkAnalysis.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialNetworkAnalysis.Algorithms.Coloring
{
    public class ColoringResult
    {
        public Dictionary<int, int> NodeColors { get; set; } = new();
    }



    public class WelshPowellColoring : AlgorithmBase
    {
        public override AlgorithmType Type => AlgorithmType.WelshPowell;

        public ColoringResult Result { get; private set; } = new();

        protected override void Execute(Graph graph)
        {
            var sortedNodes = graph.AdjacencyList
                .OrderByDescending(n => n.Value.Count)
                .Select(n => n.Key)
                .ToList();

            int currentColor = 1;

            // Loop until all nodes are colored
            while (sortedNodes.Any(n => !Result.NodeColors.ContainsKey(n)))
            {
                var nodesInThisColor = new List<int>();

                foreach (var node in sortedNodes)
                {
                    if (Result.NodeColors.ContainsKey(node))
                        continue;

                    // Check if 'node' connects to any node already in this color group
                    bool isAdjacent = false;
                    foreach (var groupNode in nodesInThisColor)
                    {
                        // Check adjacency in both directions or rely on undirected graph property in AdjacencyList
                        // Since we ensure undirected edges in Graph.AddEdge, checking one way is enough if the list is complete
                        if (graph.AdjacencyList[groupNode].Any(e => e.TargetId == node))
                        {
                            isAdjacent = true;
                            break;
                        }
                    }

                    if (!isAdjacent)
                    {
                        Result.NodeColors[node] = currentColor;
                        nodesInThisColor.Add(node);
                    }
                }

                currentColor++;
            }
        }
    }
}
