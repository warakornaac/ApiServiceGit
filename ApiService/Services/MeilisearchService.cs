using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ApiService.Models;

namespace ApiService.Services
{
    public class MeilisearchService
    {
        private readonly HttpClient _httpClient;
        private readonly string _indexUid;

        public MeilisearchService(string baseUrl, string apiKey, string indexUid) {
            if (string.IsNullOrWhiteSpace(baseUrl)) throw new ArgumentNullException("baseUrl");
            if (string.IsNullOrWhiteSpace(indexUid)) throw new ArgumentNullException("indexUid");

            _indexUid = indexUid;
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };

            if (!string.IsNullOrWhiteSpace(apiKey)) {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);
            }
        }

        /// <summary>
        /// Uploads (upserts) stock documents to Meilisearch in batches.
        /// Throws an exception if any batch fails, so the caller can
        /// avoid rebuilding the cache with a partially-uploaded dataset.
        /// </summary>
        public async Task<bool> UploadStockAsync(List<StockItem> items, int batchSize = 1000) {
            if (items == null) throw new ArgumentNullException("items");

            for (int i = 0; i < items.Count; i += batchSize) {
                var batch = items.Skip(i).Take(batchSize).ToList();

                // Meilisearch requires a unique primary key per document.
                var payload = batch.Select(x => new {
                    id = BuildDocumentId(x.Company, x.ItemNo),
                    client = x.Company,
                    item_no = x.ItemNo,
                    qty = x.ReadyQty
                });

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(
                    string.Format("/indexes/{0}/documents", _indexUid), content);

                if (!response.IsSuccessStatusCode) {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception(string.Format(
                        "Meilisearch upload failed at batch starting index {0}: {1}", i, error));
                }
            }

            return true;
        }

        private static string BuildDocumentId(string client, string itemNo) {
            // Combine client + item_no so duplicate item_no across clients
            // do not overwrite each other in the index.
            var safeClient = (client ?? string.Empty).Trim();
            var safeItemNo = (itemNo ?? string.Empty).Trim();
            return string.Format("{0}_{1}", safeClient, safeItemNo);
        }
    }
}