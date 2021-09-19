using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Blast.API.Core.Processes;
using Blast.API.Graphics;
using Blast.API.Processes;
using Blast.Core.Interfaces;
using TextCopy;
using static WikiPreview.Fluent.Plugin.WikiPreviewSearchResult;
using static System.Environment;
using static WikiPreview.Fluent.Plugin.WikiPreviewSearchApp;
using static WikiPreview.Fluent.Plugin.ResultGenerator;

namespace WikiPreview.Fluent.Plugin
{
    public class CustomPreview : IResultPreviewControlBuilder
    {
        private const string GoogleStr = "Search Google";
        private const string WikipediaStr = "Wikipedia";
        private const string CopyStr = "Copy Text";

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
            if (string.IsNullOrWhiteSpace(searchResult.SearchApp)) return false;

            if (searchResult.SearchApp.Equals(SearchAppName,
                StringComparison.OrdinalIgnoreCase)) return true;

            string host = searchResult.Context;
            if (string.IsNullOrWhiteSpace(host)) return false;

            bool result = Uri.TryCreate(host, UriKind.Absolute, out Uri uri)
                          && uri.Scheme == Uri.UriSchemeHttps;

            if (!result) return false;

            host = uri.Host[3..];
            return host.StartsWith("wikipedia.org") && uri.Segments.Length > 1 && uri.Fragment.Length == 0;
        }

        public ValueTask<Control> CreatePreviewControl(ISearchResult searchResult)
        {
            if (searchResult is WikiPreviewSearchResult result)
            {
                Control control = GeneratePreview(result);
                return new ValueTask<Control>(control);
            }

            string pageName = searchResult.Context.Split('/').Last();
            return GenerateElement(pageName);
        }

        public PreviewBuilderDescriptor PreviewBuilderDescriptor { get; }

        private static async ValueTask<Control> GenerateElement(string pageName)
        {
            WikiPreviewSearchResult searchResult = await Instance.GenerateOnDemand(pageName, true, false);
            Control control = GeneratePreview(searchResult);
            return control;
        }

        private static Control GeneratePreview(WikiPreviewSearchResult searchResult)
        {
            string text = searchResult.ResultName;

            // double the new lines for better reading.
            text = Regex.Replace(text, @"\r\n?|\n", NewLine + NewLine);

            // StackPanel to store image and text.
            var wikiDetails = new DockPanel();

            // creates image control.
            if (searchResult.PreviewImage is { IsEmpty: false })
            {
                Bitmap bitmap = searchResult.PreviewImage.ConvertToAvaloniaBitmap();

                if (!bitmap.Dpi.IsDefault & !bitmap.Size.IsDefault)
                {
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

                    wikiDetails.Children.Add(imageControl);
                    imageControl.SetValue(DockPanel.DockProperty, Dock.Top);
                }
            }

            // creates article content.
            var wikiDescription = new TextBlock
            {
                Text = text, Padding = new Thickness(5, 10, 5, 0), TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.WordEllipsis
            };

            wikiDetails.Children.Add(wikiDescription);

            var scrollViewer = new ScrollViewer
            {
                Content = wikiDetails,
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                Margin = new Thickness(0, 0, 0, 5)
            };

            scrollViewer.PointerEnter += ScrollViewerOnPointerEnter;
            scrollViewer.PointerLeave += ScrollViewerOnPointerLeave;

            // Create Parent Grid.
            var grid = new Grid
            {
                Margin = new Thickness(10.0, 0, 10, 10)
            };

            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.Children.Add(scrollViewer);

            Button openWiki = CreateButton(WikipediaStr, searchResult.Url);
            Button searchGoogle = CreateButton(GoogleStr, searchResult.DisplayedName);
            Button copyContents = CreateButton(CopyStr, text);

            var uniformGrid = new UniformGrid
            {
                Columns = 3,
                Rows = 0
            };

            uniformGrid.Children.Add(openWiki);
            uniformGrid.Children.Add(searchGoogle);
            uniformGrid.Children.Add(copyContents);

            // add buttons grid to the bottom row.
            Grid.SetRow(uniformGrid, 1);
            grid.Children.Add(uniformGrid);

            return grid;
        }

        private static void ScrollViewerOnPointerLeave(object sender, PointerEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
                scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        }

        private static void ScrollViewerOnPointerEnter(object sender, PointerEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
                scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        }

        private static Button CreateButton(string text, string tag)
        {
            var button = new Button
            {
                Content = text,
                Margin = new Thickness(0, 0, 5, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Tag = tag,
                [!TemplatedControl.BackgroundProperty] = new DynamicResourceExtension("TextControlBackground")
            };

            button.Click += ButtonOnClick;
            return button;
        }

        private static void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            string buttonContent = button?.Content.ToString();

            if (string.IsNullOrWhiteSpace(buttonContent)) return;

            string buttonTag = button.Tag?.ToString();

            if (string.IsNullOrWhiteSpace(buttonTag)) return;

            IProcessManager managerInstance = ProcessUtils.GetManagerInstance();
            if (buttonContent.Contains(WikipediaStr))
                managerInstance.StartNewProcess(WikiRootUrl + buttonTag);
            else if (buttonContent.Contains(GoogleStr))
                managerInstance.StartNewProcess(GoogleSearchUrl + buttonTag);
            else if (buttonContent.Contains(CopyStr)) Clipboard.SetText(buttonTag);
        }
    }
}
