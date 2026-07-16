using ApiService.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ApiService.Services
{
    public class MeiliSearchClient
    {
        private static readonly string Host =
            ConfigurationManager.AppSettings["MeilisearchUrl"];

        private static readonly string ApiKey =
            ConfigurationManager.AppSettings["MeilisearchKey"];

        //--------------------------------------------------
        // Search 1 Keyword
        //--------------------------------------------------

        public async Task<SearchResponse> Search(string keyword) {
            keyword =
                NormalizeService.Normalize(keyword);

            SearchResponse cache =
                SearchCache.Get(keyword);

            if (cache != null)
                return cache;

            SearchResponse result =
                await SearchInternal(keyword);

            SearchCache.Set(keyword, result);

            return result;
        }

        //--------------------------------------------------
        // Search Multiple Keyword
        //--------------------------------------------------

        public async Task<List<SearchDictionaryModel>> SearchBatch(List<string> keywords) {
            if (keywords == null)
                return new List<SearchDictionaryModel>();

            //---------------------------------------
            // Normalize + Distinct
            //---------------------------------------

            keywords =
                keywords
                    .Where(x => !String.IsNullOrWhiteSpace(x))
                    .Select(x => NormalizeService.Normalize(x))
                    .Distinct()
                    .ToList();

            //---------------------------------------
            // Parallel Search
            //---------------------------------------

            var tasks =
                keywords
                    .Select(Search)
                    .ToArray();

            SearchResponse[] responses =
                await Task.WhenAll(tasks);

            //---------------------------------------
            // Merge Result
            //---------------------------------------

            List<SearchDictionaryModel> hits =
                responses
                    .Where(x => x != null)
                    .Where(x => x.hits != null)
                    .SelectMany(x => x.hits)
                    .GroupBy(x => new {
                        x.Keyword,
                        x.SearchType
                    })
                    .Select(g =>
                        g.OrderByDescending(x => x.Priority)
                         .ThenByDescending(x => x._rankingScore)
                         .First())
                    .OrderByDescending(x => x.Keyword.Length)   // <<< สำคัญ
                    .ThenByDescending(x => x.Priority)
                    .ThenByDescending(x => x._rankingScore)
                    .ToList();

            return hits;
        }

        //--------------------------------------------------
        // Prefix Search
        //--------------------------------------------------

        public async Task<List<SearchDictionaryModel>>
            SearchPrefix(string keyword) {
            List<string> prefixes =
                BuildPrefixes(keyword);

            return await SearchBatch(prefixes);
        }

        //--------------------------------------------------
        // Private
        //--------------------------------------------------

        private async Task<SearchResponse>
            SearchInternal(string keyword) {
            using (HttpClient client =
                new HttpClient()) {
                client.DefaultRequestHeaders.Add(
                    "Authorization",
                    "Bearer " + ApiKey);

                var request = new {
                    q = keyword,

                    limit = 10,

                    matchingStrategy = "all",

                    attributesToRetrieve =
                    new[]
                    {
                        "Id",
                        "Keyword",
                        "Normalize",
                        "SearchType",
                        "Priority",
                        "SourceTable",
                        "LanguageCode",
                        "IsActive"
                    },

                    showRankingScore = true
                };

                string json =
                    JsonConvert.SerializeObject(request);

                HttpContent content =
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json");

                HttpResponseMessage response =
                    await client.PostAsync(
                        Host +
                        "/indexes/search_dictionary/search",
                        content);

                string result =
                    await response.Content
                        .ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception(result);

                return JsonConvert
                    .DeserializeObject<SearchResponse>(
                        result);
            }
        }

        //--------------------------------------------------
        // Prefix
        //--------------------------------------------------

        private List<string>
            BuildPrefixes(string keyword) {
            keyword =
                NormalizeService.Normalize(keyword);

            List<string> prefixes =
                new List<string>();

            int max =
                Math.Min(keyword.Length, 15);

            for (int i = max; i >= 2; i--) {
                prefixes.Add(
                    keyword.Substring(0, i));
            }

            return prefixes;
        }
    }
}