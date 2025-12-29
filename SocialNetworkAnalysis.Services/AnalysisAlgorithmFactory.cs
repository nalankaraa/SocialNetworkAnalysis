using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocialNetworkAnalysis.Algorithms.Base;
using SocialNetworkAnalysis.Core.Enums;

namespace SocialNetworkAnalysis.Algorithms.Analysis
{
    public class AnalysisAlgorithmFactory
    {
        private readonly IEnumerable<AnalysisAlgorithmBase> _algorithms;

        public AnalysisAlgorithmFactory(IEnumerable<AnalysisAlgorithmBase> algorithms)
        {
            _algorithms = algorithms;
        }

        public AnalysisAlgorithmBase Create(AnalysisAlgorithmType type)
        {
            return _algorithms.First(a => a.AnalysisType == type);
        }
    }
}

