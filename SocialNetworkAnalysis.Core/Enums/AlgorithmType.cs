using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialNetworkAnalysis.Core.Enums
{
    public enum AlgorithmType
    {
        BFS,
        DFS,
        Dijkstra,
        AStar,
        WelshPowell,
        None
    }

    public enum AnalysisAlgorithmType
    {
        DegreeCentrality,
        ConnectedComponents,
        ClubCommunityDetection,
        None
    }



}
