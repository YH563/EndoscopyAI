// BoolToStyleConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EndoscopyAI.Views.SubWindows
{
    public class BoolToStyleConverter : IValueConverter
    {
        public Style TrueStyle { get; set; }
        public Style FalseStyle { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueStyle : FalseStyle;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}