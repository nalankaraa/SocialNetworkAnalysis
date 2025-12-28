using System;

namespace SocialNetworkAnalysis.Analysis.Results
{
    public class AlgorithmStepResult
    {
        public int StepNumber { get; set; }
        public int NodeId { get; set; }
        public string NodeName { get; set; } = "";
        public string Description { get; set; } = "";
    }
}
