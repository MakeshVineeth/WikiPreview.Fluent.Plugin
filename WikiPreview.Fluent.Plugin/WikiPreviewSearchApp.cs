using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Blast.API.Core.Processes;
using Blast.API.Processes;
using Blast.API.Search;
using Blast.Core.Interfaces;
using Blast.Core.Objects;
using Blast.Core.Results;
using Dasync.Collections;
using TextCopy;
using static WikiPreview.Fluent.Plugin.WikiPreviewSearchResult;
using static WikiPreview.Fluent.Plugin.WikiResult;

namespace WikiPreview.Fluent.Plugin
{
    internal class WikiPreviewSearchApp : ISearchApplication
    {
        private const string SearchAppName = "WikiPreview";
        public const string WikiSearchTagName = "Wiki";
        private readonly SearchApplicationInfo _applicationInfo;
        private readonly JsonSerializerOptions _serializerOptions = new() {PropertyNameCaseInsensitive = true};

        public WikiPreviewSearchApp()
        {
            _applicationInfo = new SearchApplicationInfo(SearchAppName,
                TagDescription, SupportedOperationCollections)
            {
                MinimumSearchLength = 2,
                IsProcessSearchEnabled = false,
                IsProcessSearchOffline = false,
                SearchTagOnly = true,
                ApplicationIconGlyph = SearchResultIcon,
                SearchAllTime = ApplicationSearchTime.Fast,
                DefaultSearchTags = SearchTags
            };
        }

        public SearchApplicationInfo GetApplicationInfo()
        {
            return _applicationInfo;
        }

        public ValueTask<IHandleResult> HandleSearchResult(ISearchResult searchResult)
        {
            if (searchResult is not WikiPreviewSearchResult wikiPreviewSearchResult)
                throw new InvalidCastException(nameof(WikiPreviewSearchResult));

            string displayedName = searchResult.DisplayedName;
            if (string.IsNullOrWhiteSpace(displayedName))
                return new ValueTask<IHandleResult>(new HandleResult(true, false));

            if (wikiPreviewSearchResult.SelectedOperation is WikiPreviewSearchOperation wikiPreviewSearchOperation
            )
            {
                IProcessManager managerInstance = ProcessUtils.GetManagerInstance();
                string actionUrl = wikiPreviewSearchOperation.ActionType switch
                {
                    ActionType.Wikipedia => WikiRootUrl + displayedName,
                    ActionType.Wikiwand => WikiWandUrl + displayedName,
                    ActionType.GoogleSearch => GoogleSearchUrl + displayedName,
                    _ => null
                };

                if (!string.IsNullOrWhiteSpace(actionUrl)) managerInstance.StartNewProcess(actionUrl);
            }
            else
            {
                string wikiUrl = WikiRootUrl + displayedName;
                Clipboard.SetText(wikiUrl);
            }

            return new ValueTask<IHandleResult>(new HandleResult(true, false));
        }

        public ValueTask LoadSearchApplicationAsync()
        {
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<ISearchResult> SearchAsync(SearchRequest searchRequest,
            CancellationToken cancellationToken)
        {
            string searchedTag = searchRequest.SearchedTag;
            string searchedText = searchRequest.SearchedText;
            searchedText = searchedText.Trim();

            if (string.IsNullOrWhiteSpace(searchedTag) ||
                !searchedTag.Equals(WikiSearchTagName, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(searchedText))
                yield break;

            QueryConfiguration queryConfiguration = new()
                {SearchTerm = searchedText, WikiNameSpace = 0, ImageSize = 100, ResultsCount = 8};
            string url = GetFormattedUrl(queryConfiguration);
            using var httpClient = new HttpClient();

            var channel = Channel.CreateUnbounded<WikiPreviewSearchResult>();

            _ = httpClient.GetFromJsonAsync<Wiki>(url, _serializerOptions, cancellationToken).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                    _ = task.Result?.Query.Pages.ParallelForEachAsync(async entry =>
                    {
                        (_, PageView value) = entry;
                        string resultName = value.Extract;
                        string displayedName = value.Title;
                        double score = displayedName.SearchDistanceScore(searchedText);
                        string pageId = value.PageId.ToString();
                        string wikiUrl = WikiRootUrl + displayedName;
                        BitmapImageResult bitmapImageResult;

                        string additionalInfo = "";
                        if (!string.IsNullOrWhiteSpace(resultName))
                        {
                            using var reader = new StringReader(resultName);
                            additionalInfo = await reader.ReadLineAsync().ConfigureAwait(false) ?? resultName;
                        }

                        if (value.Thumbnail != null)
                        {
                            string imgUrl = value.Thumbnail.Source;
                            using var imageClient = new HttpClient();
                            Stream stream = await imageClient.GetStreamAsync(imgUrl, cancellationToken)
                                .ConfigureAwait(false);
                            bitmapImageResult = new BitmapImageResult(new Bitmap(stream));
                        }
                        else
                        {
                            bitmapImageResult = new BitmapImageResult(); // create empty if no image source.
                        }

                        WikiPreviewSearchResult wikiPreviewSearchResult = new()
                        {
                            Url = wikiUrl,
                            PreviewImage = bitmapImageResult,
                            DisplayedName = displayedName,
                            ResultName = resultName,
                            SearchedText = searchedText,
                            Score = score,
                            PageId = pageId,
                            AdditionalInformation = additionalInfo
                        };
                        await channel.Writer.WriteAsync(wikiPreviewSearchResult).ConfigureAwait(false);
                    }, cancellationToken).ContinueWith(_ => channel.Writer.Complete());
                else
                    channel.Writer.Complete();
            });

            await foreach (WikiPreviewSearchResult item in channel.Reader.ReadAllAsync(cancellationToken))
                yield return item;
        }

        public ValueTask<ISearchResult> GetSearchResultForId(string serializedSearchObjectId)
        {
            return new();
        }
    }
}
