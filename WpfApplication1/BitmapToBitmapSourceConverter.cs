using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace FaceTracker
{
    class BitmapToBitmapSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bitmap = value as Bitmap;

            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                 bitmap.GetHbitmap(),
                                 IntPtr.Zero,
                                 System.Windows.Int32Rect.Empty,
                                 BitmapSizeOptions.FromWidthAndHeight(bitmap.Width, bitmap.Height));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
