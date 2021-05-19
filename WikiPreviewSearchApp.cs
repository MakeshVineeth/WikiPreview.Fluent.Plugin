using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Blast.Core.Interfaces;
using Blast.Core.Objects;
using Blast.Core.Results;
using System.Net.Http;
using System.Drawing;
using System.IO;
using System.Net.Http.Json;
using static WikiPreviewConsole.WikiResult;

namespace WikiPreview.Fluent.Plugin
{
    class WikiPreviewSearchApp : ISearchApplication
    {
        private const string SearchAppName = "WikiPreview";
        private const string name = "Wiki";
        private readonly List<SearchTag> _searchTags;
        private readonly SearchApplicationInfo _applicationInfo;
        private readonly List<ISearchOperation> _supportedOperations;

        public WikiPreviewSearchApp()
        {

            _searchTags = new List<SearchTag>
            {
                new SearchTag
                    {Name = name, IconGlyph = "\uEDE4", Description = "Search in Wikipedia"},

            };
            _supportedOperations = new List<ISearchOperation>();
            _applicationInfo = new SearchApplicationInfo(SearchAppName,
                "This extension can search in Wikipedia", _supportedOperations)
            {
                MinimumSearchLength = 2,
                IsProcessSearchEnabled = false,
                IsProcessSearchOffline = false,
                ApplicationIconGlyph = "\uE946",
                SearchAllTime = ApplicationSearchTime.Fast,
                DefaultSearchTags = _searchTags,

            };
        }


        public SearchApplicationInfo GetApplicationInfo()
        {
            return _applicationInfo;
        }

        public ValueTask<ISearchResult> GetSearchResultForId(string serializedSearchObjectId)
        {
            return new();
        }

        public ValueTask<IHandleResult> HandleSearchResult(ISearchResult searchResult)
        {
            return new(new HandleResult(true, false));
        }

        public ValueTask LoadSearchApplicationAsync()
        {
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<ISearchResult> SearchAsync(SearchRequest searchRequest, CancellationToken cancellationToken)
        {
            string searchedTag = searchRequest.SearchedTag;
            string searchedText = searchRequest.SearchedText;
            searchedText = searchedText.Trim();
            searchedText = searchedText.Replace(' ', '_');

            if (string.IsNullOrWhiteSpace(searchedTag) && string.IsNullOrEmpty(searchedText))
            {
                yield break;
            }

            QueryConfiguration queryConfiguration = new() { SearchTerm = searchedText, WikiNameSpace = 0, ImageSize = 100, ResultsCount = 8, SentenceCount = 8 };
            string url = GetFormattedURL(queryConfiguration);
            using var httpClient = new HttpClient();
            var response = await httpClient.GetFromJsonAsync<Wiki>(url, cancellationToken);

            if (response != null)
            {
                foreach (KeyValuePair<string, PageView> entry in response.query.pages)
                {

                    string pageDesc = entry.Value.extract;
                    string pageTitle = entry.Value.title;
                    string imageURL = "";

                    if (entry.Value.thumbnail != null)
                    {
                        string img_url = entry.Value.thumbnail.source;
                        imageURL = img_url;
                    }

                    BitmapImageResult bitmapImageResult;
                    if (!string.IsNullOrEmpty(imageURL))
                    {
                        Stream stream = await httpClient.GetStreamAsync(imageURL, cancellationToken);
                        bitmapImageResult = new BitmapImageResult(new Bitmap(stream));
                    }
                    else
                    {
                        bitmapImageResult = null;
                    }

                    yield return new WikiPreviewSearchResult(SearchAppName, bitmapImageResult, pageTitle, pageDesc, searchedText, "", 2, _supportedOperations, _searchTags);
                }

            }

        }

        public static string GetFormattedURL(QueryConfiguration queryConfiguration)
        {
            return "https://en.wikipedia.org/w/api.php?action=query&generator=search&gsrnamespace=" + queryConfiguration.WikiNameSpace.ToString() + "&gsrsearch=" + queryConfiguration.SearchTerm + "&gsrlimit=" + queryConfiguration.ResultsCount.ToString() + "&prop=pageimages|extracts&exintro&explaintext&exsentences=" + queryConfiguration.SentenceCount + "&exlimit=max&pilicense=any&redirects&format=json&pithumbsize=" + queryConfiguration.ImageSize.ToString();
        }
    }
}
