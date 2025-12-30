using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using SocialNetworkAnalysis.Core.Models;

namespace SocialNetworkAnalysis.UI.Views
{
    public partial class MatrixWindow : Window
    {
        public MatrixWindow(Graph graph)
        {
            InitializeComponent();
            MatrixGrid.AutoGeneratingColumn += MatrixGrid_AutoGeneratingColumn;
            LoadMatrix(graph);
        }

        private void MatrixGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // Apply custom cell style for heatmap
            if (e.PropertyName == "Node")
            {
                // Styling for the Row Header Column (First Column)
                var style = new Style(typeof(DataGridCell));
                style.Setters.Add(new Setter(DataGridCell.ForegroundProperty, Brushes.White));
                style.Setters.Add(new Setter(DataGridCell.FontWeightProperty, FontWeights.Bold));
                style.Setters.Add(new Setter(DataGridCell.BackgroundProperty, new SolidColorBrush(Color.FromRgb(31, 31, 46)))); // Darker bg
                style.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, new Thickness(0, 0, 1, 0)));
                style.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(43, 43, 61))));
                e.Column.CellStyle = style;
                e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);

                // Freeze it
                // Note: FrozenColumnCount logic in Loaded event might be redundant or conflicting if we do it here, 
                // but setting property on column is better.
                // However, freezing must be done on the DataGrid.FrozenColumnCount, not per column.
            }
            else
            {
                // Styling for Value Columns
                var style = new Style(typeof(DataGridCell));

                // 1. Alignment and Layout
                style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
                style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(5)));
                style.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, new Thickness(0)));

                // 2. Heatmap Background
                // We need to bind to the value of this column. 
                // e.PropertyName is the column name. 
                // For DataTable binding, the path is the column name.
                // We use [ColumnName] syntax for complex names in Binding string, or just passing path.

                var binding = new Binding($"[{e.PropertyName}]");
                binding.Converter = (IValueConverter)this.Resources["WeightToColorConv"];

                style.Setters.Add(new Setter(DataGridCell.BackgroundProperty, binding));

                e.Column.CellStyle = style;
                e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star); // Distribute evenly? Or Auto.
                                                                                     // Let's stick to Auto or specific width for matrix look
                e.Column.Width = new DataGridLength(80);
            }
        }

        private void LoadMatrix(Graph graph)
        {
            if (graph == null || graph.Nodes.Count == 0) return;

            var table = new DataTable();
            var nodes = graph.Nodes.Values.OrderBy(n => n.Id).ToList();

            // Create columns
            // First column for Row Headers (Node Names)
            table.Columns.Add("Node", typeof(string));

            foreach (var node in nodes)
            {
                // Columns: Show only Node ID
                table.Columns.Add(node.Id.ToString(), typeof(string));
            }

            // Populate rows
            foreach (var rowNode in nodes)
            {
                var row = table.NewRow();
                // Row Header: Show only Name
                row["Node"] = rowNode.Name;

                foreach (var colNode in nodes)
                {
                    double weight = 0;
                    if (graph.AdjacencyList.ContainsKey(rowNode.Id))
                    {
                        var edge = graph.AdjacencyList[rowNode.Id].FirstOrDefault(e => e.TargetId == colNode.Id);
                        if (edge != null)
                        {
                            weight = edge.Weight;
                        }
                    }

                    // Column Name matches the ID
                    string colName = colNode.Id.ToString();
                    row[colName] = weight > 0 ? weight.ToString("0.##") : "-";
                }
                table.Rows.Add(row);
            }

            MatrixGrid.ItemsSource = table.DefaultView;

            // Logic to freeze first column is simpler in Loaded or here if we set FrozenColumnCount property
            if (MatrixGrid.Columns.Count > 0)
            {
                MatrixGrid.FrozenColumnCount = 1;
            }
        }
    }
}
