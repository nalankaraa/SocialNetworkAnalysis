using SocialNetworkAnalysis.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocialNetworkAnalysis.Algorithms.Base;

namespace SocialNetworkAnalysis.Services
{
    public class AlgorithmFactory
    {
        private readonly IEnumerable<AlgorithmBase> _algorithms;

        public AlgorithmFactory(IEnumerable<AlgorithmBase> algorithms)
        {
            _algorithms = algorithms;
        }

        public AlgorithmBase Create(AlgorithmType type)
        {
            return _algorithms.First(a => a.Type == type);
        }
    }


}
