using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Threading.Tasks;
using ApiService.DAL;
using ApiService.Services;

namespace ApiService.Jobs
{
    public class StockSyncJob
    {
        private readonly StockRepository _repository;
        private readonly MeilisearchService _meilisearch;

        public StockSyncJob() {
            _repository = new StockRepository(
                ConfigurationManager.ConnectionStrings["APIDB_ConnectionString"].ConnectionString);

            _meilisearch = new MeilisearchService(
                ConfigurationManager.AppSettings["Meili.BaseUrl"],
                ConfigurationManager.AppSettings["Meili.ApiKey"],
                "wms_item_stock");
        }

        public async Task RunAsync() {
            var items = _repository.GetAllStock();

            // If UploadStockAsync throws, RebuildCache is never called,
            // so the existing cache is left untouched (fail-safe behavior).
            var success = await _meilisearch.UploadStockAsync(items);

            if (success) {
                StockCacheService.Instance.RebuildCache(items);
            }
        }
    }
}