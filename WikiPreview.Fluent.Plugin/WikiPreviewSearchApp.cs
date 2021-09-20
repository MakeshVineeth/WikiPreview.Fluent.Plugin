using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Blast.API.Core.Processes;
using Blast.API.Processes;
using Blast.Core.Interfaces;
using Blast.Core.Objects;
using Blast.Core.Results;
using Dasync.Collections;
using TextCopy;
using static WikiPreview.Fluent.Plugin.WikiPreviewSearchResult;
using static WikiPreview.Fluent.Plugin.WikiResult;
using static WikiPreview.Fluent.Plugin.ResultGenerator;

namespace WikiPreview.Fluent.Plugin
{
    internal class WikiPreviewSearchApp : ISearchApplication
    {
        public const string SearchAppName = "WikiPreview";
        public const string WikiSearchTagName = "Wiki";
        public const string UserAgentString = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
        public static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly SearchApplicationInfo _applicationInfo;
        private readonly WikiSettings _wikiSettings;

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
                DefaultSearchTags = SearchTags,
                PluginName = "Wikipedia Preview"
            };

            _applicationInfo.SettingsPage = _wikiSettings = new WikiSettings(_applicationInfo);
        }

        public SearchApplicationInfo GetApplicationInfo()
        {
            return _applicationInfo;
        }

        public ValueTask<IHandleResult> HandleSearchResult(ISearchResult searchResult)
        {
            if (searchResult is not WikiPreviewSearchResult wikiPreviewSearchResult)
                throw new InvalidCastException(nameof(WikiPreviewSearchResult));

            string displayedName = wikiPreviewSearchResult.DisplayedName;

            if (string.IsNullOrWhiteSpace(displayedName))
                return new ValueTask<IHandleResult>(new HandleResult(true, false));

            if (wikiPreviewSearchResult.SelectedOperation is WikiPreviewSearchOperation wikiPreviewSearchOperation
            )
            {
                IProcessManager managerInstance = ProcessUtils.GetManagerInstance();
                string actionUrl = wikiPreviewSearchOperation.ActionType switch
                {
                    ActionType.Wikipedia => WikiRootUrl + wikiPreviewSearchResult.Url,
                    ActionType.Wikiwand => WikiWandUrl + wikiPreviewSearchResult.Url,
                    ActionType.GoogleSearch => GoogleSearchUrl + displayedName,
                    _ => null
                };

                if (!string.IsNullOrWhiteSpace(actionUrl)) managerInstance.StartNewProcess(actionUrl);
            }
            else if (wikiPreviewSearchResult.SelectedOperation.OperationName == CopyContentsStr)
            {
                string contents = wikiPreviewSearchResult.ResultName;
                if (!string.IsNullOrWhiteSpace(contents))
                    Clipboard.SetText(contents);
            }
            else
            {
                string pageUrl = wikiPreviewSearchResult.Url;

                if (string.IsNullOrWhiteSpace(pageUrl))
                    return new ValueTask<IHandleResult>(new HandleResult(true, false));

                string wikiUrl = WikiRootUrl + wikiPreviewSearchResult.Url;
                Clipboard.SetText(wikiUrl);
            }

            return new ValueTask<IHandleResult>(new HandleResult(true, false));
        }

        public ValueTask LoadSearchApplicationAsync()
        {
            Instance.SetImageSize(_wikiSettings.ImageSize);
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<ISearchResult> SearchAsync(SearchRequest searchRequest,
            CancellationToken cancellationToken)
        {
            string searchedTag = searchRequest.SearchedTag;
            string searchedText = searchRequest.SearchedText;
            searchedText = searchedText.Trim();

            if (!string.IsNullOrWhiteSpace(searchedTag) &&
                !searchedTag.Equals(WikiSearchTagName, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(searchedText))
                yield break;

            // Wiki Namespace set to 0 for searching in main articles only.
            QueryConfiguration queryConfiguration = new()
            {
                SearchTerm = searchedText, WikiNameSpace = 0, ImageSize = _wikiSettings.ImageSize,
                ResultsCount = _wikiSettings.MaxResults, LoadImage = _wikiSettings.LoadImages
            };

            string url = GetFormattedUrl(queryConfiguration);
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(UserAgentString);

            var channel = Channel.CreateUnbounded<WikiPreviewSearchResult>();

            _ = httpClient.GetFromJsonAsync<Wiki>(url, SerializerOptions, cancellationToken).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                    _ = task.Result?.Query.Pages.ParallelForEachAsync(async entry =>
                        {
                            WikiPreviewSearchResult wikiPreviewSearchResult =
                                await Instance.GenerateSearchResult(entry.Value, searchedText);

                            if (wikiPreviewSearchResult != null)
                                await channel.Writer.WriteAsync(wikiPreviewSearchResult, CancellationToken.None)
                                    .ConfigureAwait(false);
                        }, cancellationToken)
                        .ContinueWith(_ => channel.Writer.Complete(), CancellationToken.None);
                else
                    channel.Writer.Complete();
            }, CancellationToken.None);

            await foreach (WikiPreviewSearchResult item in channel.Reader.ReadAllAsync(cancellationToken))
                yield return item;
        }

        public async ValueTask<ISearchResult> GetSearchResultForId(object searchObjectId)
        {
            string pageId = searchObjectId as string;
            if (string.IsNullOrWhiteSpace(pageId))
                return default;

            return await Instance.GenerateOnDemand(pageId);
        }
    }
}
