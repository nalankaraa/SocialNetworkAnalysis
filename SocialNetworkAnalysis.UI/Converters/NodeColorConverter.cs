using SocialNetworkAnalysis.Core.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace SocialNetworkAnalysis.UI.Converters
{
    public class NodeColorConverter : IValueConverter
    {
        public Brush? DefaultBrush { get; set; }
        public Brush? VisitedBrush { get; set; }
        public Brush? CurrentBrush { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is NodeState state)
            {
                return state switch
                {
                    NodeState.Visited => VisitedBrush ?? Brushes.Green,
                    NodeState.Current => CurrentBrush ?? Brushes.Red,
                    _ => DefaultBrush ?? Brushes.Gray
                };

            }

            return DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
