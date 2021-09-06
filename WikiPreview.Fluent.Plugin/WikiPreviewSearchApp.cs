using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
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
        private const string UserAgentString = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
        private readonly SearchApplicationInfo _applicationInfo;
        private readonly JsonSerializerOptions _serializerOptions = new() { PropertyNameCaseInsensitive = true };
        private BitmapImageResult _bitmapLogo;

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
            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "WikiPreview.Fluent.Plugin.Wikipedia-logo.png";
            _bitmapLogo = new BitmapImageResult(assembly.GetManifestResourceStream(resourceName));
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

            QueryConfiguration queryConfiguration = new()
                { SearchTerm = searchedText, WikiNameSpace = 0, ImageSize = FixedImageSize, ResultsCount = 8 };
            string url = GetFormattedUrl(queryConfiguration);
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(UserAgentString);

            var channel = Channel.CreateUnbounded<WikiPreviewSearchResult>();

            _ = httpClient.GetFromJsonAsync<Wiki>(url, _serializerOptions, cancellationToken).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                    _ = task.Result?.Query.Pages.ParallelForEachAsync(async entry =>
                        {
                            WikiPreviewSearchResult wikiPreviewSearchResult =
                                await GenerateSearchResult(entry.Value, searchedText);

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

            string url = "https://en.wikipedia.org/w/api.php?action=query&prop=extracts|pageimages&pageids=" +
                         pageId +
                         "&explaintext&exintro&pilicense=any&pithumbsize=100&format=json";

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(UserAgentString);
            var wiki = await httpClient.GetFromJsonAsync<Wiki>(url, _serializerOptions);
            if (wiki == null) return default;

            Dictionary<string, PageView>.ValueCollection pages = wiki.Query.Pages.Values;
            if (pages is { Count: 0 }) return default;

            PageView pageView = pages.First();
            return await GenerateSearchResult(pageView, pageView?.Title);
        }

        private async ValueTask<WikiPreviewSearchResult> GenerateSearchResult(PageView value,
            string searchedText)
        {
            string resultName = value.Extract;
            string displayedName = value.Title;
            double score = displayedName.SearchDistanceScore(searchedText);
            string pageId = value.PageId.ToString();
            string wikiUrl = displayedName.Replace(' ', '_');
            BitmapImageResult bitmapImageResult;

            if (value.Thumbnail != null)
            {
                string imgUrl = value.Thumbnail.Source;
                using var imageClient = new HttpClient();
                imageClient.DefaultRequestHeaders.UserAgent.TryParseAdd(UserAgentString);
                Stream stream = await imageClient.GetStreamAsync(imgUrl);
                bitmapImageResult = new BitmapImageResult(new Bitmap(stream));
            }
            else
            {
                bitmapImageResult = _bitmapLogo;
            }

            return new WikiPreviewSearchResult(resultName)
            {
                Url = wikiUrl,
                PreviewImage = bitmapImageResult,
                DisplayedName = displayedName,
                SearchedText = searchedText,
                Score = score,
                SearchObjectId = pageId,
                PinUniqueId = pageId
            };
        }
    }
}
