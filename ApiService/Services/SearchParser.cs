using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ApiService.Models;

namespace ApiService.Services
{
    public class SearchParser
    {
        public SearchSqlRequest BuildRequest(
    string originalKeyword,
    List<SearchDictionaryModel> hits) {
            SearchSqlRequest request = new SearchSqlRequest();

            if (hits == null || hits.Count == 0) {
                request.Keyword = originalKeyword;
                request.SearchTypes = new List<string>();
                return request;
            }

            hits = hits
                .OrderByDescending(x => x.Priority)
                .ThenByDescending(x => x._rankingScore)
                .ToList();

            string keyword = originalKeyword;

            foreach (var hit in hits) {
                if (string.IsNullOrWhiteSpace(hit.Keyword))
                    continue;

                if (string.IsNullOrWhiteSpace(hit.Normalize))
                    continue;

                if (keyword.IndexOf(hit.Keyword,
                    StringComparison.OrdinalIgnoreCase) >= 0) {
                    keyword = keyword.Replace(
                        hit.Keyword,
                        hit.Normalize);
                }
            }

            request.Keyword = keyword.Trim();

            request.SearchTypes = hits
                .Select(x => x.SearchType)
                .Distinct()
                .ToList();

            return request;
        }
    }
}