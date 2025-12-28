using SocialNetworkAnalysis.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialNetworkAnalysis.Core.Interfaces
{
    public interface IGraphRepository
    {
        Graph Load(string path);
        void Save(Graph graph, string path);
    }
}
