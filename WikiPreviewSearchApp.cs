using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Blast.Core.Interfaces;
using Blast.Core.Objects;
using Blast.Core.Results;
using System.Net.Http;
using System.Text.Json;
using static System.Text.Json.JsonElement;
using WikiSnippetSpace;
using System.Drawing;
using System.IO;

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

            string url = "https://en.wikipedia.org/w/api.php?action=query&generator=search&gsrnamespace=0&gsrsearch="+ searchedText +"&gsrlimit=6&prop=pageimages|extracts&exintro&explaintext&exsentences=8&exlimit=max&pilicense=any&redirects&format=json&pithumbsize=100";

            using var httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {

                string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                JsonDocument jsonDocument = JsonDocument.Parse(responseBody);
                JsonElement query = jsonDocument.RootElement.GetProperty("query");
                JsonElement searchMap = query.GetProperty("pages");
                ObjectEnumerator list = searchMap.EnumerateObject();
                List<WikiSnippet> snippetslist = new();

                while (list.MoveNext())
                {

                    JsonProperty current = list.Current;
                    JsonElement currentElement = current.Value;
                    string pageDesc = currentElement.GetProperty("extract").ToString();
                    string pageID = currentElement.GetProperty("pageid").ToString();
                    string pageTitle = currentElement.GetProperty("title").ToString();
                    string imageURL = "";

                    if (currentElement.TryGetProperty("thumbnail", out JsonElement thumbnail))
                    {
                        if (thumbnail.TryGetProperty("source", out JsonElement source))
                        {
                            string img_url = source.ToString();
                            if (!string.IsNullOrEmpty(img_url))
                                imageURL = img_url;
                        }

                    }

                    WikiSnippet wiki = new() { ImageURL = imageURL, PageDesc = pageDesc, PageID = pageID, PageTitle = pageTitle };

                    BitmapImageResult bitmapImageResult;
                    if (!string.IsNullOrEmpty(wiki.ImageURL))
                    {
                        Stream stream = await httpClient.GetStreamAsync(wiki.ImageURL, cancellationToken);
                        bitmapImageResult = new BitmapImageResult(new Bitmap(stream));
                    }
                    else
                    {
                        bitmapImageResult = null;
                    }

                    yield return new WikiPreviewSearchResult(SearchAppName, bitmapImageResult, wiki.PageTitle, wiki.PageDesc, searchedText, "", 2, _supportedOperations, _searchTags);
                }

            }

        }
    }
}
