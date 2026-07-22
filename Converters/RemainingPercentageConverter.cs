using System;
using System.Globalization;
using System.Windows.Data;

namespace CodexQuotaMonitor.Converters
{
    public sealed class RemainingPercentageConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            double usedPercentage;
            if (!double.TryParse(
                System.Convert.ToString(value, CultureInfo.InvariantCulture),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out usedPercentage)
                || double.IsNaN(usedPercentage)
                || double.IsInfinity(usedPercentage))
            {
                return 0.0;
            }

            return Math.Max(0.0, Math.Min(100.0, 100.0 - usedPercentage));
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
