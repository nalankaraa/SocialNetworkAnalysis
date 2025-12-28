using SocialNetworkAnalysis.Core.Enums;
using SocialNetworkAnalysis.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SocialNetworkAnalysis.Algorithms.Base
{
    public abstract class AnalysisAlgorithmBase : AlgorithmBase
    {
        // Klasik AlgorithmType kullanılmaz
        public sealed override AlgorithmType Type => AlgorithmType.None;

        // Analiz algoritmalarına özel tip
        public abstract AnalysisAlgorithmType AnalysisType { get; }
    }
}



