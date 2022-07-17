﻿using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Blast.Core.Results;
using System;
using System.Globalization;

namespace WikiPreview.Fluent.Plugin
{
    internal class BackgroundConverter : IValueConverter
    {
        public static IBrush GetBackground(BitmapImageResult imageResult)
        {
            BackgroundConverter converter = new();
            return converter.Convert(imageResult, null, null, CultureInfo.CurrentCulture) as IBrush;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not BitmapImageResult imageResult)
            {
                return null;
            }

            IBrush brush = new ImageBrush
            {
                Stretch = Stretch.UniformToFill,
                Source = imageResult.AvaloniaBitmap
            };

            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ImageBrush brush)
            {
                return null;
            }

            BitmapImageResult bitmapImageResult = new()
            {
                AvaloniaBitmap = (Bitmap)brush.Source
            };

            return bitmapImageResult;
        }
    }
}
