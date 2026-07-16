using ApiService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApiService.Services
{
    public class KeywordComparer :
    IEqualityComparer<SearchDictionaryModel>
    {
        public bool Equals(
            SearchDictionaryModel x,
            SearchDictionaryModel y) {
            return x.Keyword == y.Keyword;
        }

        public int GetHashCode(
            SearchDictionaryModel obj) {
            return obj.Keyword.GetHashCode();
        }
    }
}