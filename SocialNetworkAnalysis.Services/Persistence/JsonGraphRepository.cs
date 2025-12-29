using SocialNetworkAnalysis.Core.Interfaces;
using SocialNetworkAnalysis.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SocialNetworkAnalysis.Services.Persistence
{
    public class JsonGraphRepository : IGraphRepository
    {
        public Graph Load(string path)
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Graph>(json)!;
        }

        public void Save(Graph graph, string path)
        {
            var json = JsonSerializer.Serialize(graph, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(path, json);
        }
    }
}
