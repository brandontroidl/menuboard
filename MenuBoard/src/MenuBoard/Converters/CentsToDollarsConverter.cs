using System.Globalization;
using System.Windows.Data;

namespace MenuBoard.Converters;

public class CentsToDollarsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int cents)
            return $"${cents / 100.0:F2}";
        return "$0.00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            s = s.Replace("$", "").Replace(",", "").Trim();
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var dollars) && dollars >= 0)
                return (int)Math.Round(dollars * 100);
        }
        // Unparseable input: keep the item's existing price instead of
        // silently resetting it to $0.00.
        return System.Windows.Data.Binding.DoNothing;
    }
}
