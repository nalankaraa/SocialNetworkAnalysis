using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SocialNetworkAnalysis.Algorithms.Analysis;
using SocialNetworkAnalysis.Algorithms.Base;
using SocialNetworkAnalysis.Analysis.Results;
using SocialNetworkAnalysis.Core.Enums;
using SocialNetworkAnalysis.Core.Interfaces;
using SocialNetworkAnalysis.Core.Models;
using SocialNetworkAnalysis.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace SocialNetworkAnalysis.UI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // ... previous code ...

        private string GetColor(int index)
        {
            // Simple palette
            string[] colors = { "#FF5733", "#33FF57", "#3357FF", "#F1C40F", "#9B59B6", "#E67E22", "#1ABC9C", "#34495E" };
            return colors[(index - 1) % colors.Length]; // index starts at 1 usually
        }
        private readonly IGraphService _graphService;
        private readonly AlgorithmFactory _algorithmFactory;
        private readonly AnalysisAlgorithmFactory _analysisAlgorithmFactory;

        [ObservableProperty]
        private ObservableCollection<NodeViewModel> _nodes = new();

        [ObservableProperty]
        private ObservableCollection<EdgeViewModel> _edges = new();

        [ObservableProperty]
        private int? _startNodeId;

        [ObservableProperty]
        private int? _endNodeId;

        [ObservableProperty]
        private AlgorithmType _selectedAlgorithm;

        partial void OnSelectedAlgorithmChanged(AlgorithmType value)
        {
            if (value == AlgorithmType.None)
            {
                foreach (var node in Nodes)
                {
                    node.ResetColor();
                    node.InfoText = null; // Clear info text too
                }
                ResultText = "";
                ChartData.Clear();
                AlgorithmSteps.Clear();
                ResetVisuals(); // Clear highlights
            }
        }

        [ObservableProperty]
        private string _statusMessage = "Ready";

        private string _currentCsvPath;

        [ObservableProperty]
        private NodeViewModel _selectedNode;

        public IEnumerable<AlgorithmType> AlgorithmTypes => Enum.GetValues(typeof(AlgorithmType)).Cast<AlgorithmType>();

        // Analysis results
        [ObservableProperty]
        private string _resultText = "";

        public MainViewModel(
     IGraphService graphService,
     AlgorithmFactory algorithmFactory,
     AnalysisAlgorithmFactory analysisAlgorithmFactory)
        {
            _graphService = graphService;
            _algorithmFactory = algorithmFactory;
            _analysisAlgorithmFactory = analysisAlgorithmFactory;
        }

        [RelayCommand]
        private void SelectNode(NodeViewModel node)
        {
            SelectedNode = node;
            SelectedEdge = null; // Exclusive selection

            // Optionally highlight
            ResetVisuals();
            if (node != null)
            {
                node.IsHighlighted = true;

                // Populate Neighbors (optional if removed from UI, but good for internal logic)
                node.NeighborIds.Clear();
                if (_graphService.CurrentGraph.AdjacencyList.ContainsKey(node.Id))
                {
                    foreach (var edge in _graphService.CurrentGraph.AdjacencyList[node.Id])
                    {
                        node.NeighborIds.Add(edge.TargetId);
                    }
                }
            }
        }

        [ObservableProperty]
        private EdgeViewModel _selectedEdge;

        partial void OnSelectedEdgeChanged(EdgeViewModel value)
        {
            if (value != null)
            {
                SelectedNode = null; // Exclusive selection
                ResetVisuals();
                value.IsHighlighted = true;
                if (value.Source != null) value.Source.IsHighlighted = true;
                if (value.Target != null) value.Target.IsHighlighted = true;
            }
        }

        [RelayCommand]
        private void SelectEdge(EdgeViewModel edge)
        {
            SelectedEdge = edge;
        }

        [RelayCommand]
        private void AddEdge()
        {
            var window = new Views.AddEdgeWindow();
            if (Application.Current.MainWindow != null)
                window.Owner = Application.Current.MainWindow;

            bool? result = window.ShowDialog();

            if (result == true)
            {
                int sourceId = window.SourceId;
                int targetId = window.TargetId;
                double weight = window.Weight;

                // Validate if nodes exist
                if (!_graphService.CurrentGraph.Nodes.ContainsKey(sourceId))
                {
                    MessageBox.Show($"Source Node {sourceId} does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (!_graphService.CurrentGraph.Nodes.ContainsKey(targetId))
                {
                    MessageBox.Show($"Target Node {targetId} does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Add Edge
                try
                {
                    var sourceNode = _graphService.CurrentGraph.Nodes[sourceId];
                    var targetNode = _graphService.CurrentGraph.Nodes[targetId];

                    _graphService.AddEdge(sourceNode, targetNode, weight);

                    RefreshGraph();
                    StatusMessage = $"Edge added between {sourceNode.Name} and {targetNode.Name} (Weight: {weight})";
                    CalculateInfluenceScores();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to add edge: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void UpdateEdge(EdgeViewModel edgeVm)
        {
            if (edgeVm == null) return;
            // In-memory update of weight is already done via binding.
            // We can refresh graph or just confirm.
            StatusMessage = $"Edge weight updated to {edgeVm.Weight} (Session Only).";

            // If algorithms need simpler re-init, might need to re-run.
            // But usually they read from graph. 
            // We need to ensure the graph.AdjacencyList also has the new weight.
            // Since EdgeViewModel wraps the Edge model, and Edge model is Ref, it *should* update underlying model.
            // Let's verify: EdgeViewModel -> Edge Model. Edge Model is in Graph.AdjacencyList. So yes.
        }

        [RelayCommand]
        private void RemoveEdge(EdgeViewModel edgeVm)
        {
            if (edgeVm == null) return;

            try
            {
                // 1. Remove from Graph Service
                _graphService.RemoveEdge(edgeVm.Source.Id, edgeVm.Target.Id);

                // 2. Remove from VM Collection
                Edges.Remove(edgeVm);

                // 3. Clear selection
                SelectedEdge = null;
                ResetVisuals();

                StatusMessage = "Edge removed successfully (Session Only).";
                CalculateInfluenceScores();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Remove Edge Failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ClearSession()
        {
            _currentCsvPath = null;
            _graphService.ClearGraph();

            // Clear ViewModels
            Nodes.Clear();
            Edges.Clear();
            AlgorithmSteps.Clear();
            TopInfluentialNodes.Clear();
            ChartData.Clear();
            SelectedNode = null;
            StartNodeId = null;
            EndNodeId = null;
            ChartTitle = "Analysis Data";
            ResultText = "";

            StatusMessage = "Session cleared. Ready for manual editing.";
        }

        [RelayCommand]
        private void LoadCsv()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "CSV Files|*.csv" };
                if (dialog.ShowDialog() == true)
                {
                    StatusMessage = "Loading data...";
                    _currentCsvPath = dialog.FileName;
                    _graphService.LoadGraph(dialog.FileName);
                    RefreshGraph();
                    StatusMessage = $"Graph loaded: {_graphService.CurrentGraph.Nodes.Count} nodes, {_graphService.CurrentGraph.EdgeCount} edges.";

                    // Reset algos
                    ResultText = "";
                    AlgorithmSteps.Clear();

                    // Show Influence Scores by default
                    CalculateInfluenceScores();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading CSV: {ex.Message}";
            }
        }


        [RelayCommand]
        private void RemoveNode()
        {
            int? targetId = null;
            string nodeName = "";

            if (SelectedNode != null)
            {
                targetId = SelectedNode.Id;
                nodeName = SelectedNode.Name;
            }
            else if (StartNodeId.HasValue)
            {
                targetId = StartNodeId.Value;
                nodeName = targetId.ToString();
            }

            if (targetId.HasValue)
            {
                // Check if CSV path is known for persistence
                if (!string.IsNullOrEmpty(_currentCsvPath))
                {
                    try
                    {
                        _graphService.RemoveNodeWithPersistence(targetId.Value, _currentCsvPath);
                        RefreshGraph();
                        StatusMessage = $"Node {nodeName} (ID: {targetId.Value}) permanently removed from Graph and CSV.";
                        SelectedNode = null;
                        CalculateInfluenceScores();
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Remove Failed: {ex.Message}";
                    }
                }
                else
                {
                    // Fallback to memory only if no CSV loaded (shouldn't happen in normal flow if loaded)
                    _graphService.RemoveNode(targetId.Value);
                    RefreshGraph();
                    StatusMessage = $"Node {targetId.Value} removed (Memory Only - CSV path not set).";
                    SelectedNode = null;
                }
            }
            else
            {
                StatusMessage = "Select a Node or enter Node ID in 'Start ID' to remove.";
            }
        }

        [RelayCommand]
        private void UpdateNode(NodeViewModel nodeVm)
        {
            UpdateNodeWithPersistence();
        }

        [RelayCommand]
        private void UpdateNodeWithPersistence()
        {
            if (SelectedNode == null) return;

            // Check if CSV path is known for persistence
            // Check if CSV path is known for persistence
            if (!string.IsNullOrEmpty(_currentCsvPath))
            {
                try
                {
                    var neighborList = SelectedNode.NeighborIds.ToList();
                    _graphService.UpdateNodeWithPersistence(SelectedNode.Model, neighborList, _currentCsvPath);

                    RefreshGraph(); // To update edges visually
                    StatusMessage = $"Node {SelectedNode.Name} updated successfully.";
                    CalculateInfluenceScores();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Update Failed: {ex.Message}";
                }
            }
            else
            {
                // Memory Only Update
                try
                {
                    var neighborList = SelectedNode.NeighborIds.ToList();
                    _graphService.UpdateNode(SelectedNode.Model, neighborList); // Use new overload
                    RefreshGraph();
                    StatusMessage = $"Node {SelectedNode.Name} updated (Memory Only).";
                    CalculateInfluenceScores();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Update Failed: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        private void AddNeighbor(string neighborIdRaw)
        {
            if (SelectedNode == null || string.IsNullOrWhiteSpace(neighborIdRaw)) return;

            if (int.TryParse(neighborIdRaw, out int nid))
            {
                if (nid == SelectedNode.Id)
                {
                    StatusMessage = "Cannot link node to itself.";
                    return;
                }

                if (!SelectedNode.NeighborIds.Contains(nid))
                {
                    // Verify if target node exists
                    if (_graphService.CurrentGraph.Nodes.ContainsKey(nid))
                    {
                        SelectedNode.NeighborIds.Add(nid);
                        StatusMessage = $"Neighbor {nid} added (unsaved). Click Update to persist.";
                    }
                    else
                    {
                        StatusMessage = $"Node ID {nid} does not exist.";
                    }
                }
            }
        }

        [RelayCommand]
        private void RemoveNeighbor(object neighborIdObj)
        {
            if (SelectedNode == null || neighborIdObj == null) return;

            if (neighborIdObj is int nid)
            {
                if (SelectedNode.NeighborIds.Contains(nid))
                {
                    SelectedNode.NeighborIds.Remove(nid);
                    StatusMessage = $"Neighbor {nid} removed (unsaved). Click Update to persist.";
                }
            }
        }

        [RelayCommand]
        private void AddNode()
        {
            // Removed CSV check to allow memory-only addition
            // if (string.IsNullOrEmpty(_currentCsvPath)) ...

            bool isCsvLoaded = !string.IsNullOrEmpty(_currentCsvPath);
            var dialog = new SocialNetworkAnalysis.UI.Views.AddNodeWindow(showNeighbors: isCsvLoaded);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Generate new ID
                    int newId = 1;
                    if (_graphService.CurrentGraph.Nodes.Any())
                    {
                        newId = _graphService.CurrentGraph.Nodes.Keys.Max() + 1;
                    }

                    var node = dialog.NewNode;
                    node.Id = newId;

                    if (!string.IsNullOrEmpty(_currentCsvPath))
                    {
                        _graphService.AddNodeWithPersistence(node, dialog.NeighborIds, _currentCsvPath);
                        StatusMessage = $"Node {node.Name} (ID: {node.Id}) added to Graph and CSV.";
                    }
                    else
                    {
                        _graphService.AddNode(node, dialog.NeighborIds);
                        StatusMessage = $"Node {node.Name} (ID: {node.Id}) added (Memory Only).";
                    }

                    RefreshGraph();
                    CalculateInfluenceScores();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Add Node Failed: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        private void ExportJson()
        {
            try
            {
                var json = _graphService.ExportToJson();
                var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "JSON Files|*.json", FileName = "graph_export.json" };
                if (dialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(dialog.FileName, json);
                    StatusMessage = $"Exported to {dialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export Failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ImportJson()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "JSON Files|*.json" };
                if (dialog.ShowDialog() == true)
                {
                    var json = System.IO.File.ReadAllText(dialog.FileName);
                    _graphService.ImportFromJson(json);
                    RefreshGraph();
                    StatusMessage = $"Imported graph from {dialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Import Failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ShowMatrix()
        {
            try
            {
                // Visual Matrix Window
                var matrixWindow = new SocialNetworkAnalysis.UI.Views.MatrixWindow(_graphService.CurrentGraph);
                matrixWindow.Show();
                StatusMessage = "Visual Matrix opened.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Matrix Failed: {ex.Message}";
            }
        }

        [ObservableProperty]
        private string _executionText = "Last Run: -";

        [RelayCommand]
        private async Task RunAlgorithm()
        {
            if (SelectedAlgorithm == AlgorithmType.None)
            {
                StatusMessage = "Please select an algorithm.";
                return;
            }

            AlgorithmBase algo = null;
            AnalysisAlgorithmBase analysisAlgo = null;

            // 1️⃣ Traversal / ShortestPath / Coloring algoritmaları
            if (SelectedAlgorithm != AlgorithmType.None)
            {
                algo = _algorithmFactory.Create(SelectedAlgorithm);
            }

            if (SelectedAlgorithm == AlgorithmType.None)
            {
                // Check if we are in a special analysis mode passed via CommandParameter or just context
                // For now, let's assume if it's not a standard algo, we might default to Centrality OR we rely on a separate specific command.

                // However, "RunAlgorithm" uses "SelectedAlgorithm".
                // "ClubCommunityDetection" is in "AnalysisAlgorithmType".
                // We need a way to run it.

                // If SelectedAlgorithm is None, we check specific flags or we just don't run here.
                // The "RunCommunityAnalysis" command will likely set a property or call logic directly.
                // But let's hack it: 
                // If we want to use the main "Run Analysis" button...
                // But user asked for a SEPARATE button.

                // So "RunAlgorithm" is for the main combo box.
                // "RunCommunityAnalysis" will be a separate method.

                analysisAlgo = _analysisAlgorithmFactory.Create(
                   AnalysisAlgorithmType.DegreeCentrality);
            }


            if (algo == null)
            {
                StatusMessage = "Algorithm implementation not found.";
                return;
            }

            // Configure algo
            if (algo is SocialNetworkAnalysis.Algorithms.ShortestPath.DijkstraAlgorithm dijkstra)
            {
                dijkstra.StartNodeId = StartNodeId;
                dijkstra.EndNodeId = EndNodeId;
            }
            if (algo is SocialNetworkAnalysis.Algorithms.ShortestPath.AStarAlgorithm astar)
            {
                astar.StartNodeId = StartNodeId;
                astar.EndNodeId = EndNodeId;
            }
            if (algo is SocialNetworkAnalysis.Algorithms.Traversal.BFSAlgorithm bfs)
            {
                bfs.StartNodeId = StartNodeId;
            }
            if (algo is SocialNetworkAnalysis.Algorithms.Traversal.DFSAlgorithm dfs)
            {
                dfs.StartNodeId = StartNodeId;
            }

            StatusMessage = $"Running {SelectedAlgorithm}...";

            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                if (algo != null)
                {
                    await Task.Run(() => algo.Run(_graphService.CurrentGraph));
                    await VisualizeResults(algo);
                }
                else if (analysisAlgo != null)
                {
                    await Task.Run(() => analysisAlgo.Run(_graphService.CurrentGraph));
                    await VisualizeResults(analysisAlgo);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Algorithm Error: {ex.Message}";
                return;
            }

            sw.Stop();

            // Special handling for ClubCommunityDetection to expose Results
            if (analysisAlgo is SocialNetworkAnalysis.Algorithms.Analysis.ClubCommunityDetectionAlgorithm clubAlgo)
            {
                // Manually copy results to VM collection
                foreach (var res in clubAlgo.Results)
                {
                    AlgorithmSteps.Add(res);
                }
                // Color nodes
                foreach (var kvp in clubAlgo.NodeCommunityMap)
                {
                    var nodeVm = Nodes.FirstOrDefault(n => n.Id == kvp.Key);
                    if (nodeVm != null)
                    {
                        nodeVm.Color = GetColor(kvp.Value);
                    }
                }
                ResultText = $"Found {clubAlgo.Results.Count} Communities.";
            }

            ExecutionText = $"Last Run: {sw.ElapsedMilliseconds}ms";
            StatusMessage = "Algorithm completed.";

        }


        [RelayCommand]
        private async Task RunCommunityAnalysis()
        {
            StatusMessage = "Running Club Community Analysis...";
            try
            {
                var algo = _analysisAlgorithmFactory.Create(AnalysisAlgorithmType.ClubCommunityDetection);
                if (algo == null)
                {
                    StatusMessage = "Algorithm not found.";
                    return;
                }

                await Task.Run(() => algo.Run(_graphService.CurrentGraph));

                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    ResetVisuals();
                    AlgorithmSteps.Clear();
                    ResultText = "";

                    await VisualizeResults(algo);

                    StatusMessage = "Community Analysis Completed.";
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Analysis Error: {ex.Message}";
            }
        }

        private void RefreshGraph()
        {
            Nodes.Clear();
            Edges.Clear();

            var graph = _graphService.CurrentGraph;
            var nodeKeys = graph.Nodes.Keys.ToList();
            int nodeCount = nodeKeys.Count;

            if (nodeCount == 0) return;

            // --- Optimized Force-Directed Layout for Compactness ---
            double width = 800; // Virtual width for calculation (smaller than actual canvas to force density)
            double height = 600;
            // Use a specific node size for calculations
            double nodeSize = 50;

            // Tune k: Optimal distance between nodes
            // Reducing the area factor makes the graph denser.
            // Using logic: k ~ C * sqrt(area/N). C is usually ~0.75
            // User requested: "Nodes should be close when adding multiple" (Small N problem)

            // CRITIAL FIX: For small N, Sqrt(Area/N) yields a HUGE number (e.g. 500px). 
            // We must CAP k to reasonable limit (e.g. 150px) so they don't fly apart.
            double baseK = Math.Sqrt((width * height) / (nodeCount + 1)); // +1 to avoid div zero

            // Adaptive Density:
            // If N is small (< 10), we actually want a SMALLER multiplier to keep them close.
            // Old logic (densityFactor 1.5) was making them HUGE.
            double densityFactor = nodeCount < 10 ? 0.3 : (nodeCount < 50 ? 0.6 : 0.8);

            double k = baseK * densityFactor;

            // Hard Cap on K: Never let ideal distance be more than ~120px
            if (k > 150) k = 150;
            if (k < 60) k = 60; // Minimum size

            // Constraints requested by user: Min/Max distance
            // We enforce these via physics dampening/boosting
            double minDistance = nodeSize * 1.2; // Reduced slightly
            double maxDistance = k * 2.5;        // Tighter max distance logic

            int iterations = 300;
            double temp = width / 8; // Initial temperature

            // Physics Constants
            double gravity = 0.5; // STRONG gravity to keep it compact and centered

            // Center point
            double centerX = width / 2;
            double centerY = height / 2;

            // Calculate center of mass of EXISTING nodes if any, to place new nodes near them
            // If most nodes are at (0,0), use screen center.
            double avgX = 0, avgY = 0;
            int placedCount = 0;
            foreach (var n in graph.Nodes.Values)
            {
                if (Math.Abs(n.PosX) > 1 && Math.Abs(n.PosY) > 1)
                {
                    avgX += n.PosX;
                    avgY += n.PosY;
                    placedCount++;
                }
            }
            if (placedCount > 0)
            {
                centerX = avgX / placedCount;
                centerY = avgY / placedCount;
            }

            // Initialize positions (Random Scatter in a tighter circle for new layout)
            Random rnd = new Random();
            foreach (var node in graph.Nodes.Values)
            {
                // Only reset if at 0,0 (newly added or reset requested)
                // Or if they are way off screen
                if ((node.PosX == 0 && node.PosY == 0) || Math.Abs(node.PosX) > 5000)
                {
                    // Random Scatter with wider range (User requested "Random")
                    // Use a range of ~300px around center to be distinct but visible
                    node.PosX = centerX + (rnd.NextDouble() - 0.5) * 300;
                    node.PosY = centerY + (rnd.NextDouble() - 0.5) * 300;
                }
            }

            // Simulation Loop
            try
            {
                var dispX = new Dictionary<int, double>();
                var dispY = new Dictionary<int, double>();

                for (int i = 0; i < iterations; i++)
                {
                    // Optimization: limit temperature influence over time
                    // Start hot, cool down
                    bool isCoolingPhase = i > iterations * 0.7;

                    foreach (var id in nodeKeys) { dispX[id] = 0; dispY[id] = 0; }

                    // 1. Repulsive forces (All pairs)
                    // F_rep = k^2 / d
                    for (int u = 0; u < nodeCount; u++)
                    {
                        var n1 = graph.Nodes[nodeKeys[u]];
                        for (int v = u + 1; v < nodeCount; v++)
                        {
                            var n2 = graph.Nodes[nodeKeys[v]];

                            double dx = n1.PosX - n2.PosX;
                            double dy = n1.PosY - n2.PosY;
                            double dSq = dx * dx + dy * dy;
                            double d = Math.Sqrt(dSq);

                            if (d < 0.1) d = 0.1;

                            // Custom Repulsion
                            // If too close (overlap risk), exponential repulsion
                            // If far, normal repulsion

                            double force = 0;

                            if (d < minDistance)
                            {
                                // Overlap prevention: Extremely high force
                                force = (k * k * 5) / d;
                            }
                            else
                            {
                                // Standard Fruchterman-Reingold
                                force = (k * k) / d;
                            }

                            // Apply weighting for 'balanced density'
                            // Remove long-range repulsion to allow isolated clusters to come closer to center
                            if (d > maxDistance * 2)
                                force = 0;

                            double fx = (dx / d) * force;
                            double fy = (dy / d) * force;

                            dispX[n1.Id] += fx;
                            dispY[n1.Id] += fy;
                            dispX[n2.Id] -= fx;
                            dispY[n2.Id] -= fy;
                        }
                    }

                    // 2. Attractive forces (Edges)
                    // F_att = d^2 / k
                    foreach (var list in graph.AdjacencyList.Values)
                    {
                        foreach (var edge in list)
                        {
                            if (edge.SourceId >= edge.TargetId) continue;

                            var n1 = graph.Nodes[edge.SourceId];
                            var n2 = graph.Nodes[edge.TargetId];

                            double dx = n1.PosX - n2.PosX;
                            double dy = n1.PosY - n2.PosY;
                            double d = Math.Sqrt(dx * dx + dy * dy);
                            if (d < 0.1) d = 0.1;

                            // Standard Attraction
                            double force = (d * d) / k;

                            // Constrain max distance: if d > max, pull HARDER
                            if (d > maxDistance)
                            {
                                force *= 2.0;
                            }

                            double fx = (dx / d) * force;
                            double fy = (dy / d) * force;

                            dispX[n1.Id] -= fx;
                            dispY[n1.Id] -= fy;
                            dispX[n2.Id] += fx;
                            dispY[n2.Id] += fy;
                        }
                    }

                    // 3. Central Gravity (Crucial for "compactness")
                    foreach (var id in nodeKeys)
                    {
                        var n = graph.Nodes[id];
                        double dx = centerX - n.PosX;
                        double dy = centerY - n.PosY;

                        // Pull to center
                        // Force increases with distance from center
                        // Using a stronger gravity factor than standard

                        dispX[id] += dx * gravity;
                        dispY[id] += dy * gravity;
                    }

                    // 4. Apply Displacement
                    // Limit max displacement by Temperature
                    foreach (var id in nodeKeys)
                    {
                        var node = graph.Nodes[id];
                        double dx = dispX[id];
                        double dy = dispY[id];
                        double d = Math.Sqrt(dx * dx + dy * dy);

                        if (d > 0)
                        {
                            // Cap displacement at temp
                            double limitedDist = Math.Min(d, temp);

                            // Apply move
                            node.PosX += (dx / d) * limitedDist;
                            node.PosY += (dy / d) * limitedDist;
                        }
                    }

                    // Cooling
                    temp *= 0.95;
                }

                // 5. Post-Processing: Center and Bounds Check
                double minX = double.MaxValue, maxX = double.MinValue;
                double minY = double.MaxValue, maxY = double.MinValue;

                foreach (var node in graph.Nodes.Values)
                {
                    if (node.PosX < minX) minX = node.PosX;
                    if (node.PosX > maxX) maxX = node.PosX;
                    if (node.PosY < minY) minY = node.PosY;
                    if (node.PosY > maxY) maxY = node.PosY;
                }

                double graphCenterX = (minX + maxX) / 2;
                double graphCenterY = (minY + maxY) / 2;

                // Center in existing implementation was based on canvas 1000x700 center (500, 350)
                // Let's ensure it maps strictly to the viewable center
                double finalCenterX = 500;
                double finalCenterY = 350;

                double shiftX = finalCenterX - graphCenterX;
                double shiftY = finalCenterY - graphCenterY;

                foreach (var node in graph.Nodes.Values)
                {
                    node.PosX += shiftX;
                    node.PosY += shiftY;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Layout Error: {ex.Message}";
            }

            // Populate ViewModels with final positions
            foreach (var node in _graphService.CurrentGraph.Nodes.Values)
            {
                Nodes.Add(new NodeViewModel(node));
            }

            foreach (var list in _graphService.CurrentGraph.AdjacencyList.Values)
            {
                foreach (var edge in list)
                {
                    if (edge.SourceId < edge.TargetId)
                    {
                        var sourceVM = Nodes.FirstOrDefault(n => n.Model.Id == edge.SourceId);
                        var targetVM = Nodes.FirstOrDefault(n => n.Model.Id == edge.TargetId);
                        if (sourceVM != null && targetVM != null)
                        {
                            Edges.Add(new EdgeViewModel(edge, sourceVM, targetVM));
                        }
                    }
                }
            }
        }

        public ObservableCollection<AlgorithmStepResult> AlgorithmSteps { get; } = new();
        public ObservableCollection<AlgorithmStepResult> TopInfluentialNodes { get; } = new();
        public ObservableCollection<ChartDataPoint> ChartData { get; } = new();

        [ObservableProperty]
        private string _chartTitle = "Analysis Data";

        private async Task VisualizeResults(SocialNetworkAnalysis.Algorithms.Base.AlgorithmBase algorithm)
        {
            ResultText = "";
            AlgorithmSteps.Clear();
            ChartData.Clear();
            ResetVisuals();

            // Clear InfoText
            foreach (var n in Nodes) n.InfoText = null;

            switch (algorithm)
            {
                case SocialNetworkAnalysis.Algorithms.Traversal.BFSAlgorithm bfs:
                    HighlightNodes(bfs.VisitedNodes);
                    ResultText = $"BFS Traversal Order ({bfs.VisitedNodes.Count} nodes)";
                    ChartTitle = "BFS Level/Order";

                    // Visual: Simple numeric labels for order
                    for (int i = 0; i < bfs.VisitedNodes.Count; i++)
                    {
                        var id = bfs.VisitedNodes[i];
                        var node = Nodes.FirstOrDefault(n => n.Model.Id == id);
                        if (node != null)
                        {
                            node.InfoText = $"#{i + 1}"; // Order label
                            AlgorithmSteps.Add(new AlgorithmStepResult { StepNumber = i + 1, NodeId = id, NodeName = node.Model.Name, Description = "Visited" });
                            if (i < 30) ChartData.Add(new ChartDataPoint { Label = id.ToString(), Value = i + 1 });
                        }
                    }
                    break;

                case SocialNetworkAnalysis.Algorithms.Traversal.DFSAlgorithm dfs:
                    HighlightNodes(dfs.VisitedNodes);
                    ResultText = $"DFS Backtracking Trace ({dfs.VisitedNodes.Count} nodes)";
                    ChartTitle = "DFS Depth/Order";

                    // Visual: Order label, but maybe we can imply backtracking by showing re-visits if we had that data.
                    // For now, strict order.
                    for (int i = 0; i < dfs.VisitedNodes.Count; i++)
                    {
                        var id = dfs.VisitedNodes[i];
                        var node = Nodes.FirstOrDefault(n => n.Model.Id == id);
                        if (node != null)
                        {
                            node.InfoText = $"#{i + 1}";
                            AlgorithmSteps.Add(new AlgorithmStepResult { StepNumber = i + 1, NodeId = id, NodeName = node.Model.Name, Description = "Visited / Backtracked" });
                            if (i < 30) ChartData.Add(new ChartDataPoint { Label = id.ToString(), Value = i + 1 });
                        }
                    }
                    break;

                case SocialNetworkAnalysis.Algorithms.ShortestPath.DijkstraAlgorithm dijkstra:
                    ChartTitle = "Dijkstra Distances";
                    if (dijkstra.Path.Any())
                    {
                        HighlightPath(dijkstra.Path);
                        ResultText = $"Total Cost: {dijkstra.Distances[dijkstra.EndNodeId ?? 0]:F2}";

                        int step = 1;
                        foreach (var id in dijkstra.Path)
                        {
                            var node = Nodes.FirstOrDefault(n => n.Model.Id == id);
                            double dist = dijkstra.Distances.ContainsKey(id) ? dijkstra.Distances[id] : 0;
                            // Visual: Distance Label
                            if (node != null) node.InfoText = $"{dist:F1}"; // Show distance on node

                            AlgorithmSteps.Add(new AlgorithmStepResult { StepNumber = step++, NodeId = id, NodeName = node?.Model.Name ?? id.ToString(), Description = $"Acc. Dist: {dist:F2}" });
                        }

                        // Also show distances for non-path nodes (visited)?
                        // Let's show top 15 visited nodes distances in Chart
                        foreach (var d in dijkstra.Distances.OrderBy(x => x.Value).Take(20))
                        {
                            ChartData.Add(new ChartDataPoint { Label = d.Key.ToString(), Value = d.Value });
                        }
                    }
                    else ResultText = "Target unreachable.";
                    break;

                case SocialNetworkAnalysis.Algorithms.ShortestPath.AStarAlgorithm astar:
                    ChartTitle = "A* Heuristics (Est. Cost)";
                    if (astar.Path.Any())
                    {
                        HighlightPath(astar.Path);
                        ResultText = $"Total Cost: {astar.Distances[astar.EndNodeId ?? 0]:F2}";

                        int step = 1;
                        foreach (var id in astar.Path)
                        {
                            var node = Nodes.FirstOrDefault(n => n.Model.Id == id);
                            double dist = astar.Distances.ContainsKey(id) ? astar.Distances[id] : 0;

                            // Visual: Heuristic Label (f = g + h, but we have 'dist' here usually as g. 
                            // Ideally showing 'f' is better for A*. 
                            // Since we don't expose 'f' easily from this generic algo result structure without refactoring, 
                            // we show 'g' (dist). Or we can calculate H if we have EndNode.
                            // Let's just show the Cost here.
                            if (node != null) node.InfoText = $"g:{dist:F1}";

                            AlgorithmSteps.Add(new AlgorithmStepResult { StepNumber = step++, NodeId = id, NodeName = node?.Model.Name ?? id.ToString(), Description = $"Cost: {dist:F2}" });
                        }

                        foreach (var d in astar.Distances.OrderBy(x => x.Value).Take(20))
                        {
                            ChartData.Add(new ChartDataPoint { Label = d.Key.ToString(), Value = d.Value });
                        }
                    }
                    else ResultText = "Target unreachable.";
                    break;

                case SocialNetworkAnalysis.Algorithms.Analysis.DegreeCentralityAlgorithm degreeAlgo:
                    // Show Top 5 Table
                    var scores = degreeAlgo.Result.Scores.OrderByDescending(x => x.Value).Take(5).ToList();

                    var sb = new StringBuilder();
                    sb.AppendLine("Top 5 Degree Centrality:");
                    int rank = 1;
                    foreach (var score in scores)
                    {
                        var nName = Nodes.FirstOrDefault(n => n.Model.Id == score.Key)?.Name ?? $"Node {score.Key}";
                        sb.AppendLine($"{rank}. {nName}: {score.Value:F4}");

                        AlgorithmSteps.Add(new AlgorithmStepResult { StepNumber = rank, NodeId = score.Key, NodeName = nName, Description = $"Score: {score.Value:F4}" });
                        rank++;
                    }
                    ResultText = sb.ToString();
                    ChartTitle = "Top Centrality Scores";

                    foreach (var s in scores)
                    {
                        var nName = Nodes.FirstOrDefault(n => n.Model.Id == s.Key)?.Name ?? $"ID {s.Key}";
                        ChartData.Add(new ChartDataPoint { Label = nName, Value = s.Value * 100 }); // Scale for visibility
                    }
                    break;

                case SocialNetworkAnalysis.Algorithms.Coloring.WelshPowellColoring wp:
                    ResultText = $"Chromatic Number: {wp.Result.NodeColors.Values.Distinct().Count()}\nUsed Colors for Conflicts.";
                    ChartTitle = "Color Group Sizes";

                    var groups = wp.Result.NodeColors.GroupBy(x => x.Value).OrderBy(g => g.Key);
                    var maxCount = groups.Any() ? groups.Max(g => g.Count()) : 1;

                    foreach (var g in groups)
                    {
                        double height = (double)g.Count() / maxCount * 100;
                        ChartData.Add(new ChartDataPoint { Label = $"C{g.Key}", Value = height });
                    }

                    // Sequential Coloring Effect
                    // Sort nodes by Color Group (so we color Group 1, then Group 2...)
                    // Or we can color by ID order, but coloring by Group shows the algorithm step better (sets of non-adjacent nodes).
                    // Let's color by Step/Group order.

                    var sortedColors = wp.Result.NodeColors.OrderBy(x => x.Value).ThenBy(x => x.Key).ToList();

                    foreach (var kvp in sortedColors)
                    {
                        var nodeVm = Nodes.FirstOrDefault(n => n.Model.Id == kvp.Key);
                        if (nodeVm != null)
                        {
                            // Animation Delay
                            await Task.Delay(50);

                            nodeVm.Color = GetColor(kvp.Value);
                            // Visual: Show Color ID
                            nodeVm.InfoText = $"C.{kvp.Value}";

                            AlgorithmSteps.Add(new AlgorithmStepResult { StepNumber = kvp.Key, NodeId = kvp.Key, NodeName = nodeVm.Model.Name, Description = $"Assigned Group {kvp.Value}" });
                        }
                    }
                    break;

                case SocialNetworkAnalysis.Algorithms.Analysis.ClubCommunityDetectionAlgorithm clubAlgo:
                    ResultText = $"Analysis Complete: {clubAlgo.Results.Count} Communities Found.";
                    ChartTitle = "Community Sizes";
                    ChartData.Clear();

                    foreach (var res in clubAlgo.Results)
                    {
                        AlgorithmSteps.Add(res);
                        ChartData.Add(new ChartDataPoint { Label = $"C{res.NodeId}", Value = 1.0 });
                    }

                    foreach (var kvp in clubAlgo.NodeCommunityMap)
                    {
                        var nodeVm = Nodes.FirstOrDefault(n => n.Model.Id == kvp.Key);
                        if (nodeVm != null)
                        {
                            nodeVm.Color = GetColor(kvp.Value);
                            nodeVm.InfoText = $"Comm.{kvp.Value}";
                        }
                    }
                    break;
            }
        }

        private void ResetVisuals()
        {
            foreach (var n in Nodes) n.IsHighlighted = false;
            foreach (var e in Edges) e.IsHighlighted = false;
        }

        private void HighlightNodes(IEnumerable<int> nodeIds)
        {
            foreach (var id in nodeIds)
            {
                var vm = Nodes.FirstOrDefault(n => n.Model.Id == id);
                if (vm != null) vm.IsHighlighted = true;
            }
        }

        private void CalculateInfluenceScores()
        {
            if (_graphService?.CurrentGraph?.Nodes == null) return;

            // Logic: Degree Centrality (Connection Count)

            ChartTitle = "Node Influence (Degree Centrality)";
            ChartData.Clear();

            var scores = new Dictionary<int, double>();
            var nodes = _graphService.CurrentGraph.Nodes;
            var adj = _graphService.CurrentGraph.AdjacencyList;

            // Calculate Degree
            foreach (var id in nodes.Keys)
            {
                if (adj.ContainsKey(id))
                {
                    scores[id] = adj[id].Count;
                }
                else
                {
                    scores[id] = 0;
                }
            }

            var topNodes = scores.OrderByDescending(x => x.Value).Take(8).ToList();

            // Populate Table (Top 5) -> Now into TopInfluentialNodes
            TopInfluentialNodes.Clear();

            int rank = 1;
            foreach (var score in topNodes.Take(5))
            {
                var nName = nodes.ContainsKey(score.Key) ? nodes[score.Key].Name : score.Key.ToString();
                TopInfluentialNodes.Add(new AlgorithmStepResult
                {
                    StepNumber = rank++,
                    NodeId = score.Key,
                    NodeName = nName,
                    Description = $"{score.Value:F2}"
                });
            }

            // Populate Chart (Top 8)
            double maxScore = topNodes.Any() ? topNodes.Max(x => x.Value) : 1;
            if (maxScore < 1) maxScore = 1;

            foreach (var score in topNodes)
            {
                var nodeName = nodes.ContainsKey(score.Key) ? nodes[score.Key].Name : score.Key.ToString();

                // Truncate name if too long
                if (nodeName.Length > 10) nodeName = nodeName.Substring(0, 8) + "..";

                double uiHeight = (score.Value / maxScore) * 100; // Scale to 0-100 range for bar height
                if (uiHeight < 5) uiHeight = 5; // Minimal visibility

                ChartData.Add(new ChartDataPoint { Label = nodeName, Value = uiHeight });
            }
        }

        private void HighlightPath(List<int> path)
        {
            for (int i = 0; i < path.Count; i++)
            {
                var id = path[i];
                var vm = Nodes.FirstOrDefault(n => n.Model.Id == id);
                if (vm != null) vm.IsHighlighted = true;

                if (i < path.Count - 1)
                {
                    var nextId = path[i + 1];
                    var edgeVm = Edges.FirstOrDefault(e =>
                        (e.Model.SourceId == id && e.Model.TargetId == nextId) ||
                        (e.Model.SourceId == nextId && e.Model.TargetId == id));
                    if (edgeVm != null) edgeVm.IsHighlighted = true;
                }
            }
        }
    }



    public class ChartDataPoint
    {
        public string Label { get; set; }
        public double Value { get; set; }
    }
}
