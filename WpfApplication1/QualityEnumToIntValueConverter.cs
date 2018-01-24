using System;
using System.Globalization;
using System.Windows.Data;

namespace FaceTracker
{
    class QualityEnumToIntValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)(QualityEnum)(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (QualityEnum)System.Convert.ToInt32((double)value);
        }
    }
}
