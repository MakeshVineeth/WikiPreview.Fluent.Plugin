using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Blast.API.Graphics;
using Blast.Core.Interfaces;
using static WikiPreview.Fluent.Plugin.WikiPreviewSearchResult;

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
            return true;
        }

        public ValueTask<Control> CreatePreviewControl(ISearchResult searchResult)
        {
            Control control = GeneratePreview(searchResult.ResultName,
                searchResult.PreviewImage.ConvertToAvaloniaBitmap());
            return new ValueTask<Control>(control);
        }

        public PreviewBuilderDescriptor PreviewBuilderDescriptor { get; }

        private static Control GeneratePreview(string text, IBitmap bitmap)
        {
            var wikiDescription = new TextBlock
            {
                Text = text, Padding = new Thickness(0, 5, 0, 0), TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.WordEllipsis
            };

            var stackPanel = new StackPanel();
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

            stackPanel.Children.Add(imageControl);
            stackPanel.Children.Add(wikiDescription);

            var scrollViewer = new ScrollViewer
            {
                Content = stackPanel,
                Margin = new Thickness(5.0),
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden
            };

            return scrollViewer;
        }
    }
}
