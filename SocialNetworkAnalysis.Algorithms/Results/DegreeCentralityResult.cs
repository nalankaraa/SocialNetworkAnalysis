using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocialNetworkAnalysis.Algorithms.Base;

namespace SocialNetworkAnalysis.Algorithms.Results
{
    public class DegreeCentralityResult
    {
        public Dictionary<int, double> Scores { get; set; } = new();
    }
}
