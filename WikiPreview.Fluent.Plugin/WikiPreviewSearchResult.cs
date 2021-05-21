using System.Collections.ObjectModel;
using Blast.Core.Interfaces;
using Blast.Core.Results;
using static WikiPreview.Fluent.Plugin.WikiPreviewSearchOperation;

namespace WikiPreview.Fluent.Plugin
{
    public sealed class WikiPreviewSearchResult : SearchResultBase
    {

        public static readonly ObservableCollection<ISearchOperation> supportedOperations = new()
        {
            new WikiSearchOperation(),
            new WikiwandSearchOperation(),
            new GoogleSearchOperation(),
            new CopyUrlSearchOperation()
        };

        public static readonly ObservableCollection<SearchTag> searchTags = new()
        {
            new SearchTag
            { Name = WikiPreviewSearchApp.WikiSearchTagName, IconGlyph = "\uEDE4", Description = "Search in Wikipedia" },
        };

        public WikiPreviewSearchResult() : base()
        {
            PinUniqueId = PageID;
            SearchObjectId = PageID;
            Tags = searchTags;
            SupportedOperations = supportedOperations;
            ProcessInfo = null;

            if (PreviewImage == null || PreviewImage.IsEmpty)
                IconGlyph = "\uF6FA";
        }

        public string URL { get; set; }
        public string PageID { get; set; }
        public override string Context => URL;

        protected override void OnSelectedSearchResultChanged()
        {
        }
    }
}
