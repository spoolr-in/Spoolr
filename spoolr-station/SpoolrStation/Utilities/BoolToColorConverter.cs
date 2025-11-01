using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SpoolrStation;

/// <summary>
/// Converts a boolean value to a color brush for the vendor availability toggle
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isAvailable)
        {
            // Green when available (online), red when not available (offline)
            return isAvailable 
                ? new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#27AE60")!) // Green
                : new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#E74C3C")!); // Red
        }
        
        // Default to red if not a boolean
        return new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#E74C3C")!);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
