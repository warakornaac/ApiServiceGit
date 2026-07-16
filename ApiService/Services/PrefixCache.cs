using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using ApiService.Models;

namespace ApiService.Services
{
    public static class PrefixCache
    {
        private static readonly MemoryCache Cache = MemoryCache.Default;

        public static List<SearchDictionaryModel> Get(string prefix) {
            return Cache.Get(prefix) as List<SearchDictionaryModel>;
        }

        public static void Set(
            string prefix,
            List<SearchDictionaryModel> value) {
            Cache.Set(
                prefix,
                value,
                DateTimeOffset.Now.AddHours(6));
        }
    }
}