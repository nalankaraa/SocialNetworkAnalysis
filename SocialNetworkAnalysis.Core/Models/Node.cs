using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialNetworkAnalysis.Core.Models
{
    public class Node
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Club { get; set; } = "";
        public string Role { get; set; } = "";
        public double Activity { get; set; }
        public double Interaction { get; set; }
        public int ConnectionCount { get; set; }
        public double PosX { get; set; }
        public double PosY { get; set; }
    }
}
