using System.Collections.Generic;

namespace WikiPreviewConsole
{
    class WikiResult
    {
        public class PageView
        {
            public int pageid { get; set; }
            public string title { get; set; }
            public string extract { get; set; }
            public WikImage thumbnail { get; set; }
        }

        public class WikImage
        {
            public string source { get; set; }
        }


        public class Query
        {
            public Dictionary<string, PageView> pages { get; set; }
        }

        public class Wiki
        {
            public Query query { get; set; }
        }
    }
}
