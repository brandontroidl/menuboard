using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace MenuBoard.Converters;

public class ImagePathConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isVisibilityConversion = string.Equals(parameter as string, "visibility", StringComparison.OrdinalIgnoreCase);

        if (value is string relativePath && !string.IsNullOrEmpty(relativePath))
        {
            var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", relativePath);
            if (File.Exists(fullPath))
            {
                if (isVisibilityConversion)
                    return Visibility.Visible;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                return bitmap;
            }
        }
        return isVisibilityConversion ? Visibility.Collapsed : null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
