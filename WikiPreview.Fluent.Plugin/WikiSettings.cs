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
        public int MaxResults { get; set; } = 8;

        [Setting(Name = "Load Images", Description = "Whether to retrieve and load images from Wikipedia",
            DefaultValue = true, IconGlyph = "\uEB9F")]
        public bool LoadImages { get; set; } = true;

        [Setting(Name = "Max Image Size",
            Description = "Sets the max image size shown in the Preview window",
            DefaultValue = 150, IconGlyph = "\uE91B", MinValue = 80, MaxValue = 300, RequireRestart = true,
            IsAdvanced = true)]
        public int ImageSize { get; set; } = 150;
    }
}
