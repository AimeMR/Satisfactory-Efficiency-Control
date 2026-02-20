using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SatisfactoryManagerApp.Converters
{
    /// <summary>
    /// Converts a connection's UsageRatio (0.0–1.0+) to a SolidColorBrush:
    ///   Green  (≤ 0.75) = flowing well
    ///   Orange (≤ 0.99) = nearing capacity
    ///   Red    (> 0.99) = saturated / bottleneck
    /// </summary>
    public class FlowColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double ratio)
            {
                if (ratio > 0.99) return new SolidColorBrush(Color.FromRgb(244, 67, 54));  // Red
                if (ratio > 0.75) return new SolidColorBrush(Color.FromRgb(255, 152, 0));  // Orange
                return new SolidColorBrush(Color.FromRgb(76, 175, 80));         // Green
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Returns true if the flow value is greater than zero (port is connected and carrying flow).
    /// </summary>
    public class FlowToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is double d && d > 0;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
