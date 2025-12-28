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
    public abstract class AlgorithmBase
    {
        protected readonly Stopwatch Stopwatch = new();

        public abstract AlgorithmType Type { get; }

        public TimeSpan ExecutionTime { get; private set; }

        public void Run(Graph graph)
        {
            Stopwatch.Restart();
            Execute(graph);
            Stopwatch.Stop();
            ExecutionTime = Stopwatch.Elapsed;
        }

        protected abstract void Execute(Graph graph);
    }
}
