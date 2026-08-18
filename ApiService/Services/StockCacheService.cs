using System;
using System.Collections.Generic;
using ApiService.Models;

namespace ApiService.Services
{
    public class StockCacheService
    {
        private static readonly Lazy<StockCacheService> _instance =
            new Lazy<StockCacheService>(() => new StockCacheService());

        public static StockCacheService Instance {
            get { return _instance.Value; }
        }

        // volatile so that a reference swap in RebuildCache is immediately
        // visible to threads calling GetByItemNo on other requests.
        private volatile Dictionary<string, StockItem> _cache =
            new Dictionary<string, StockItem>(StringComparer.OrdinalIgnoreCase);

        public DateTime LastUpdated { get; private set; }

        private StockCacheService() { }

        /// <summary>
        /// Rebuilds the entire cache from a freshly-fetched dataset.
        /// Call this only after the corresponding Meilisearch upload has succeeded,
        /// so the cache and the search index stay in sync.
        /// </summary>
        public void RebuildCache(List<StockItem> items) {
            if (items == null) throw new ArgumentNullException("items");

            var newCache = new Dictionary<string, StockItem>(items.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var item in items) {
                if (string.IsNullOrWhiteSpace(item.ItemNo))
                    continue;

                // NOTE: if the same item_no can exist under different clients,
                // change the key to a composite of client + item_no instead.
                newCache[item.ItemNo] = item;
            }

            _cache = newCache; // atomic reference swap
            LastUpdated = DateTime.Now;
        }

        public StockItem GetByItemNo(string itemNo) {
            if (string.IsNullOrWhiteSpace(itemNo))
                return null;

            StockItem item;
            _cache.TryGetValue(itemNo, out item);
            return item;
        }

        public int Count {
            get { return _cache.Count; }
        }
    }
}