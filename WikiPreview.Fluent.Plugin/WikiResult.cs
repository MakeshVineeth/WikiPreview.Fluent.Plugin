using System.Collections.Generic;

namespace WikiPreviewConsole
{
    internal class WikiResult
    {
        public class PageView
        {
            public int PageId { get; set; }
            public string Title { get; set; }
            public string Extract { get; set; }
            public WikImage Thumbnail { get; set; }
        }

        public class WikImage
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
