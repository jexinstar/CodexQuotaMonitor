using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CodexQuotaMonitor.Converters
{
    public sealed class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return value != null
                   && parameter != null
                   && value.Equals(parameter);
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return value is bool
                   && (bool)value
                ? parameter
                : DependencyProperty.UnsetValue;
        }
    }
}
