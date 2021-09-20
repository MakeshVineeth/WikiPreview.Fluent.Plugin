using Blast.API.Settings;
using Blast.Core.Objects;
using static WikiPreview.Fluent.Plugin.WikiPreviewSearchResult;

namespace WikiPreview.Fluent.Plugin
{
    public class WikiSettings : SearchApplicationSettingsPage
    {
        public WikiSettings(SearchApplicationInfo applicationInfo) : base(applicationInfo)
        {
        }

        [Setting(Name = "Max Results", Description = "Control number of results from Wikipedia", MinValue = 1,
            MaxValue = 30,
            DefaultValue = 8, IconGlyph = SearchResultIcon)]
        public int MaxResults { get; set; }

        [Setting(Name = "Load Images", Description = "Whether to retrieve and load images from Wikipedia",
            DefaultValue = true, IconGlyph = "\uEB9F")]
        public bool LoadImages { get; set; }
    }
}
