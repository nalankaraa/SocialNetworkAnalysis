using SocialNetworkAnalysis.Algorithms.Base;
using SocialNetworkAnalysis.Core.Enums;
using SocialNetworkAnalysis.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialNetworkAnalysis.Algorithms.Analysis
{
    public class ConnectedComponentsAlgorithm : AnalysisAlgorithmBase
    {
        public override AnalysisAlgorithmType AnalysisType => AnalysisAlgorithmType.ConnectedComponents;

        public List<List<int>> Components { get; private set; } = new();

        protected override void Execute(Graph graph)
        {
            var visited = new HashSet<int>();

            foreach (var node in graph.AdjacencyList.Keys)
            {
                if (visited.Contains(node))
                    continue;

                var component = new List<int>();
                DFS(node, graph, visited, component);
                Components.Add(component);
            }
        }

        private void DFS(int node, Graph graph, HashSet<int> visited, List<int> component)
        {
            visited.Add(node);
            component.Add(node);

            foreach (var edge in graph.AdjacencyList[node])
            {
                if (!visited.Contains(edge.TargetId))
                    DFS(edge.TargetId, graph, visited, component);
            }
        }
    }
}
