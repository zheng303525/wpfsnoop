using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Snoop.Converters
{
    /// <summary>
    /// 将double类型转为Brush，通过Slider调节背景
    /// </summary>
    [ValueConversion(typeof(double), typeof(Brush))]
    public class DoubleToWhitenessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            float val = (float)(double)value;
            Color c = new Color();
            c.ScR = val;
            c.ScG = val;
            c.ScB = val;
            c.ScA = 1;
            
            return new SolidColorBrush(c);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
