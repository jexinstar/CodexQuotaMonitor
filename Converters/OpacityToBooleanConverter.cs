using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CodexQuotaMonitor.Converters
{
    public sealed class OpacityToBooleanConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            double currentValue;
            double targetValue;
            return TryConvertToDouble(value, out currentValue)
                   && TryConvertToDouble(parameter, out targetValue)
                   && Math.Abs(currentValue - targetValue) < 0.005;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            bool isChecked = value is bool && (bool)value;
            double targetValue;
            return isChecked && TryConvertToDouble(parameter, out targetValue)
                ? (object)targetValue
                : DependencyProperty.UnsetValue;
        }

        private static bool TryConvertToDouble(object value, out double result)
        {
            return double.TryParse(
                System.Convert.ToString(value, CultureInfo.InvariantCulture),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out result);
        }
    }
}
