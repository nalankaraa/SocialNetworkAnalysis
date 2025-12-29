using SocialNetworkAnalysis.Core.Interfaces;
using SocialNetworkAnalysis.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialNetworkAnalysis.Services.Persistence
{
    public class CsvDataSeeder
    {
        public Graph LoadGraph(string path, IWeightCalculator weightCalculator)
        {
            var graph = new Graph();
            if (!File.Exists(path)) return graph;

            var lines = File.ReadAllLines(path);

            // Skip header if it exists. Assume first line is header if it starts with text like "DugumId"
            // Or just skip first line always as per previous implementation logic .Skip(1)
            var dataLines = lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l));

            // First pass: Create Nodes
            foreach (var line in dataLines)
            {
                try
                {
                    var parts = ParseLine(line);
                    // Schema: Id, Name, Club, Role, Activity, Interaction, ConnectionCount, Neighbors
                    if (parts.Length < 7) continue;

                    var node = new Node
                    {
                        Id = int.Parse(parts[0]),
                        Name = parts[1],
                        Club = parts[2],
                        Role = parts[3],
                        Activity = double.Parse(parts[4], System.Globalization.CultureInfo.InvariantCulture),
                        Interaction = double.Parse(parts[5], System.Globalization.CultureInfo.InvariantCulture),
                        ConnectionCount = int.Parse(parts[6])
                    };
                    graph.AddNode(node);
                }
                catch (FormatException) { continue; } // Skip malformed lines
                catch (Exception) { continue; }
            }

            // Second pass: Create Edges
            foreach (var line in dataLines)
            {
                var parts = ParseLine(line);
                if (parts.Length < 8) continue; // No neighbors?

                int sourceId = int.Parse(parts[0]);
                string neighborsStr = parts[7];

                var neighborIds = neighborsStr.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var nIdStr in neighborIds)
                {
                    if (int.TryParse(nIdStr, out int targetId))
                    {
                        if (targetId == sourceId) continue; // No self loops? Req says prevent them.

                        // Check if nodes exist
                        if (graph.Nodes.ContainsKey(sourceId) && graph.Nodes.ContainsKey(targetId))
                        {
                            var source = graph.Nodes[sourceId];
                            var target = graph.Nodes[targetId];
                            double weight = weightCalculator.Calculate(source, target);

                            // AddEdge handles both directions if needed, but the CSV might define adjacency fully or partially.
                            // If CSV says 1 connects to 2, and 2 connects to 1 separately, AddEdge might add duplicates?
                            // My Graph.AddEdge adds both directions. 
                            // Using a check to avoid re-adding if edge exists.

                            bool alreadyConnects = graph.AdjacencyList[sourceId].Any(e => e.TargetId == targetId);
                            if (!alreadyConnects)
                            {
                                graph.AddEdge(sourceId, targetId, weight);
                            }
                        }
                    }
                }
            }

            return graph;
        }

        private string[] ParseLine(string line)
        {
            // Simple CSV split for now. 
            // If we have complex quoting, we need a regex or full parser.
            // Assumption: Simple CSV: Val,Val,Val,Val,Neighbors
            // If neighbors contain commas, they should be quoted "2,4,5"
            // This simple split fails for quoted commas.

            // Let's assume standard CSV with possible quotes
            // Or fallback to space separated if the user provided that example literally.

            if (line.Contains(","))
            {
                // Regex for CSV splitting handling quotes
                // But simplified:
                // If we see quotes, we treat it specially.
                // For this academic project, let's try strict comma split, 
                // but if the last part is the neighbors list and it relies on being the 'rest', it's tricky.

                // Quick fix: Split by comma but re-join if inside quotes? 
                // Nah, let's just assume the helper method "ParseCsvLine"
                return SplitCsvLine(line);
            }
            else
            {
                // Space separated?
                return line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        private string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result.ToArray();
        }
        public void AppendNode(string path, Node node, List<int> neighborIds)
        {
            if (!File.Exists(path)) return;

            // Format: Id, Name, Club, Role, Activity, Interaction, ConnectionCount, Neighbors
            var neighborStr = string.Join(",", neighborIds);

            // Handle CSV escaping if needed (simple quote wrap if comma exists)
            if (neighborStr.Contains(",")) neighborStr = $"\"{neighborStr}\"";

            var line = $"{node.Id},{node.Name},{node.Club},{node.Role},{node.Activity.ToString(System.Globalization.CultureInfo.InvariantCulture)},{node.Interaction.ToString(System.Globalization.CultureInfo.InvariantCulture)},{node.ConnectionCount},{neighborStr}";

            File.AppendAllLines(path, new[] { line });
        }

        public void RemoveNode(string path, int nodeId)
        {
            if (!File.Exists(path)) return;

            var lines = File.ReadAllLines(path);
            if (lines.Length == 0) return;

            var newLines = new List<string>();
            // Keep header
            newLines.Add(lines[0]);

            var dataLines = lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l));

            foreach (var line in dataLines)
            {
                // Parse to check ID
                var parts = ParseLine(line);
                if (parts.Length < 1) continue;

                if (int.TryParse(parts[0], out int id))
                {
                    // Skip the node to be deleted
                    if (id == nodeId) continue;

                    // For other nodes, we must update the Neighbors column
                    // Reconstruct line is tricky without a full object model here, 
                    // but we can try to locate the neighbors part.
                    // Assuming columns: 0..6 fixed, 7 is neighbors.

                    if (parts.Length >= 8)
                    {
                        var neighborsStr = parts[7];
                        var neighborParts = neighborsStr.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                        if (neighborParts.Contains(nodeId.ToString()))
                        {
                            neighborParts.Remove(nodeId.ToString());

                            // Reconstruct neighbors string
                            var newNeighborsStr = string.Join(",", neighborParts);
                            if (newNeighborsStr.Contains(",")) newNeighborsStr = $"\"{newNeighborsStr}\"";

                            // Reconstruct the full line using parts 0-6 and new neighbors
                            var sb = new StringBuilder();
                            for (int i = 0; i < 7; i++)
                            {
                                sb.Append(parts[i]).Append(",");
                            }
                            sb.Append(newNeighborsStr);
                            newLines.Add(sb.ToString());
                        }
                        else
                        {
                            // No change needed for this line
                            newLines.Add(line);
                        }
                    }
                    else
                    {
                        newLines.Add(line);
                    }
                }
                else
                {
                    newLines.Add(line);
                }
            }

            File.WriteAllLines(path, newLines);
        }
        public void UpdateNode(string path, Node node, List<int> neighborIds)
        {
            if (!File.Exists(path)) return;

            var lines = File.ReadAllLines(path);
            if (lines.Length == 0) return;

            var newLines = new List<string>();
            newLines.Add(lines[0]); // Header

            var dataLines = lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l));

            foreach (var line in dataLines)
            {
                var parts = ParseLine(line);
                if (parts.Length < 1) continue;

                if (int.TryParse(parts[0], out int id))
                {
                    if (id == node.Id)
                    {
                        // The Target Node: Update thoroughly
                        var neighborStr = string.Join(",", neighborIds);
                        if (neighborStr.Contains(",")) neighborStr = $"\"{neighborStr}\"";

                        var newLine = $"{node.Id},{node.Name},{node.Club},{node.Role},{node.Activity.ToString(System.Globalization.CultureInfo.InvariantCulture)},{node.Interaction.ToString(System.Globalization.CultureInfo.InvariantCulture)},{node.ConnectionCount},{neighborStr}";
                        newLines.Add(newLine);
                    }
                    else
                    {
                        // Other Nodes: Check and Sync Bidirectional Connection
                        bool shouldBeNeighbor = neighborIds.Contains(id);

                        // Parse existing neighbors of this other node
                        var currentNeighbors = new List<string>();
                        string originalNeighborsStr = "";

                        if (parts.Length >= 8)
                        {
                            originalNeighborsStr = parts[7];
                            var split = originalNeighborsStr.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            currentNeighbors.AddRange(split);
                        }

                        bool isCurrentlyNeighbor = currentNeighbors.Contains(node.Id.ToString());
                        bool changed = false;

                        if (shouldBeNeighbor && !isCurrentlyNeighbor)
                        {
                            // Add connection
                            currentNeighbors.Add(node.Id.ToString());
                            changed = true;
                        }
                        else if (!shouldBeNeighbor && isCurrentlyNeighbor)
                        {
                            // Remove connection
                            currentNeighbors.Remove(node.Id.ToString());
                            changed = true;
                        }

                        if (changed)
                        {
                            // Reconstruct the neighbors string
                            // Sort for consistency if desired, but not strictly required
                            var newNeighborsStr = string.Join(",", currentNeighbors);
                            if (newNeighborsStr.Contains(",")) newNeighborsStr = $"\"{newNeighborsStr}\"";

                            // Reconstruct the full line
                            var sb = new StringBuilder();
                            // Append parts 0-6 (Activity/Interaction need to be careful not to lose precision if re-parsing, 
                            // but parts are strings from ParseLine, so they should be safe unless ParseLine did weird things. 
                            // Wait, ParseLine returns strings. But ParseLine splits by commas.
                            // Re-joining parts[0]..parts[6] with commas is safe IF they don't contain commas.
                            // Names/Clubs might contain commas?
                            // Standard CSV rule: If it contains comma, it should have been quoted. 
                            // My ParseLine handles removing quotes. So parts[1] "Doe, John" becomes "Doe, John".
                            // When re-writing, I must check if it needs quotes again.

                            for (int i = 0; i < 7; i++)
                            {
                                string p = (i < parts.Length) ? parts[i] : "";
                                if (p.Contains(",")) sb.Append($"\"{p}\"");
                                else sb.Append(p);

                                sb.Append(",");
                            }
                            sb.Append(newNeighborsStr);
                            newLines.Add(sb.ToString());
                        }
                        else
                        {
                            // No change
                            newLines.Add(line);
                        }
                    }
                }
                else
                {
                    newLines.Add(line);
                }
            }

            File.WriteAllLines(path, newLines);
        }
    }
}
