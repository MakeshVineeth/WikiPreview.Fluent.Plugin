using System;
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

            string searchSuggest = "https://en.wikipedia.org/w/api.php?action=query&list=search&srsearch=" + searchedText + "&srlimit=8&format=json";

            JsonElement query = await GetJsonElementAsync(searchSuggest);
            bool status = query.TryGetProperty("search", out JsonElement searchMap);

            if (status)
            {
                ArrayEnumerator list = searchMap.EnumerateArray();

                while (list.MoveNext())
                {
                    JsonElement currentElement = list.Current;
                    string snippet = currentElement.GetProperty("snippet").ToString();

                    snippet = snippet.Replace("<span class=\"searchmatch\">", "");
                    snippet = snippet.Replace("</span>", "");
                    snippet = snippet.Replace("&quot;", "");

                    string pageID = currentElement.GetProperty("pageid").ToString();
                    string pageTitle = currentElement.GetProperty("title").ToString();

                    List<string> data = await GetPageDescription(pageID);

                    if (data.Count != 0)
                    {
                        string pageDesc = data[0];
                        string imageURL = data.Count == 2 ? data[1] : "";

                        WikiSnippet wiki = new() { SnippetText = snippet, ImageURL = imageURL, PageDesc = pageDesc, PageID = pageID, PageTitle = pageTitle };

                        BitmapImageResult bitmapImageResult;
                        if (!string.IsNullOrEmpty(wiki.ImageURL))
                        {
                            using var httpClient = new HttpClient();
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

        public static async Task<JsonElement> GetJsonElementAsync(string url)
        {
            using var httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                JsonDocument jsonDocument = JsonDocument.Parse(responseBody);

                return jsonDocument.RootElement.GetProperty("query");
            }
            else
            {
                return new JsonElement();
            }


        }

        public static async Task<List<string>> GetPageDescription(string pageID)
        {
            string url = "https://en.wikipedia.org/w/api.php?action=query&format=json&prop=extracts|pageimages&pageids=" + pageID + "&explaintext&redirects=&exintro";
            JsonElement pageParse = await GetJsonElementAsync(url);

            bool status = pageParse.TryGetProperty("pages", out JsonElement pagesMap);

            if (!status) return new List<string>();

            JsonElement pageIDJson = pagesMap.GetProperty(pageID);

            List<string> details = new();
            string desc = pageIDJson.GetProperty("extract").ToString();
            details.Add(desc);

            if (pageIDJson.TryGetProperty("thumbnail", out JsonElement thumbnail))
            {
                if (thumbnail.TryGetProperty("source", out JsonElement source))
                {
                    string img_url = source.ToString();
                    if (!string.IsNullOrEmpty(img_url))
                        details.Add(img_url);
                }

            }

            return details;
        }
    }
}
