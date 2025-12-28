using SocialNetworkAnalysis.Algorithms.Base;
using SocialNetworkAnalysis.Algorithms.Results;
using SocialNetworkAnalysis.Core.Enums;
using SocialNetworkAnalysis.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SocialNetworkAnalysis.Algorithms.Analysis
{
    public class DegreeCentralityAlgorithm : AnalysisAlgorithmBase
    {
        public override AnalysisAlgorithmType AnalysisType =>
            AnalysisAlgorithmType.DegreeCentrality;

        public DegreeCentralityResult Result { get; } = new();

        protected override void Execute(Graph graph)
        {
            Result.Scores.Clear();

            int n = graph.AdjacencyList.Count;
            if (n <= 1) return;

            foreach (var kvp in graph.AdjacencyList)
            {
                Result.Scores[kvp.Key] =
                    (double)kvp.Value.Count / (n - 1);
            }
        }
    }
}
