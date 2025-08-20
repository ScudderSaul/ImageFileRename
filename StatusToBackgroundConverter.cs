using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ImageFileRename
{
    public class StatusToBackgroundConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = (value as string)?.ToLowerInvariant() ?? "";
            return status switch
            {
                "renamed" => Brushes.LightGreen,
                "conflict" => Brushes.LightYellow,
                var s when s.StartsWith("error") => Brushes.LightCoral,
                "skipped" => Brushes.Gainsboro,
                "pending" => Brushes.White,
                "summary" => Brushes.LightGray,
                _ => Brushes.White
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
