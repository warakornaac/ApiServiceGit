using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Caching;

namespace ApiService.Services
{
    public class LongestWordCache
    {
        private static readonly ObjectCache Cache =
            MemoryCache.Default;

        private const int CacheMinutes = 30;

        public static string Get(string keyword) {
            return Cache.Get(BuildKey(keyword)) as string;
        }

        public static void Set(string keyword, string value) {
            if (string.IsNullOrEmpty(value))
                return;

            Cache.Set(
                BuildKey(keyword),
                value,
                DateTimeOffset.Now.AddMinutes(CacheMinutes));
        }

        public static void Remove(string keyword) {
            Cache.Remove(BuildKey(keyword));
        }

        private static string BuildKey(string keyword) {
            if (String.IsNullOrWhiteSpace(keyword))
                return "LONGEST_EMPTY";

            return "LONGEST_" +
                keyword.Trim().ToLowerInvariant();
        }
    }
}