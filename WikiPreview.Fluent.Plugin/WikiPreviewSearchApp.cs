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
using System.Threading.Channels;
using Dasync.Collections;
using static WikiPreview.Fluent.Plugin.WikiPreviewSearchOperation;
using System;
using Blast.API.Core.Processes;
using Blast.API.Processes;
using System.Text.Json;
using Blast.API.Search;

namespace WikiPreview.Fluent.Plugin
{
    class WikiPreviewSearchApp : ISearchApplication
    {
        public static readonly string SearchAppName = "WikiPreview";
        public static readonly string WikiSearchTagName = "Wiki";
        private readonly string wikiRootUrl = "https://en.wikipedia.org/wiki/";
        private readonly SearchApplicationInfo _applicationInfo;

        public WikiPreviewSearchApp()
        {

            _applicationInfo = new SearchApplicationInfo(SearchAppName,
                "This extension can search in Wikipedia", WikiPreviewSearchResult.supportedOperations)
            {
                MinimumSearchLength = 2,
                IsProcessSearchEnabled = false,
                IsProcessSearchOffline = false,
                ApplicationIconGlyph = "\uE946",
                SearchAllTime = ApplicationSearchTime.Fast,
                DefaultSearchTags = WikiPreviewSearchResult.searchTags,
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
            ISearchOperation selectedOperation = searchResult.SelectedOperation;
            Type selectedOperationType = selectedOperation.GetType();
            IProcessManager managerInstance = ProcessUtils.GetManagerInstance();

            switch (selectedOperationType)
            {
                case var type when type == typeof(WikiSearchOperation):
                    {
                        string wikiUrl = wikiRootUrl + searchResult.DisplayedName;
                        managerInstance.StartNewProcess(wikiUrl);
                        return new ValueTask<IHandleResult>(new HandleResult(true, false));
                    }

                case var type when type == typeof(WikiwandSearchOperation):
                    {
                        string url = "https://www.wikiwand.com/en/" + searchResult.DisplayedName;
                        managerInstance.StartNewProcess(url);
                        return new ValueTask<IHandleResult>(new HandleResult(true, false));
                    }

                case var type when type == typeof(CopyUrlSearchOperation):
                    {
                        string wikiUrl = wikiRootUrl + searchResult.DisplayedName;
                        TextCopy.Clipboard.SetText(wikiUrl);
                        return new ValueTask<IHandleResult>(new HandleResult(true, false));
                    }

                case var type when type == typeof(GoogleSearchOperation):
                    {
                        string url = "https://www.google.com/search?q=" + searchResult.DisplayedName;
                        managerInstance.StartNewProcess(url);
                        return new ValueTask<IHandleResult>(new HandleResult(true, false));
                    }
            }

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

            if (string.IsNullOrWhiteSpace(searchedTag) && !searchedTag.Equals(WikiSearchTagName, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(searchedText))
            {
                yield break;
            }

            QueryConfiguration queryConfiguration = new() { SearchTerm = searchedText, WikiNameSpace = 0, ImageSize = 100, ResultsCount = 8, SentenceCount = 8 };
            string url = GetFormattedURL(queryConfiguration);
            using var httpClient = new HttpClient();
            Channel<WikiPreviewSearchResult> channel = Channel.CreateUnbounded<WikiPreviewSearchResult>();

            JsonSerializerOptions serializerOptions = new() { PropertyNameCaseInsensitive = true };
            _ = httpClient.GetFromJsonAsync<Wiki>(url, serializerOptions, cancellationToken).ContinueWith(task =>
             {
                 if (task.IsCompletedSuccessfully && task.Result != null)
                 {
                     _ = task.Result.Query.Pages.ParallelForEachAsync(async entry =>
                     {
                         string resultName = entry.Value.Extract;
                         string displayedName = entry.Value.Title;
                         double score = displayedName.SearchDistanceScore(searchedText);
                         string pageID = entry.Value.PageId.ToString();
                         string url = wikiRootUrl + displayedName;
                         string resultType = "";
                         BitmapImageResult bitmapImageResult;

                         if (entry.Value.Thumbnail != null)
                         {
                             string img_url = entry.Value.Thumbnail.Source;
                             using var imageClient = new HttpClient();
                             Stream stream = await imageClient.GetStreamAsync(img_url, cancellationToken);
                             bitmapImageResult = new BitmapImageResult(new Bitmap(stream));
                         }
                         else
                         {
                             bitmapImageResult = new BitmapImageResult(); // create empty if no image source.
                         }

                         WikiPreviewSearchResult wikiPreviewSearchResult = new() { URL = url, PreviewImage = bitmapImageResult, DisplayedName = displayedName, ResultName = resultName, SearchedText = searchedText, ResultType = resultType, Score = score, PageID = pageID };
                         await channel.Writer.WriteAsync(wikiPreviewSearchResult);
                     }, maxDegreeOfParallelism: 0, cancellationToken).ContinueWith(_ => channel.Writer.Complete());
                 }

                 else
                 {
                     channel.Writer.Complete();
                 }
             }, cancellationToken);

            await foreach (WikiPreviewSearchResult item in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return item;
            }
        }

        public static string GetFormattedURL(QueryConfiguration queryConfiguration)
        {
            return "https://en.wikipedia.org/w/api.php?action=query&generator=search&gsrnamespace=" + queryConfiguration.WikiNameSpace + "&gsrsearch=" + queryConfiguration.SearchTerm + "&gsrlimit=" + queryConfiguration.ResultsCount + "&prop=pageimages|extracts&exintro&explaintext&exsentences=" + queryConfiguration.SentenceCount + "&exlimit=max&pilicense=any&redirects&format=json&pithumbsize=" + queryConfiguration.ImageSize;
        }
    }
}
