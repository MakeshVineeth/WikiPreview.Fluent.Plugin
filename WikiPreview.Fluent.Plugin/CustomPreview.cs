using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Blast.API.Graphics;
using Blast.Core.Interfaces;
using static WikiPreview.Fluent.Plugin.WikiPreviewSearchResult;
using static System.Environment;

namespace WikiPreview.Fluent.Plugin
{
    public class CustomPreview : IResultPreviewControlBuilder
    {
        public CustomPreview()
        {
            PreviewBuilderDescriptor = new PreviewBuilderDescriptor
            {
                Name = "Wikipedia Preview",
                Description = "Displays the Wikipedia Article Information within Fluent Search.",
                ShowPreviewAutomatically = true
            };
        }

        public bool CanBuildPreviewForResult(ISearchResult searchResult)
        {
            return !string.IsNullOrWhiteSpace(searchResult.SearchApp) &&
                   searchResult.SearchApp.Equals(WikiPreviewSearchApp.SearchAppName,
                       StringComparison.OrdinalIgnoreCase);
        }

        public ValueTask<Control> CreatePreviewControl(ISearchResult searchResult)
        {
            Control control = GeneratePreview(searchResult.DisplayedName, searchResult.ResultName,
                searchResult.PreviewImage.ConvertToAvaloniaBitmap());
            return new ValueTask<Control>(control);
        }

        public PreviewBuilderDescriptor PreviewBuilderDescriptor { get; }

        private static Control GeneratePreview(string title, string text, IBitmap bitmap)
        {
            // double the new lines for better reading.
            text = Regex.Replace(text, @"\r\n?|\n", NewLine + NewLine);

            // creates heading.
            var header = new TextBlock
            {
                Text = title,
                FontWeight = FontWeight.SemiBold,
                FontSize = 20.0
            };

            // creates separator
            var defaultTheme = new UISettings();
            string uiTheme = defaultTheme.GetColorValue(UIColorType.Foreground).ToString();
            Color lineColor = Color.Parse(uiTheme);

            var separator = new Border
            {
                BorderThickness = new Thickness(0.6),
                BorderBrush = new SolidColorBrush(lineColor),
                Margin = new Thickness(0, 8)
            };

            // creates article content.
            var wikiDescription = new TextBlock
            {
                Text = text, Padding = new Thickness(0, 10, 0, 0), TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.WordEllipsis
            };

            // creates image control.
            var imageControl = new Border
            {
                Background = new ImageBrush(bitmap)
                {
                    Stretch = Stretch.UniformToFill
                },
                CornerRadius = new CornerRadius(5.0),
                BorderThickness = new Thickness(5.0),
                Height = bitmap.Size.Height,
                Width = bitmap.Size.Width,
                MaxHeight = FixedImageSize,
                MaxWidth = FixedImageSize
            };

            var stackPanel = new StackPanel();
            stackPanel.Children.Add(header);
            stackPanel.Children.Add(separator);
            stackPanel.Children.Add(imageControl);
            stackPanel.Children.Add(wikiDescription);

            var scrollViewer = new ScrollViewer
            {
                Content = stackPanel,
                Margin = new Thickness(10.0, 0),
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden
            };

            return scrollViewer;
        }
    }
}
