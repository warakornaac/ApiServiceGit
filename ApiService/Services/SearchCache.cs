using System;
using System.Runtime.Caching;
using ApiService.Models;

namespace ApiService.Services
{
    public class SearchCache
    {
        private static readonly ObjectCache Cache =
            MemoryCache.Default;

        private const int CacheMinutes = 1;

        public static SearchResponse Get(string key) {
            return Cache.Get(BuildKey(key)) as SearchResponse;
        }

        public static void Set(string key, SearchResponse value) {
            if (value == null)
                return;

            Cache.Set(
                BuildKey(key),
                value,
                DateTimeOffset.Now.AddMinutes(CacheMinutes));
        }

        public static void Remove(string key) {
            Cache.Remove(BuildKey(key));
        }

        public static void Clear() {
            foreach (var item in Cache) {
                if (item.Key.StartsWith("SEARCH_"))
                    Cache.Remove(item.Key);
            }
        }

        private static string BuildKey(string keyword) {
            if (String.IsNullOrWhiteSpace(keyword))
                return "SEARCH_EMPTY";

            return "SEARCH_" +
                keyword.Trim()
                       .ToLowerInvariant();
        }
    }
}