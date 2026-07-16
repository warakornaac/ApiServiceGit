using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApiService.Models
{
    public class SearchResponse
    {
        [JsonProperty("hits")]
        public List<SearchDictionaryModel> hits { get; set; }
        [JsonProperty("query")]
        public string query { get; set; }
        [JsonProperty("processingTimeMs")]
        public int processingTimeMs { get; set; }

        public int limit { get; set; }

        public int offset { get; set; }
        [JsonProperty("estimatedTotalHits")]
        public int estimatedTotalHits { get; set; }

        public string requestUid { get; set; }


        public SearchResponse() {
            hits = new List<SearchDictionaryModel>();
        }
    }
}