using System;
using System.Globalization;
using System.Windows.Data;

namespace FaceTracker
{
    class QualityEnumToStringValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            QualityEnum quality = (QualityEnum)(value);
            return quality.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
