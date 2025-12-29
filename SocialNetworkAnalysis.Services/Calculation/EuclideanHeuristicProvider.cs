using SocialNetworkAnalysis.Core.Interfaces;
using SocialNetworkAnalysis.Core.Models;
using System;

namespace SocialNetworkAnalysis.Services.Calculation
{
    public class EuclideanHeuristicProvider : IHeuristicProvider
    {
        public double Estimate(Node from, Node to)
        {
            // Simple Euclidean distance
            double dx = from.PosX - to.PosX;
            double dy = from.PosY - to.PosY;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
