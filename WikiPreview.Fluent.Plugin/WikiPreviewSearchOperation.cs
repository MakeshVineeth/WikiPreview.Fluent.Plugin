using Blast.Core.Results;

namespace WikiPreview.Fluent.Plugin
{
    public enum ActionType
    {
        Wikipedia,
        Wikiwand,
        GoogleSearch
    }

    public sealed class WikiPreviewSearchOperation : SearchOperationBase
    {
        private WikiPreviewSearchOperation(ActionType actionType, string actionName, string actionDescription,
            string icon)
        {
            ActionType = actionType;
            OperationName = actionName;
            Description = actionDescription;
            IconGlyph = icon;
        }

        public ActionType ActionType { get; }

        public static WikiPreviewSearchOperation OpenWiki { get; } =
            new(ActionType.Wikipedia, "Open in Wikipedia", "Opens the article in Wikipedia.", "\uE71B");

        public static WikiPreviewSearchOperation OpenWikiWand { get; } =
            new(ActionType.Wikiwand, "Open in Wikiwand",
                "Opens the Wikipedia article with a modern look.", "\uE774");

        public static WikiPreviewSearchOperation OpenGoogle { get; } =
            new(ActionType.GoogleSearch, "Search in Google", "Search with Google.", "\uE721");
    }
}
