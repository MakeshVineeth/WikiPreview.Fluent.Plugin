using System.Collections.ObjectModel;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Blast.API.Graphics;
using Blast.API.Search.SearchOperations;
using Blast.Core.Interfaces;
using Blast.Core.Results;
using static WikiPreview.Fluent.Plugin.WikiPreviewSearchApp;
using static WikiPreview.Fluent.Plugin.WikiPreviewSearchOperation;

namespace WikiPreview.Fluent.Plugin
{
    public sealed class WikiPreviewSearchResult : SearchResultBase
    {
        public const string WikiRootUrl = "https://en.wikipedia.org/wiki/";
        public const string WikiWandUrl = "https://www.wikiwand.com/en/";
        public const string GoogleSearchUrl = "https://www.google.com/search?q=";
        public const string SearchResultIcon = "\uEDE4";
        public const string TagDescription = "Search in Wikipedia";
        public const string CopyContentsStr = "Copy Contents";
        public const int FixedImageSize = 120;

        public static readonly ObservableCollection<ISearchOperation> SupportedOperationCollections
            = new()
            {
                OpenWiki,
                OpenWikiWand,
                OpenGoogle,
                new CopySearchOperation("Copy URL") { Description = "Copies the Wikipedia Page URL to Clipboard." },
                new CopySearchOperation("Copy Contents")
                    { Description = "Copy the Contents of the Result.", KeyGesture = new KeyGesture(Key.None) }
            };

        public static readonly ObservableCollection<SearchTag> SearchTags = new()
        {
            new SearchTag
            {
                Name = WikiSearchTagName,
                IconGlyph = SearchResultIcon,
                Description = TagDescription
            }
        };

        public WikiPreviewSearchResult(string resultName)
        {
            Tags = SearchTags;
            SupportedOperations = SupportedOperationCollections;
            IconGlyph = SearchResultIcon;
            ResultType = WikiSearchTagName;
            WikiText = resultName;
        }

        public string WikiText { get; }

        public string Url { get; set; }
        public override string Context => WikiRootUrl + Url;

        public override void OnFocusLoad(ISearchResult old, bool hasUserInteracted)
        {
            UseCustomControl = true;
            var wikiDescription = new TextBlock
            {
                Text = WikiText, Padding = new Thickness(0, 5, 0, 0), TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.WordEllipsis
            };

            var stackPanel = new StackPanel();
            Bitmap bitmap = PreviewImage.ConvertToAvaloniaBitmap();
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
                MaxHeight = 200,
                Margin = new Thickness(5.0),
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden
            };

            CustomControl = scrollViewer;
        }

        public static string GetFormattedUrl(QueryConfiguration queryConfiguration)
        {
            var builder =
                new StringBuilder("https://en.wikipedia.org/w/api.php?action=query&generator=search&gsrnamespace=");
            builder.Append(queryConfiguration.WikiNameSpace);

            builder.Append("&gsrsearch=");
            builder.Append(queryConfiguration.SearchTerm);

            builder.Append("&gsrlimit=");
            builder.Append(queryConfiguration.ResultsCount);

            builder.Append("&prop=pageimages|extracts&exintro&explaintext&pilicense=any&format=json&pithumbsize=");
            builder.Append(queryConfiguration.ImageSize);

            return builder.ToString();
        }

        protected override void OnSelectedSearchResultChanged()
        {
        }
    }
}
