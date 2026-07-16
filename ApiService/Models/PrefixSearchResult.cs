using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApiService.Models
{
    public class PrefixSearchResult
    {
        public string Prefix { get; set; }

        public List<SearchDictionaryModel> Hits { get; set; }

        public PrefixSearchResult() {
            Hits = new List<SearchDictionaryModel>();
        }
    }
}