using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Snoop.Converters
{
    [ValueConversion(typeof(double), typeof(Thickness))]
    public class IndentToMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new Thickness((double)value, 0, 0, 0);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
