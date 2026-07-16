using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApiService.Models
{
    public class SearchSqlRequest
    {
        public string Keyword { get; set; }

        public List<string> SearchTypes { get; set; }

        public SearchSqlRequest() {
            SearchTypes = new List<string>();
        }
    }
}