using System.Collections.Generic;

namespace WikiPreview.Fluent.Plugin
{
    internal static class WikiResult
    {
        public class PageView
        {
            public int PageId { get; set; }
            public string Title { get; set; }
            public string Extract { get; set; }
            public WikiImage Thumbnail { get; set; }
        }

        public class WikiImage
        {
            public string Source { get; set; }
        }

        public class Query
        {
            public Dictionary<string, PageView> Pages { get; set; }
        }

        public class Wiki
        {
            public Query Query { get; set; }
        }
    }
}