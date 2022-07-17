using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Blast.API.Core.UI;
using Blast.API.Search;
using Blast.Core.Results;
using static WikiPreview.Fluent.Plugin.WikiPreviewSearchApp;
using static WikiPreview.Fluent.Plugin.WikiResult;

namespace WikiPreview.Fluent.Plugin
{
    /// <summary>
    ///     A static class that stores the common methods to generate search results.
    ///     Also stores and initializes WikipediaLogo which will be used as placeholder for Wiki Results with no images.
    ///     Stores the Image Size (set in FS Plugin Settings) and is used across multiple places.
    /// </summary>
    public static class ResultGenerator
    {
        private static readonly BitmapImageResult WikipediaLogo;
        private static int _imageSizePrefs;

        static ResultGenerator()
        {
            var assembly = Assembly.GetExecutingAssembly();
            WikipediaLogo =
                new BitmapImageResult(
                    assembly.GetManifestResourceStream("WikiPreview.Fluent.Plugin.Wikipedia-logo.png"));
        }

        public static int GetImageSizePrefs()
        {
            return _imageSizePrefs;
        }

        public static void SetImageSizePrefs(int size)
        {
            _imageSizePrefs = size;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public static ValueTask<WikiPreviewSearchResult> GenerateSearchResult(PageView value,
            string searchedText, CancellationToken cancellationToken, bool loadImage = true)
        {
            string displayedName = value?.Title;
            string pageId = value?.PageId.ToString();

            if (string.IsNullOrWhiteSpace(pageId) || string.IsNullOrWhiteSpace(displayedName))
            {
                return new ValueTask<WikiPreviewSearchResult>();
            }

            string resultName = value.Extract?.Trim();
            if (string.IsNullOrWhiteSpace(resultName))
                resultName = "Description not available for this Search Result.";
            else
                resultName = Regex.Replace(resultName, @"[\r\n]+", "\n");

            double score = displayedName.SearchTokens(searchedText);
            string wikiUrl = displayedName.Replace(' ', '_');
            string imgUrl = string.Empty;

            if (loadImage && value.Thumbnail != null)
            {
                imgUrl = value.Thumbnail.Source;
            }

            WikiPreviewSearchResult searchResult = new(resultName)
            {
                Url = wikiUrl,
                DisplayedName = displayedName,
                SearchedText = searchedText,
                Score = score,
                SearchObjectId = pageId,
                PinUniqueId = pageId,
                UseIconGlyph = false,
                PreviewImage = WikipediaLogo,
                ImageUrl = imgUrl
            };

            // Set Additional Info
            using var reader = new StringReader(resultName);
            string first_line = reader.ReadLine();
            if (!string.IsNullOrWhiteSpace(first_line))
            {
                searchResult.AdditionalInformation = first_line;
            }

            _ = Task.Run(async () =>
            {
                BitmapImageResult bitmapImageResult = null;

                if (!string.IsNullOrWhiteSpace(imgUrl))
                {
                    using var imageClient = new HttpClient();
                    imageClient.DefaultRequestHeaders.UserAgent.TryParseAdd(UserAgentString);

                    await imageClient.GetStreamAsync(imgUrl).ContinueWith(task =>
                    {
                        if (!task.IsCompletedSuccessfully) return;
                        var bitmap =
                            new Bitmap(task.Result); // Wiki Images are not working with AvaloniaBitmap as of now.

                        if (!bitmap.Size.IsEmpty)
                            bitmapImageResult = new BitmapImageResult(bitmap);
                    }, cancellationToken);
                }

                void UpdatePreviewImage()
                {
                    searchResult.PreviewImage = bitmapImageResult;
                }

                if (bitmapImageResult != null && !bitmapImageResult.IsEmpty)
                {
                    UiUtilities.UiDispatcher.Post(UpdatePreviewImage);
                }
            }, cancellationToken);

            return new ValueTask<WikiPreviewSearchResult>(searchResult);
        }

        public static async ValueTask<WikiPreviewSearchResult> GenerateOnDemand(string searchId,
            bool isCustomPreview = false,
            bool loadImage = true)
        {
            if (string.IsNullOrWhiteSpace(searchId))
                return default;

            string searchType = isCustomPreview ? "titles=" : "pageids=";

            string url = "https://en.wikipedia.org/w/api.php?action=query&prop=extracts|pageimages&" + searchType +
                         searchId +
                         "&explaintext&exintro&pilicense=any&pithumbsize=100&format=json";

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(UserAgentString);
            var wiki = await httpClient.GetFromJsonAsync<Wiki>(url, SerializerOptions);

            Dictionary<string, PageView>.ValueCollection pages = wiki?.Query?.Pages?.Values;
            if (pages is { Count: 0 }) return default;

            PageView pageView = pages?.First();
            return await GenerateSearchResult(pageView, pageView?.Title, CancellationToken.None, loadImage);
        }
    }
}
