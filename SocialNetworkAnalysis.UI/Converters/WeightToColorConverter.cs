using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SocialNetworkAnalysis.UI.Converters
{
    public class WeightToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                if (text == "-") return Brushes.Transparent;

                // Try parse double
                if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double weight))
                {
                    // Gradient from Dark Blue to Bright Cyan/Purple based on weight
                    // Assuming weight range 0 to ~10 usually, or 0-1 normalized?
                    // Let's assume generic weights.

                    // Cap at some max for coloring
                    double max = 5.0; // Arbitrary cap for full brightness
                    double normalized = Math.Min(weight, max) / max;

                    // Base color #1F1F2E (Panel) -> Target #00E5FF (Cyan)
                    // Or use opacity of Cyan

                    byte alpha = (byte)(30 + (normalized * 150)); // Min 30, Max 180 alpha

                    return new SolidColorBrush(Color.FromArgb(alpha, 0, 229, 255));
                }
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
