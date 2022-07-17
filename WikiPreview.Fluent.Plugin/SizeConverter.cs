using Avalonia.Data.Converters;
using Blast.Core.Results;
using System;
using System.Globalization;

namespace WikiPreview.Fluent.Plugin
{
    internal class SizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not BitmapImageResult bitmapImageResult)
            {
                return null;
            }

            bool param = (bool)parameter;

            if (param)
            {
                return bitmapImageResult.AvaloniaBitmap.Size.Height;
            }
            else
            {
                return bitmapImageResult.AvaloniaBitmap.Size.Width;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
