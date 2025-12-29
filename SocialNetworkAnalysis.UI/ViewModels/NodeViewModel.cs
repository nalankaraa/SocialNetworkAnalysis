using CommunityToolkit.Mvvm.ComponentModel;
using SocialNetworkAnalysis.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialNetworkAnalysis.UI.ViewModels
{
    public partial class NodeViewModel : ObservableObject
    {
        public Node Model { get; }

        [ObservableProperty]
        private bool _isHighlighted;

        [ObservableProperty]
        private string _color;

        [ObservableProperty]
        private string _infoText; // For displaying distances, heuristics, etc. 

        public System.Collections.ObjectModel.ObservableCollection<int> NeighborIds { get; } = new();

        partial void OnColorChanged(string value)
        {
            // Optional: validation or debug
        }

        public NodeViewModel(Node node)
        {
            Model = node;
            _color = DetermineColorByRole(node.Role);
        }

        private string DetermineColorByRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role)) return "#3498DB"; // Default Blue

            var r = role.Trim().ToLower(System.Globalization.CultureInfo.InvariantCulture);

            // Specific Check for 'Baskan Yrd' or 'Vice' first to avoid partial match with 'Baskan'
            if (r.Contains("yrd") || r.Contains("vice"))
                return "#E67E22"; // Orange (Vice President)

            // Check for 'Birim Baskani' or 'Unit Head'
            if (r.Contains("birim") && (r.Contains("baskan") || r.Contains("head")))
                return "#FFD740"; // Yellow (Unit Head)

            // Check for 'Baskan' (President) - ensuring complete match or main role
            // Since we handled VP and Unit Head above, 'baskan' here likely means the main President
            if (r.Contains("baskan") || r.Contains("president"))
                return "#E74C3C"; // Red (President)

            if (r.Contains("uye") || r.Contains("member"))
                return "#3498DB"; // Blue (Member)

            return "#95A5A6"; // Gray (Default/Unknown)
        }

        public void ResetColor()
        {
            Color = DetermineColorByRole(Model.Role);
        }

        public int Id => Model.Id;

        public string Name
        {
            get => Model.Name;
            set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
        }

        public string Club
        {
            get => Model.Club;
            set => SetProperty(Model.Club, value, Model, (m, v) => m.Club = v);
        }

        public string Role
        {
            get => Model.Role;
            set
            {
                if (SetProperty(Model.Role, value, Model, (m, v) => m.Role = v))
                {
                    Color = DetermineColorByRole(value);
                }
            }
        }

        public double Activity
        {
            get => Model.Activity;
            set => SetProperty(Model.Activity, value, Model, (m, v) => m.Activity = v);
        }

        public double Interaction
        {
            get => Model.Interaction;
            set => SetProperty(Model.Interaction, value, Model, (m, v) => m.Interaction = v);
        }

        public int ConnectionCount
        {
            get => Model.ConnectionCount;
            set => SetProperty(Model.ConnectionCount, value, Model, (m, v) => m.ConnectionCount = v);
        }

        public double X
        {
            get => Model.PosX;
            set => SetProperty(Model.PosX, value, Model, (m, v) => m.PosX = v);
        }

        public double Y
        {
            get => Model.PosY;
            set => SetProperty(Model.PosY, value, Model, (m, v) => m.PosY = v);
        }
    }

}
