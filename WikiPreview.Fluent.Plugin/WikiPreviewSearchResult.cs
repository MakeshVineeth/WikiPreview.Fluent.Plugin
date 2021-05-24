using System.Collections.ObjectModel;
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

        public static readonly ObservableCollection<ISearchOperation> SupportedOperationCollections
            = new()
            {
                OpenWiki,
                OpenWikiWand,
                OpenGoogle,
                new CopySearchOperation("Copy URL")
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

        public WikiPreviewSearchResult()
        {
            PinUniqueId = PageId;
            SearchObjectId = PageId;
            Tags = SearchTags;
            SupportedOperations = SupportedOperationCollections;
            IconGlyph = SearchResultIcon;
            ResultType = WikiSearchTagName;
        }

        public string Url { get; set; }
        public string PageId { get; set; }
        public override string Context => Url;

        public static string GetFormattedUrl(QueryConfiguration queryConfiguration)
        {
            return "https://en.wikipedia.org/w/api.php?action=query&generator=search&gsrnamespace=" +
                   queryConfiguration.WikiNameSpace + "&gsrsearch=" + queryConfiguration.SearchTerm + "&gsrlimit=" +
                   queryConfiguration.ResultsCount +
                   "&prop=pageimages|extracts&exintro&explaintext&pilicense=any&format=json&pithumbsize=" +
                   queryConfiguration.ImageSize;
        }

        protected override void OnSelectedSearchResultChanged()
        {
        }
    }
}
