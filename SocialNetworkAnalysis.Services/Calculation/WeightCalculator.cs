using SocialNetworkAnalysis.Core.Interfaces;
using SocialNetworkAnalysis.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialNetworkAnalysis.Services.Calculation
{
    public class WeightCalculator : IWeightCalculator
    {
        public double Calculate(Node i, Node j)
        {
            // Formula: 1 / ( (1 + |ActDiff|) * (2 + |IntDiff|) * (2 + |ConnDiff|^2) )
            // Note: Use Math.Pow(..., 2) for the last term only.

            double diffActivity = Math.Abs(i.Activity - j.Activity);
            double diffInteraction = Math.Abs(i.Interaction - j.Interaction);
            double diffConnection = Math.Abs(i.ConnectionCount - j.ConnectionCount);

            double part1 = 1 + diffActivity;
            double part2 = 2 + diffInteraction;
            double part3 = Math.Pow(2 + diffConnection, 2);

            double denominator = part1 * part2 * part3;

            if (denominator == 0) return 0; // Avoid division by zero, though unlikely with +1/+2 offsets.

            return 1.0 / denominator;
        }
    }
}
