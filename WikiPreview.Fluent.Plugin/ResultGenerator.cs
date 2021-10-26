using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
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

        public static async ValueTask<WikiPreviewSearchResult> GenerateSearchResult(PageView value,
            string searchedText, bool loadImage = true)
        {
            string displayedName = value?.Title;
            string pageId = value?.PageId.ToString();
            if (string.IsNullOrWhiteSpace(pageId) || string.IsNullOrWhiteSpace(displayedName)) return null;

            string resultName = value.Extract;
            if (string.IsNullOrWhiteSpace(resultName))
                resultName = "Description not available for this Search Result.";

            double score = displayedName.SearchTokens(searchedText);
            string wikiUrl = displayedName.Replace(' ', '_');
            BitmapImageResult bitmapImageResult;

            if (loadImage && value.Thumbnail != null)
            {
                string imgUrl = value.Thumbnail.Source;
                using var imageClient = new HttpClient();
                imageClient.DefaultRequestHeaders.UserAgent.TryParseAdd(UserAgentString);
                Stream stream = await imageClient.GetStreamAsync(imgUrl);
#pragma warning disable CA1416
                var bitmap = new Bitmap(stream); // Wiki Images are not working with AvaloniaBitmap as of now.
#pragma warning restore CA1416
                bitmapImageResult = new BitmapImageResult(bitmap);
            }
            else
            {
                bitmapImageResult = WikipediaLogo;
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
            return await GenerateSearchResult(pageView, pageView?.Title, loadImage);
        }
    }
}
