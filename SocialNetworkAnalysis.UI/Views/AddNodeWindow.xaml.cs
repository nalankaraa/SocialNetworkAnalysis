using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using SocialNetworkAnalysis.Core.Models;

namespace SocialNetworkAnalysis.UI.Views
{
    public partial class AddNodeWindow : Window
    {
        public Node NewNode { get; private set; }
        public List<int> NeighborIds { get; private set; } = new();

        public AddNodeWindow(bool showNeighbors = true)
        {
            InitializeComponent();

            if (!showNeighbors)
            {
                LblNeighbors.Visibility = Visibility.Collapsed;
                TxtNeighbors.Visibility = Visibility.Collapsed;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(TxtActivity.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double activity))
            {
                MessageBox.Show("Invalid Activity value.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(TxtInteraction.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double interaction))
            {
                MessageBox.Show("Invalid Interaction value.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var neighbors = new List<int>();
            if (!string.IsNullOrWhiteSpace(TxtNeighbors.Text))
            {
                var parts = TxtNeighbors.Text.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    if (int.TryParse(p, out int id))
                    {
                        neighbors.Add(id);
                    }
                    else
                    {
                        MessageBox.Show($"Invalid Neighbor ID: {p}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
            }

            NewNode = new Node
            {
                Name = TxtName.Text,
                Club = TxtClub.Text,
                Role = TxtRole.Text,
                Activity = activity,
                Interaction = interaction,
                ConnectionCount = neighbors.Count
            };
            NeighborIds = neighbors;

            DialogResult = true;
            Close();
        }
    }
}
