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
    public static class ResultGenerator
    {
        private static readonly BitmapImageResult BitmapLogo;
        private static int _imageSize;

        static ResultGenerator()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = "WikiPreview.Fluent.Plugin.Wikipedia-logo.png";
            BitmapLogo = new BitmapImageResult(assembly.GetManifestResourceStream(resourceName));
        }

        public static int GetWikiImageSize()
        {
            return _imageSize;
        }

        public static void SetWikiImageSize(int size)
        {
            _imageSize = size;
        }

        public static async ValueTask<WikiPreviewSearchResult> GenerateSearchResult(PageView value,
            string searchedText, bool loadImage = true)
        {
            string resultName = value.Extract;
            string displayedName = value.Title;
            double score = displayedName.SearchTokens(searchedText);
            string pageId = value.PageId.ToString();
            string wikiUrl = displayedName.Replace(' ', '_');
            BitmapImageResult bitmapImageResult;

            if (value.Thumbnail != null && loadImage)
            {
                string imgUrl = value.Thumbnail.Source;
                using var imageClient = new HttpClient();
                imageClient.DefaultRequestHeaders.UserAgent.TryParseAdd(UserAgentString);
                Stream stream = await imageClient.GetStreamAsync(imgUrl);
                var bitmap = new Bitmap(stream);
                bitmapImageResult = new BitmapImageResult(bitmap);
            }
            else
            {
                bitmapImageResult = BitmapLogo;
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
