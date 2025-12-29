using SocialNetworkAnalysis.Core.Exceptions;
using SocialNetworkAnalysis.Core.Interfaces;
using SocialNetworkAnalysis.Core.Models;
using SocialNetworkAnalysis.Services.Persistence;
using System.Text;
using System.Text.Json;

namespace SocialNetworkAnalysis.Services
{
    public class GraphService : IGraphService
    {
        private readonly IWeightCalculator _weightCalculator;
        private readonly CsvDataSeeder _csvDataSeeder;

        public Graph CurrentGraph { get; private set; } = new();

        public GraphService(IWeightCalculator weightCalculator, CsvDataSeeder csvDataSeeder)
        {
            _weightCalculator = weightCalculator;
            _csvDataSeeder = csvDataSeeder;
        }

        public void LoadGraph(string path)
        {
            CurrentGraph = _csvDataSeeder.LoadGraph(path, _weightCalculator);
        }

        public void AddNode(Node node)
        {
            if (CurrentGraph.Nodes.ContainsKey(node.Id))
                throw new GraphException("Bu ID'ye sahip düğüm zaten var.");

            CurrentGraph.AddNode(node);
        }

        public void AddNode(Node node, List<int> neighborIds)
        {
            AddNode(node);

            foreach (var nid in neighborIds)
            {
                if (CurrentGraph.Nodes.ContainsKey(nid))
                {
                    var weight = _weightCalculator.Calculate(node, CurrentGraph.Nodes[nid]);
                    CurrentGraph.AddEdge(node.Id, nid, weight);
                }
            }
        }

        public void AddNodeWithPersistence(Node node, List<int> neighborIds, string filePath)
        {
            AddNode(node, neighborIds);
            _csvDataSeeder.AppendNode(filePath, node, neighborIds);
        }

        public void AddEdge(Node source, Node target)
        {
            if (source.Id == target.Id)
                throw new GraphException("Self-loop yasak.");

            var weight = _weightCalculator.Calculate(source, target);
            CurrentGraph.AddEdge(source.Id, target.Id, weight);
        }

        public void AddEdge(Node source, Node target, double weight)
        {
            if (source.Id == target.Id)
                throw new GraphException("Self-loop yasak.");

            CurrentGraph.AddEdge(source.Id, target.Id, weight);
        }

        public void UpdateNode(Node node)
        {
            if (!CurrentGraph.Nodes.ContainsKey(node.Id))
                throw new GraphException("Node not found.");

            var existing = CurrentGraph.Nodes[node.Id];
            existing.Name = node.Name;
            existing.Club = node.Club;
            existing.Role = node.Role;
            existing.Activity = node.Activity;
            existing.Interaction = node.Interaction;

            if (CurrentGraph.AdjacencyList.ContainsKey(node.Id))
            {
                foreach (var edge in CurrentGraph.AdjacencyList[node.Id])
                {
                    if (CurrentGraph.Nodes.TryGetValue(edge.TargetId, out var target))
                        edge.Weight = _weightCalculator.Calculate(existing, target);
                }
            }
        }

