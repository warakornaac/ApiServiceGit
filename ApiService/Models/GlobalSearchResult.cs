using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApiService.Models
{
    public class GlobalSearchResult
    {
        public bool Success { get; set; }

        public string Message { get; set; }

        public string NormalizeKeyword { get; set; }

        public List<string> SearchTypes { get; set; }

        public List<SearchDictionaryModel> MeiliHits { get; set; }

        public List<ProductSearchVioDataResponse> Items { get; set; }
        // Debug
        public List<string> Tokens { get; set; }

        public SearchSqlRequest SqlRequest { get; set; }

        //public GlobalSearchResult() {
        //    SearchTypes = new List<string>();
        //    MeiliHits = new List<SearchDictionaryModel>();
        //    Items = new List<ProductSearchVioDataResponse>();
        //}

        //public GlobalSearchResult() {
        //    Success = true;
        //    Message = "";
        //    NormalizeKeyword = "";
        //    SearchTypes = new List<string>();
        //    Items = new List<ProductSearchVioDataResponse>();
        //}
    }
}