using CommunityToolkit.Mvvm.ComponentModel;
using SocialNetworkAnalysis.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialNetworkAnalysis.UI.ViewModels
{
    public partial class EdgeViewModel : ObservableObject
    {
        public Edge Model { get; }
        public NodeViewModel Source { get; }
        public NodeViewModel Target { get; }

        public EdgeViewModel(Edge edge, NodeViewModel source, NodeViewModel target)
        {
            Model = edge;
            Source = source;
            Target = target;
        }

        [ObservableProperty]
        private bool _isHighlighted;

        public double Weight
        {
            get => Model.Weight;
            set
            {
                if (Model.Weight != value)
                {
                    Model.Weight = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
