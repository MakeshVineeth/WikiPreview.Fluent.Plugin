using System;
using System.Collections.Generic;
using System.Linq;

namespace WikiPreview.Fluent.Plugin
{
    class QueryConfiguration
    {
        public string SearchTerm { get; set; }
        public int SentenceCount { get; set; }
        public int WikiNameSpace { get; set; }
        public int ResultsCount { get; set; }
        public int ImageSize { get; set; }
    }
}
