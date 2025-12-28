using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialNetworkAnalysis.Core.Models
{
    public class Edge
    {
        public int SourceId { get; set; }
        public int TargetId { get; set; }
        public double Weight { get; set; }
    }
}
