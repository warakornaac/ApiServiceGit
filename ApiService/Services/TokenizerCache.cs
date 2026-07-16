using System;
using System.Collections.Generic;
using System.Runtime.Caching;

namespace ApiService.Services
{
    public static class TokenizerCache
    {
        private static readonly ObjectCache Cache =
             MemoryCache.Default;

        private const int CacheMinutes = 180;

        public static List<string> Get(string keyword) {
            return Cache.Get(BuildKey(keyword))
                as List<string>;
        }

        public static void Set(
            string keyword,
            List<string> tokens) {
            if (tokens == null)
                return;

            Cache.Set(
                BuildKey(keyword),
                tokens,
                DateTimeOffset.Now.AddMinutes(CacheMinutes));
        }

        public static void Remove(string keyword) {
            Cache.Remove(BuildKey(keyword));
        }

        public static void Clear() {
            foreach (var item in Cache) {
                if (item.Key.StartsWith("TOKEN_"))
                    Cache.Remove(item.Key);
            }
        }

        private static string BuildKey(string keyword) {
            if (String.IsNullOrWhiteSpace(keyword))
                return "TOKEN_EMPTY";

            return "TOKEN_" +
                keyword.Trim()
                       .ToLowerInvariant();
        }
    }
}