using Blast.Core.Results;

namespace WikiPreview.Fluent.Plugin
{
    class WikiPreviewSearchOperation
    {

        public class WikiSearchOperation : SearchOperationBase
        {
            protected internal WikiSearchOperation() : base("Open in Wikipedia",
                "Opens the article on Wikipedia.",
                "\uE71B")
            {

            }
        }

        public class WikiwandSearchOperation : SearchOperationBase
        {
            protected internal WikiwandSearchOperation() : base("Open in Wikiwand", "Opens the Wikipedia article in Wikiwand with a custom viewing experience.",
                "\uE774")
            {
                
            }
        }

        public class CopyUrlSearchOperation : SearchOperationBase
        {
            protected internal CopyUrlSearchOperation() : base("Copy URL", "Copies the Wikipedia Article URL to the Clipboard.",
                "\uE8C8")
            {

            }
        }

        public class GoogleSearchOperation : SearchOperationBase
        {
            protected internal GoogleSearchOperation() : base("Search in Google", "Search with Google.",
                "\uE721")
            {

            }
        }
    }
}
