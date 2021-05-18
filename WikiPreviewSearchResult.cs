using System.Collections.Generic;
using Blast.Core.Interfaces;
using Blast.Core.Objects;
using Blast.Core.Results;

namespace WikiPreview.Fluent.Plugin
{
    public sealed class WikiPreviewSearchResult : SearchResultBase
    {

        public WikiPreviewSearchResult(string searchAppName, BitmapImageResult bitmapImageResult, string displayedName, string resultName, string searchedText, string resultType, double score, IList<ISearchOperation> supportedOperations, ICollection<SearchTag> tags, ProcessInfo processInfo = null) : base(searchAppName, resultName, searchedText, resultType, score, supportedOperations, tags, processInfo)
        {
            DisplayedName = displayedName;
            if (bitmapImageResult != null) PreviewImage = bitmapImageResult;
        }

        protected override void OnSelectedSearchResultChanged()
        {

        }
    }
}
