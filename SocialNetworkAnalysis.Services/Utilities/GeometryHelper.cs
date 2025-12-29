using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialNetworkAnalysis.Services.Utilities
{
    public static class GeometryHelper
    {
        public static double Distance(double x1, double y1, double x2, double y2)
            => Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
    }
}
