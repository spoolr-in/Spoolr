using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SpoolrStation.Converters
{
    /// <summary>
    /// Converter that returns Visible when count is 0, Collapsed when count > 0
    /// Used for showing empty state messages
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                // Show empty state message when count is 0
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            if (value is null)
            {
                return Visibility.Visible;
            }

            // Default to hidden if not a valid count
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("CountToVisibilityConverter is one-way only");
        }
    }
}