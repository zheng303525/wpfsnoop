﻿using System;
using Snoop.DebugListenerTab;
using System.Windows.Data;

namespace Snoop.Converters
{
    [ValueConversion(typeof(FilterType), typeof(int))]
    public class FilterTypeToIntConverter : IValueConverter
    {
        public static readonly FilterTypeToIntConverter Default = new FilterTypeToIntConverter();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is FilterType))
                return value;

            FilterType filterType = (FilterType)value;
            return (int)filterType;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is int))
                return value;

            int intValue = (int)value;
            return (FilterType)intValue;
        }
    }
}
