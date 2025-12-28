using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialNetworkAnalysis.Core.Exceptions
{
    public class GraphException : Exception
    {
        public GraphException(string message) : base(message) { }
    }
}