        public void UpdateNode(Node node, List<int> neighborIds)
        {
            UpdateNode(node);

            // 1. Identify previous connections to handle removals
            if (CurrentGraph.AdjacencyList.ContainsKey(node.Id))
            {
                var currentEdges = CurrentGraph.AdjacencyList[node.Id].ToList();
                var currentNeighborIds = currentEdges.Select(e => e.TargetId).ToList();

                // Find neighbors that were removed
                var removedNeighborIds = currentNeighborIds.Except(neighborIds).ToList();

                foreach (var removedId in removedNeighborIds)
                {
                    // Remove the reverse edge from the neighbor
                    if (CurrentGraph.AdjacencyList.ContainsKey(removedId))
                    {
                        CurrentGraph.AdjacencyList[removedId].RemoveAll(e => e.TargetId == node.Id);
                    }
                }
            }

            // 2. Clear current node's list to rebuild it strictly from input
            if (CurrentGraph.AdjacencyList.ContainsKey(node.Id))
            {
                CurrentGraph.AdjacencyList[node.Id].Clear();
            }

            // 3. Add Edges (Forward and ensures Backward exists)
            foreach (var nid in neighborIds)
            {
                if (CurrentGraph.Nodes.ContainsKey(nid))
                {
                    var weight = _weightCalculator.Calculate(node, CurrentGraph.Nodes[nid]);

                    // Forward Edge (Always add because we cleared)
                    CurrentGraph.AdjacencyList[node.Id].Add(new Edge { SourceId = node.Id, TargetId = nid, Weight = weight });

                    // Backward Edge (Sync)
                    if (CurrentGraph.AdjacencyList.ContainsKey(nid))
                    {
                        var neighborEdges = CurrentGraph.AdjacencyList[nid];
                        var existingReverse = neighborEdges.FirstOrDefault(e => e.TargetId == node.Id);

                        if (existingReverse == null)
                        {
                            // Create if missing
                            neighborEdges.Add(new Edge { SourceId = nid, TargetId = node.Id, Weight = weight });
                        }
                        else
                        {
                            // Update weight if exists
                            existingReverse.Weight = weight;
                        }
                    }
                }
            }
        }

        public void UpdateNodeWithPersistence(Node node, List<int> neighborIds, string filePath)
        {
            UpdateNode(node, neighborIds);
            _csvDataSeeder.UpdateNode(filePath, node, neighborIds);
        }

        public void RemoveNode(int nodeId)
        {
            if (!CurrentGraph.Nodes.ContainsKey(nodeId)) return;

            CurrentGraph.Nodes.Remove(nodeId);
            CurrentGraph.AdjacencyList.Remove(nodeId);

            foreach (var list in CurrentGraph.AdjacencyList.Values)
                list.RemoveAll(e => e.TargetId == nodeId);
        }

        public void RemoveNodeWithPersistence(int nodeId, string filePath)
        {
            RemoveNode(nodeId);
            _csvDataSeeder.RemoveNode(filePath, nodeId);
        }

        public void RemoveEdge(int sourceId, int targetId)
        {
            // Remove forward
            if (CurrentGraph.AdjacencyList.ContainsKey(sourceId))
            {
                var edge = CurrentGraph.AdjacencyList[sourceId].FirstOrDefault(e => e.TargetId == targetId);
                if (edge != null) CurrentGraph.AdjacencyList[sourceId].Remove(edge);
            }

            // Remove backward (undirected)
            if (CurrentGraph.AdjacencyList.ContainsKey(targetId))
            {
                var edge = CurrentGraph.AdjacencyList[targetId].FirstOrDefault(e => e.TargetId == sourceId);
                if (edge != null) CurrentGraph.AdjacencyList[targetId].Remove(edge);
            }
        }

        public string ExportToJson()
        {
            var dto = new
            {
                Nodes = CurrentGraph.Nodes.Values.ToList(),
                Edges = CurrentGraph.AdjacencyList.SelectMany(x => x.Value).ToList()
            };

            return JsonSerializer.Serialize(
                   dto,
                   new JsonSerializerOptions
                   {
                       WriteIndented = true
                   }
            );

        }

        public void ImportFromJson(string jsonContent)
        {
            throw new NotImplementedException();
        }

        // 🔥 INTERFACE İLE UYUMLU
        public double[,] GenerateAdjacencyMatrix()
        {
            var nodes = CurrentGraph.Nodes.Values.OrderBy(n => n.Id).ToList();
            int n = nodes.Count;
            var matrix = new double[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    var from = nodes[i].Id;
                    var to = nodes[j].Id;

                    if (CurrentGraph.AdjacencyList.ContainsKey(from))
                    {
                        var edge = CurrentGraph.AdjacencyList[from]
                            .FirstOrDefault(e => e.TargetId == to);
                        matrix[i, j] = edge?.Weight ?? 0;
                    }
                }
            }
            return matrix;
        }

        public void ClearGraph()
        {
            CurrentGraph = new Graph();
        }
    }
}
