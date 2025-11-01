using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SpoolrStation.Converters
{
    /// <summary>
    /// Converter that converts hex color strings to SolidColorBrush
    /// </summary>
    public class StringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorString)
            {
                try
                {
                    // Handle both hex colors (#FF0000) and named colors (Red)
                    var color = (WpfColor)WpfColorConverter.ConvertFromString(colorString);
                    return new SolidColorBrush(color);
                }
                catch
                {
                    // Return a default brush if conversion fails
                    return new SolidColorBrush(Colors.Gray);
                }
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color.ToString();
            }
            
            return "#808080"; // Default gray
        }
    }
}
